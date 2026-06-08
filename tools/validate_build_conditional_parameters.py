#!/usr/bin/env python3
from __future__ import annotations
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
PROJECT=ROOT/'src'/'JCLib.VisualStudio'
pack=json.loads((PROJECT/'Assets/Packs/build_pack.json').read_text(encoding='utf-8-sig'))

def walk(v):
    if isinstance(v,dict):
        for f in v.get('functions',[]) or []:
            if isinstance(f,dict): yield f
        for k,c in v.items():
            if k!='functions': yield from walk(c)
    elif isinstance(v,list):
        for c in v: yield from walk(c)

def is_enabled(params, values, index):
    c=params[index].get('enabledWhen')
    if not isinstance(c,dict): return True
    source=-1
    if isinstance(c.get('index'),int): source=c['index']
    elif isinstance(c.get('parameter'),str):
        source=next((i for i,p in enumerate(params) if p.get('name')==c['parameter']),-1)
    if source<0 or source>=len(values) or source==index: return True
    value=str(values[source] or '').strip()
    if c.get('notEmpty') is True and not value: return False
    if c.get('empty') is True and value: return False
    if isinstance(c.get('equals'),str) and c['equals'] and value!=c['equals']: return False
    if isinstance(c.get('notEquals'),str) and c['notEquals'] and value==c['notEquals']: return False
    if isinstance(c.get('values'),list) and c['values'] and value not in c['values']: return False
    return True

errors=[]
functions=list(walk(pack))
by_name={f.get('name'):f for f in functions}
expected=[
'gcc compile executable','gcc compile object','gcc shared library','g++ compile executable',
'g++ shared library','clang++ compile C++ executable','MinGW gcc executable',
'MinGW g++ executable','MinGW DLL import library']
checked=[]
for name in expected:
    f=by_name.get(name)
    if not f:
        errors.append(f'missing {name}')
        continue
    if f.get('symbolKind')!='command': errors.append(f'{name}: expected symbolKind command')
    params=f.get('parameters',[]) or []
    names=[p.get('name') for p in params]
    if 'includePathPrefix' not in names or 'includeDirectory' not in names:
        errors.append(f'{name}: missing include prefix/path pair')
        continue
    i_prefix=names.index('includePathPrefix'); i_dir=names.index('includeDirectory')
    values=['' for _ in params]
    if is_enabled(params,values,i_dir): errors.append(f'{name}: includeDirectory should start disabled')
    values[i_prefix]='-I'
    if not is_enabled(params,values,i_dir): errors.append(f'{name}: includeDirectory should enable after -I')
    pc=params[i_prefix].get('pickerConfig',{})
    if pc.get('defaultTargetIndex')!=i_dir: errors.append(f'{name}: include defaultTargetIndex mismatch')
    if name!='gcc compile object':
        if 'libraryPathPrefix' not in names or 'libraryDirectory' not in names or 'linkedLibraries' not in names:
            errors.append(f'{name}: missing linker parameters')
        else:
            l_prefix=names.index('libraryPathPrefix'); l_dir=names.index('libraryDirectory')
            values=['' for _ in params]
            if is_enabled(params,values,l_dir): errors.append(f'{name}: libraryDirectory should start disabled')
            values[l_prefix]='-L'
            if not is_enabled(params,values,l_dir): errors.append(f'{name}: libraryDirectory should enable after -L')
            pc=params[l_prefix].get('pickerConfig',{})
            if pc.get('defaultTargetIndex')!=l_dir: errors.append(f'{name}: library defaultTargetIndex mismatch')
    checked.append(name)
source=(PROJECT/'Services/SnippetParameterService.cs').read_text(encoding='utf-8')
ui=(PROJECT/'ToolWindows/JCLibToolWindowControl.xaml.cs').read_text(encoding='utf-8')
loader=(PROJECT/'Services/CatalogLoader.cs').read_text(encoding='utf-8')
models=(PROJECT/'Models/CatalogModels.cs').read_text(encoding='utf-8')
for token,text in [
    ('public static bool IsEnabled',source),('EnabledWhen',models),('DefaultTargetIndex',models),
    ('ParseEnabledWhen',loader),('RefreshConditionalParameterEditors',ui),('SetEnabled(bool enabled)',ui),
    ('NormalizeCommandText',source),('IsCommand(entry)',source),('SymbolKind ?? string.Empty).Trim(), "command"',models)
]:
    if token not in text: errors.append(f'missing source token: {token}')
report={'status':'ok' if not errors else 'error','pack_version':pack.get('version'),'cards':len(functions),'commands_checked':checked,'errors':errors}
print(json.dumps(report,ensure_ascii=False,indent=2))
raise SystemExit(0 if not errors else 1)
