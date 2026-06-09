\
#!/usr/bin/env python3
from __future__ import annotations
import hashlib
import json
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/JCLib.VisualStudio"
errors: list[str] = []
checks: dict[str, object] = {}

def need(condition: bool, key: str, message: str) -> None:
    checks[key] = bool(condition)
    if not condition:
        errors.append(message)

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")

manifest = read("src/JCLib.VisualStudio/source.extension.vsixmanifest")
package = read("src/JCLib.VisualStudio/JCLibPackage.cs")
main_xaml = read("src/JCLib.VisualStudio/ToolWindows/JCLibToolWindowControl.xaml")
editor_xaml = read("src/JCLib.VisualStudio/ToolWindows/PackEditorWindow.xaml")
appearance_xaml = read("src/JCLib.VisualStudio/ToolWindows/AppearanceDialog.xaml")
appearance_cs = read("src/JCLib.VisualStudio/ToolWindows/AppearanceDialog.xaml.cs")
models = read("src/JCLib.VisualStudio/Models/CatalogModels.cs")
editor_models = read("src/JCLib.VisualStudio/Models/PackEditorModels.cs")
theme = read("src/JCLib.VisualStudio/Services/ThemeService.cs")
prefs = read("src/JCLib.VisualStudio/Services/UserPreferencesStore.cs")

need('Version="1.3.11"' in manifest, "manifest-version", "VSIX manifest must use version 1.3.11")
need('"1.3.11")' in package, "package-version", "InstalledProductRegistration must use version 1.3.11")
need('Visual Pack Editor avancé 1.3.11' in editor_xaml, "editor-title", "Visual Pack Editor title must use 1.3.11")

for file in sorted((SRC / "ToolWindows").glob("*.xaml")) + [SRC / "source.extension.vsixmanifest"]:
    try:
        ET.parse(file)
        checks[f"xml:{file.name}"] = True
    except Exception as exc:
        checks[f"xml:{file.name}"] = False
        errors.append(f"Invalid XML/XAML in {file.relative_to(ROOT)}: {exc}")

palette = [
    "TreeRoot", "TreePack", "TreeEnvironment", "TreeLibrary", "TreeCategory",
    "TreeGroup", "TreeElement", "TreeBadge", "TreeIconText",
]
for name in palette:
    need(f'public string {name}' in prefs, f"prefs:{name}", f"Missing ThemePreferences.{name}")
    need(f'"{name}Brush"' in theme, f"theme:{name}", f"ThemeService must publish {name}Brush")

for text, key in [
    ('VisualKindLabel', 'catalog-visual-kind'), ('VisualGlyph', 'catalog-glyph'),
    ('CountLabel', 'catalog-count'), ('VisualToolTip', 'catalog-tooltip'),
]:
    need(text in models, key, f"CatalogNode must expose {text}")
    need(text in editor_models, f"editor-{key}", f"PackEditorNode must expose {text}")

need('Children.CollectionChanged' in models, 'catalog-count-live', 'CatalogNode counters must refresh on child collection changes')
need('Children.CollectionChanged' in editor_models, 'editor-count-live', 'PackEditorNode counters must refresh on child collection changes')

for xaml, prefix in [(main_xaml, 'main'), (editor_xaml, 'editor')]:
    need('NodeIconBorder' in xaml, f'{prefix}-node-icon', f'{prefix} tree must use icon borders')
    need('VisualKindLabel' in xaml, f'{prefix}-kind-badge', f'{prefix} tree must render kind badges')
    need('CountLabel' in xaml, f'{prefix}-count-badge', f'{prefix} tree must render count badges')
    need('TreeEnvironmentBrush' in xaml and 'TreeLibraryBrush' in xaml and 'TreeCategoryBrush' in xaml and 'TreeGroupBrush' in xaml,
         f'{prefix}-palette', f'{prefix} tree must use hierarchy palette brushes')

need('Catalogue structuré' in main_xaml, 'main-legend', 'Main browser must expose a hierarchy legend')
need('Hiérarchie du pack' in editor_xaml and 'ToolTip="Bibliothèque"' in editor_xaml, 'editor-legend', 'Pack editor must expose a hierarchy legend')
need('Palette de la hiérarchie' in appearance_xaml, 'appearance-section', 'Appearance dialog must expose hierarchy palette section')
for name in ["TreeRootTextBox", "TreePackTextBox", "TreeEnvironmentTextBox", "TreeLibraryTextBox", "TreeCategoryTextBox", "TreeGroupTextBox", "TreeElementTextBox", "TreeBadgeTextBox", "TreeIconTextTextBox"]:
    need(name in appearance_xaml and name in appearance_cs, f'appearance:{name}', f'Appearance dialog must load/save {name}')

# The UI-only release must not alter bundled catalog bytes relative to the 1.3.10 source snapshot.
reference = ROOT.parent / 'visualStudio_JClib-1.3.10-reference' / 'src/JCLib.VisualStudio/Assets/Packs'
current = SRC / 'Assets/Packs'
if reference.exists():
    ref_files = sorted(p.name for p in reference.glob('*.json'))
    cur_files = sorted(p.name for p in current.glob('*.json'))
    need(ref_files == cur_files, 'catalog-file-set', 'Catalog file set changed unexpectedly')
    unchanged = True
    hashes = {}
    for name in cur_files:
        r = hashlib.sha256((reference/name).read_bytes()).hexdigest()
        c = hashlib.sha256((current/name).read_bytes()).hexdigest()
        hashes[name] = c
        if r != c:
            unchanged = False
            errors.append(f'Catalog changed unexpectedly: {name}')
    checks['catalog-hashes'] = hashes
    checks['catalog-bytes-unchanged'] = unchanged
else:
    checks['catalog-bytes-unchanged'] = 'not-checked: reference directory not present'

result = {
    'status': 'ok' if not errors else 'error',
    'version': '1.3.11',
    'compileStatus': 'not-run: Linux environment does not provide Visual Studio 2026, MSBuild or the Visual Studio SDK.',
    'checks': checks,
    'errors': errors,
}
out = ROOT / 'docs/VISUALSTUDIO_TREE_VISUAL_HIERARCHY_1.3.11_VALIDATION.json'
out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, ensure_ascii=False, indent=2))
raise SystemExit(0 if not errors else 1)
