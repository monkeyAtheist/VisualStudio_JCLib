#!/usr/bin/env python3
"""Validate JC Lib VS Code catalogs against the Visual Studio 1.3.1 importer contract."""
from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter
from pathlib import Path

PLACEHOLDER_RE = re.compile(r"\{\{([A-Za-z_][A-Za-z0-9_]*)\}\}")
KNOWN_EDITOR_TYPES = {"", "text", "enum", "handle", "pathFile", "pathFolder", "boolean"}


def walk_functions(value):
    if isinstance(value, dict):
        functions = value.get("functions")
        if isinstance(functions, list):
            for function in functions:
                if isinstance(function, dict):
                    yield function
        for key, child in value.items():
            if key != "functions":
                yield from walk_functions(child)
    elif isinstance(value, list):
        for child in value:
            yield from walk_functions(child)


def has_useful_empty_choice(choice: dict) -> bool:
    return "value" in choice and str(choice.get("value") or "") == "" and bool(
        str(choice.get("label") or choice.get("description") or choice.get("detail") or "").strip()
    )


def validate_choice(choice, path: str, errors: list[str], stats: Counter):
    if isinstance(choice, (str, int, float, bool)):
        stats["simple_choices"] += 1
        return
    if not isinstance(choice, dict):
        errors.append(f"{path}: choice must be a scalar or object")
        return
    stats["documented_choices"] += 1
    has_explicit_value = "value" in choice
    effective = str(choice.get("value") if has_explicit_value else (choice.get("constant") or choice.get("label") or ""))
    if not effective.strip() and not has_useful_empty_choice(choice):
        errors.append(f"{path}: documented choice has no usable value/constant/label")
    if has_useful_empty_choice(choice):
        stats["explicit_empty_choices"] += 1
    source_types = choice.get("sourceTypes")
    if source_types is not None and (not isinstance(source_types, list) or any(not isinstance(v, str) for v in source_types)):
        errors.append(f"{path}.sourceTypes: must be an array of strings")


def validate_choices(values, path: str, errors: list[str], stats: Counter):
    if values is None:
        return
    if not isinstance(values, list):
        errors.append(f"{path}: must be an array")
        return
    for index, choice in enumerate(values):
        validate_choice(choice, f"{path}[{index}]", errors, stats)


def validate_picker(picker, path: str, errors: list[str], stats: Counter):
    if not isinstance(picker, dict):
        errors.append(f"{path}: pickerConfig must be an object")
        return
    stats["picker_configs"] += 1
    if picker.get("multiSelect") is True:
        stats["multi_select_pickers"] += 1
    if "valueSeparator" in picker and not isinstance(picker.get("valueSeparator"), str):
        errors.append(f"{path}.valueSeparator: must be a string")
    if "emptyValue" in picker and not isinstance(picker.get("emptyValue"), str):
        errors.append(f"{path}.emptyValue: must be a string")
    if "applyDefaultIfEmpty" in picker and not isinstance(picker.get("applyDefaultIfEmpty"), bool):
        errors.append(f"{path}.applyDefaultIfEmpty: must be a boolean")
    source_types = picker.get("sourceTypes", picker.get("controlTypes"))
    if source_types is not None and (not isinstance(source_types, list) or any(not isinstance(v, str) for v in source_types)):
        errors.append(f"{path}.sourceTypes: must be an array of strings")
    sections = picker.get("sections", [])
    if not isinstance(sections, list):
        errors.append(f"{path}.sections: must be an array")
        return
    if not sections:
        stats["picker_configs_without_sections"] += 1
    for section_index, section in enumerate(sections):
        section_path = f"{path}.sections[{section_index}]"
        if not isinstance(section, dict):
            errors.append(f"{section_path}: section must be an object")
            continue
        groups = section.get("groups", [])
        if not isinstance(groups, list):
            errors.append(f"{section_path}.groups: must be an array")
            continue
        for group_index, group in enumerate(groups):
            group_path = f"{section_path}.groups[{group_index}]"
            if not isinstance(group, dict):
                errors.append(f"{group_path}: group must be an object")
                continue
            validate_choices(group.get("items", []), f"{group_path}.items", errors, stats)


def validate_pack(path: Path) -> dict:
    errors: list[str] = []
    stats: Counter = Counter()
    try:
        pack = json.loads(path.read_text(encoding="utf-8-sig"))
    except Exception as exc:
        return {"file": path.name, "errors": [f"invalid JSON: {exc}"], "stats": {}}
    for key in ("id", "name", "version", "environments"):
        if key not in pack:
            errors.append(f"root: missing {key!r}")
    if not isinstance(pack.get("environments"), list):
        errors.append("root.environments: must be an array")
    for function_index, function in enumerate(walk_functions(pack)):
        stats["functions"] += 1
        parameters = function.get("parameters", []) or []
        if not isinstance(parameters, list):
            errors.append(f"function[{function_index}] {function.get('name')}: parameters must be an array")
            continue
        param_names: set[str] = set()
        for parameter_index, parameter in enumerate(parameters):
            parameter_path = f"function[{function_index}] {function.get('name')}.parameters[{parameter_index}]"
            stats["parameters"] += 1
            if not isinstance(parameter, dict):
                errors.append(f"{parameter_path}: parameter must be an object")
                continue
            name = str(parameter.get("name") or "").strip()
            if not name:
                errors.append(f"{parameter_path}: missing name")
            else:
                param_names.add(name)
            editor_type = str(parameter.get("editorType") or "")
            stats[f"editor_type:{editor_type or '<none>'}"] += 1
            if editor_type not in KNOWN_EDITOR_TYPES:
                stats["unknown_editor_types"] += 1
            if editor_type in ("pathFile", "pathFolder"):
                stats["path_browsers"] += 1
            validate_choices(parameter.get("options"), f"{parameter_path}.options", errors, stats)
            validate_choices(parameter.get("presets"), f"{parameter_path}.presets", errors, stats)
            if "pickerConfig" in parameter:
                validate_picker(parameter.get("pickerConfig"), f"{parameter_path}.pickerConfig", errors, stats)
        insert_text = str(function.get("insertText") or "")
        placeholders = set(PLACEHOLDER_RE.findall(insert_text))
        if placeholders:
            stats["parameterized_templates"] += 1
        missing = placeholders - param_names
        if missing:
            errors.append(f"function[{function_index}] {function.get('name')}: unresolved placeholders {sorted(missing)}")
    return {
        "file": path.name,
        "id": pack.get("id"),
        "name": pack.get("name"),
        "version": pack.get("version"),
        "stats": dict(sorted(stats.items())),
        "errors": errors,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("catalog_dir", type=Path, help="Directory containing JC Lib pack JSON files")
    parser.add_argument("--output", type=Path, help="Optional JSON report path")
    args = parser.parse_args()
    files = sorted(args.catalog_dir.glob("*.json"))
    results = [validate_pack(path) for path in files]
    aggregate: Counter = Counter()
    errors: list[str] = []
    for result in results:
        aggregate.update(result.get("stats", {}))
        errors.extend(f"{result['file']}: {message}" for message in result.get("errors", []))
    report = {
        "status": "ok" if not errors else "error",
        "catalog_directory": str(args.catalog_dir),
        "files": len(files),
        "aggregate": dict(sorted(aggregate.items())),
        "packs": results,
        "errors": errors,
    }
    text = json.dumps(report, indent=2, ensure_ascii=False)
    print(text)
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(text + "\n", encoding="utf-8")
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
