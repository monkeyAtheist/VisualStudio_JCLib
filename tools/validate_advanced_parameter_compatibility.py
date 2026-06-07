#!/usr/bin/env python3
"""Static compatibility checks for JC Lib Visual Studio structured parameters.

This validator intentionally avoids requiring Visual Studio or MSBuild. It verifies the
JSON contract, the bundled fallback catalog, the advanced fixture, XML resources and
source hooks that must exist before a Windows VSIX build is attempted.
"""
from __future__ import annotations
from pathlib import Path
import json
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "src/JCLib.VisualStudio/Assets/Packs/default_pack.json"
FIXTURE = ROOT / "docs/example_packs/jclib_advanced_parameter_pack.json"


def walk_groups(groups):
    for group in groups or []:
        yield from group.get("functions", []) or []
        yield from walk_groups(group.get("groups"))


def walk_functions(pack):
    for env in pack.get("environments", []) or []:
        for lib in env.get("libraries", []) or []:
            for category in lib.get("categories", []) or []:
                yield from category.get("functions", []) or []
                yield from walk_groups(category.get("groups"))


def validate_choice(value, path, errors):
    if isinstance(value, str):
        return
    if not isinstance(value, dict):
        errors.append(f"{path}: choice must be a string or object")
        return
    has_explicit_value = "value" in value
    effective = str(value.get("value") if has_explicit_value else (value.get("constant") or value.get("label") or ""))
    if not effective.strip() and not (has_explicit_value and str(value.get("label") or value.get("description") or "").strip()):
        errors.append(f"{path}: documented choice has no value/constant/label")


def validate_picker(picker, path, errors, stats):
    if not isinstance(picker, dict):
        errors.append(f"{path}: pickerConfig must be an object")
        return
    if picker.get("multiSelect") is True:
        stats["multi_select_pickers"] += 1
        if not isinstance(picker.get("valueSeparator", " | "), str):
            errors.append(f"{path}: multi-select valueSeparator must be a string")
    sections = picker.get("sections", [])
    if sections is not None and not isinstance(sections, list):
        errors.append(f"{path}: sections must be an array")
        return
    for si, section in enumerate(sections or []):
        if not isinstance(section, dict):
            errors.append(f"{path}.sections[{si}]: section must be an object")
            continue
        groups = section.get("groups", [])
        if not isinstance(groups, list):
            errors.append(f"{path}.sections[{si}].groups: must be an array")
            continue
        for gi, group in enumerate(groups):
            if not isinstance(group, dict):
                errors.append(f"{path}.sections[{si}].groups[{gi}]: group must be an object")
                continue
            items = group.get("items", [])
            if not isinstance(items, list):
                errors.append(f"{path}.sections[{si}].groups[{gi}].items: must be an array")
                continue
            for ii, item in enumerate(items):
                validate_choice(item, f"{path}.sections[{si}].groups[{gi}].items[{ii}]", errors)


def validate_pack(path: Path):
    pack = json.loads(path.read_text(encoding="utf-8"))
    errors = []
    stats = {
        "functions": 0,
        "parameters": 0,
        "documented_choices": 0,
        "picker_configs": 0,
        "multi_select_pickers": 0,
        "path_browsers": 0,
        "templates": 0,
    }
    for fi, fn in enumerate(walk_functions(pack)):
        stats["functions"] += 1
        params = fn.get("parameters", []) or []
        if not isinstance(params, list):
            errors.append(f"function[{fi}] {fn.get('name')}: parameters must be an array")
            continue
        if "{{" in str(fn.get("insertText", "")):
            stats["templates"] += 1
        for pi, param in enumerate(params):
            stats["parameters"] += 1
            ppath = f"function[{fi}] {fn.get('name')}.parameters[{pi}]"
            if not isinstance(param, dict):
                errors.append(f"{ppath}: parameter must be an object")
                continue
            for list_name in ("options", "presets"):
                values = param.get(list_name, []) or []
                if not isinstance(values, list):
                    errors.append(f"{ppath}.{list_name}: must be an array")
                    continue
                for ci, choice in enumerate(values):
                    validate_choice(choice, f"{ppath}.{list_name}[{ci}]", errors)
                    if isinstance(choice, dict):
                        stats["documented_choices"] += 1
            if param.get("editorType") in ("pathFile", "pathFolder"):
                stats["path_browsers"] += 1
            if "pickerConfig" in param:
                stats["picker_configs"] += 1
                validate_picker(param["pickerConfig"], f"{ppath}.pickerConfig", errors, stats)
    return pack, stats, errors


