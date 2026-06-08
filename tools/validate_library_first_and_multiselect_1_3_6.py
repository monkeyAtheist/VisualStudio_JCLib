#!/usr/bin/env python3
import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=root/'src/JCLib.VisualStudio'
build=json.loads((src/'Assets/Packs/build_pack.json').read_text(encoding='utf-8'))
assert build['version']=='1.2.3'
def walk_groups(groups):
 for g in groups or []:
  yield from g.get('functions',[])
  yield from walk_groups(g.get('groups',[]))
def funcs():
 for env in build.get('environments',[]):
  for lib in env.get('libraries',[]):
   for cat in lib.get('categories',[]):
    yield from cat.get('functions',[])
    yield from walk_groups(cat.get('groups',[]))
entries=list(funcs())
obj=next(x for x in entries if x.get('name')=='gcc compile object')
w=next(x for x in obj['parameters'] if x.get('name')=='warnings')
assert w['pickerConfig']['minimumSelections']==1
assert w['pickerConfig']['defaultValue']=='-Wall -Wextra'
cl=next(x for x in entries if x.get('name')=='cl C++ executable')
cw=next(x for x in cl['parameters'] if x.get('name')=='warnings')
items=[i for s in cw['pickerConfig']['sections'] for g in s['groups'] for i in g['items']]
w4=next(i for i in items if i['value']=='/W4')
assert '/Wall' in w4['incompatibleWith']
loader=(src/'Services/CatalogLoader.cs').read_text(encoding='utf-8')
dialog=(src/'ToolWindows/StructuredChoiceDialog.cs').read_text(encoding='utf-8')
editor=(src/'Services/PackEditorDocument.cs').read_text(encoding='utf-8')
for token in ['MinimumSelections','DefaultValue','ValidationMessage','IncompatibleWith','AddVisibleLibrary','PackDto']:
 assert token in loader or token in dialog or token in editor, token
assert 'BuildLibraryFirstStorageRoot' in editor
assert 'NormalizeLibraryFirstInputForEditing' in editor
assert 'EnsureMinimumSelectionFallback' in dialog
assert 'UpdateCompatibilityState' in dialog
print('JC Lib Visual Studio 1.3.6 library-first and multi-select validation: OK')
