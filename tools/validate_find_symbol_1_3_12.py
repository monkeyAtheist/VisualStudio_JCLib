#!/usr/bin/env python3
from __future__ import annotations
import hashlib, json
from pathlib import Path
from xml.etree import ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'src/JCLib.VisualStudio'
errors=[]; checks={}
def need(cond,key,msg):
    checks[key]=bool(cond)
    if not cond: errors.append(msg)
def read(rel): return (ROOT/rel).read_text(encoding='utf-8')
manifest=read('src/JCLib.VisualStudio/source.extension.vsixmanifest')
package=read('src/JCLib.VisualStudio/JCLibPackage.cs')
ids=read('src/JCLib.VisualStudio/PackageIds.cs')
vsct=read('src/JCLib.VisualStudio/Commands/JCLibPackage.vsct')
main_xaml=read('src/JCLib.VisualStudio/ToolWindows/JCLibToolWindowControl.xaml')
main_cs=read('src/JCLib.VisualStudio/ToolWindows/JCLibToolWindowControl.xaml.cs')
dlg_xaml=read('src/JCLib.VisualStudio/ToolWindows/FindSymbolDialog.xaml')
dlg_cs=read('src/JCLib.VisualStudio/ToolWindows/FindSymbolDialog.xaml.cs')
need('Version="1.3.12"' in manifest,'manifest-version','VSIX manifest must use 1.3.12')
need('"1.3.12")' in package,'package-version','InstalledProductRegistration must use 1.3.12')
need('FindSymbol = 0x0101' in ids,'package-id','FindSymbol package command id missing')
need('cmdidFindSymbol' in vsct,'vsct-command','VSCT Find Symbol command missing')
need('key1="P" mod1="CONTROL" mod2="ALT"' in vsct,'vsct-shortcut','Ctrl+Alt+P default binding missing')
need('ExecuteFindSymbol' in package and 'OpenFindSymbolDialog' in package,'package-handler','Package command must open Find Symbol')
need('OnFindSymbolClick' in main_xaml and 'OpenFindSymbolDialog' in main_cs,'toolwindow-button','Tool window Find Symbol button missing')
need('QueryTextBox.Focus()' in dlg_cs and 'Keyboard.Focus(QueryTextBox)' in dlg_cs,'dialog-focus','Dialog must focus its query input')
need('if (query.Length == 0)' in dlg_cs and 'ClearResultsAndPreview' in dlg_cs,'dialog-empty-query','Dialog must clear results for an empty query')
need('PreviewBorder.Visibility = Visibility.Collapsed' in dlg_cs,'dialog-empty-preview','Dialog must hide preview before a query')
need('ResultsListBox.ItemsSource = null' in dlg_cs,'dialog-empty-results','Dialog must clear result list before a query')
for f in sorted((SRC/'ToolWindows').glob('*.xaml'))+[SRC/'source.extension.vsixmanifest',SRC/'Commands/JCLibPackage.vsct']:
    try:
        ET.parse(f); checks[f'xml:{f.name}']=True
    except Exception as exc:
        checks[f'xml:{f.name}']=False; errors.append(f'Invalid XML {f.relative_to(ROOT)}: {exc}')
# Validate synchronized distributed catalogs
packs=SRC/'Assets/Packs'
versions={}
for p in sorted(packs.glob('*.json')):
    try:
        data=json.loads(p.read_text(encoding='utf-8'))
        versions[p.name]=data.get('version','')
        checks[f'json:{p.name}']=True
    except Exception as exc:
        checks[f'json:{p.name}']=False; errors.append(f'Invalid catalog {p.name}: {exc}')
need(versions.get('web_language_pack.json')=='2.0.0','web-pack-version','Web pack must be synchronized to 2.0.0')
need(versions.get('c_language_pack.json')=='1.12.0','c-pack-version','C pack must be synchronized to 1.12.0')
need(versions.get('cpp_language_pack.json')=='2.16.0','cpp-pack-version','C++ pack must be synchronized to 2.16.0')
need(versions.get('windows_api_device_pack.json')=='1.9.0','win32-pack-version','Win32 pack must be synchronized to 1.9.0')
result={
 'status':'ok' if not errors else 'error',
 'version':'1.3.12',
 'compileStatus':'not-run: Linux environment does not provide Visual Studio 2026, MSBuild or the Visual Studio SDK.',
 'checks':checks,
 'catalogVersions':versions,
 'errors':errors,
}
out=ROOT/'docs/VISUALSTUDIO_FIND_SYMBOL_SHORTCUT_1.3.12_VALIDATION.json'
out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
raise SystemExit(0 if not errors else 1)