def format_path_for_template(template: str, name: str, path: str) -> str:
    escaped = path.replace("\\", "\\\\").replace('"', '\\"')
    wrapped = re.search(rf'''["']\s*\{{\{{{re.escape(name)}\}}\}}\s*["']''', template)
    return escaped if wrapped else f'"{escaped}"'


def render(template: str, values: dict[str, str]) -> str:
    output = template
    for name, value in values.items():
        output = re.sub(rf"\{{\{{{re.escape(name)}\}}\}}", lambda _m: value, output)
    return output



def combine_multi_selection(values: list[str], separator: str, empty_value: str) -> str:
    non_empty = [value for value in values if str(value).strip()]
    return separator.join(non_empty) if non_empty else empty_value


def require_source(path: str, snippets: list[str], errors: list[str]):
    source = (ROOT / path).read_text(encoding="utf-8")
    for snippet in snippets:
        if snippet not in source:
            errors.append(f"{path}: missing source hook {snippet!r}")


def main() -> int:
    errors = []
    fallback, fallback_stats, fallback_errors = validate_pack(PACK)
    fixture, fixture_stats, fixture_errors = validate_pack(FIXTURE)
    errors += fallback_errors + fixture_errors
    if fallback.get("version") != "2.05.0":
        errors.append(f"Bundled default pack version is {fallback.get('version')!r}, expected '2.05.0'")

    require_source("src/JCLib.VisualStudio/Services/CatalogLoader.cs", [
        "ParseChoices(parameter.Options)",
        "ParsePickerConfig(parameter.PickerConfig)",
        "HasExplicitDefaultValue = parameter.DefaultValue is not null",
        "JsonConvert.DeserializeObject<PackDto>",
    ], errors)
    require_source("src/JCLib.VisualStudio/Services/SnippetParameterService.cs", [
        "HasParameterizedInsertTemplate",
        "ApplyParameterizedInsertTemplate",
        "CreateEffectivePickerConfig",
        "FormatPathForTemplate",
    ], errors)
    require_source("src/JCLib.VisualStudio/ToolWindows/JCLibToolWindowControl.xaml.cs", [
        "OnStructuredPickerClick",
        "StructuredChoiceDialog",
        "FormatPathForTemplate",
    ], errors)
    require_source("src/JCLib.VisualStudio/Services/PackEditorDocument.cs", [
        "SetParameterChoiceListProperty",
        "SetParameterObjectProperty",
        "SetParameterBooleanProperty",
    ], errors)

    for xml_path in [
        ROOT / "src/JCLib.VisualStudio/source.extension.vsixmanifest",
        ROOT / "src/JCLib.VisualStudio/ToolWindows/JCLibToolWindowControl.xaml",
        ROOT / "src/JCLib.VisualStudio/ToolWindows/PackEditorWindow.xaml",
    ]:
        try:
            ET.parse(xml_path)
        except Exception as exc:
            errors.append(f"{xml_path.relative_to(ROOT)}: XML parse error: {exc}")

    fixture_fn = next(fn for fn in walk_functions(fixture) if fn["name"] == "configure_device")
    file_value = format_path_for_template(fixture_fn["insertText"], "path", r"C:\work\device.json")
    generated = render(fixture_fn["insertText"], {"path": file_value, "mode": "MODE_FAST", "flags": "FLAG_LOG | FLAG_RETRY"})
    if generated != 'configure_device("C:\\\\work\\\\device.json", MODE_FAST, FLAG_LOG | FLAG_RETRY)':
        errors.append(f"Unexpected fixture rendering: {generated!r}")
    wrapped_value = format_path_for_template('tool --input "{{path}}"', "path", r"C:\work\device.json")
    if wrapped_value != r"C:\\work\\device.json":
        errors.append(f"Quoted template path adaptation failed: {wrapped_value!r}")
    empty_fn = next(fn for fn in walk_functions(fixture) if fn["name"] == "optional fragment")
    empty_render = render(empty_fn["insertText"], {"required": "run", "optionalFlags": ""})
    if empty_render != "command run ":
        errors.append(f"Explicit empty default was not preserved: {empty_render!r}")
    if combine_multi_selection(["", "FLAG_LOG"], " | ", "0") != "FLAG_LOG":
        errors.append("Empty documented choice polluted a non-empty multi-selection")
    if combine_multi_selection([""], " | ", "") != "":
        errors.append("Empty documented choice was not preserved when selected alone")

    print(json.dumps({
        "status": "ok" if not errors else "failed",
        "bundled_pack": {"id": fallback.get("id"), "version": fallback.get("version"), **fallback_stats},
        "advanced_fixture": {"id": fixture.get("id"), "version": fixture.get("version"), **fixture_stats},
        "errors": errors,
    }, ensure_ascii=False, indent=2))
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
