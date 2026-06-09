from pathlib import Path
import json,re,sys
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'src/JCLib.VisualStudio'
PACKS=SRC/'Assets/Packs'
errors=[]
def need(cond,code,msg):
    if not cond: errors.append({'code':code,'message':msg})
def walk_functions(obj):
    if isinstance(obj,dict):
        if 'functions' in obj and isinstance(obj['functions'],list):
            for f in obj['functions']:
                if isinstance(f,dict): yield f
        for v in obj.values(): yield from walk_functions(v)
    elif isinstance(obj,list):
        for v in obj: yield from walk_functions(v)
def find(pack,name): return [f for f in walk_functions(pack) if f.get('name')==name]
manifest=(SRC/'source.extension.vsixmanifest').read_text(encoding='utf-8')
package=(SRC/'JCLibPackage.cs').read_text(encoding='utf-8')
models=(SRC/'Models/CatalogModels.cs').read_text(encoding='utf-8')
loader=(SRC/'Services/CatalogLoader.cs').read_text(encoding='utf-8')
service=(SRC/'Services/SnippetParameterService.cs').read_text(encoding='utf-8')
ui=(SRC/'ToolWindows/JCLibToolWindowControl.xaml.cs').read_text(encoding='utf-8')
dialog=(SRC/'ToolWindows/StructuredChoiceDialog.cs').read_text(encoding='utf-8')
csproj=(SRC/'JCLib.VisualStudio.csproj').read_text(encoding='utf-8')
need('Version="1.3.10"' in manifest,'manifest-version','VSIX manifest must use 1.3.10')
need('"1.3.10"' in package,'package-version','InstalledProductRegistration must use 1.3.10')
for asset in ['default_pack.json','c_language_pack.json','cpp_language_pack.json','windows_api_device_pack.json']:
    need((PACKS/asset).is_file(),'asset-'+asset,f'missing {asset}')
    need(asset in csproj,'csproj-'+asset,f'{asset} not included in csproj')
need('InsertValueMap' in models and 'ParseStringMap' in loader and 'insertValueMap' in loader,'insert-value-map','insertValueMap contract is missing')
need('ResolveParameterizedInsertValue' in service,'insert-value-runtime','insertValueMap runtime is missing')
need('AcceptsReturn = isMultiline' in ui,'multiline-ui','multiline editor is missing')
need('PreserveSourceOrder' in models and 'preserveSourceOrder' in loader and '_config.PreserveSourceOrder' in dialog,'source-order','preserveSourceOrder support is missing')
for asset,version in [('default_pack.json','2.25.0'),('c_language_pack.json','1.12.0'),('cpp_language_pack.json','2.16.0'),('windows_api_device_pack.json','1.9.0')]:
    pack=json.loads((PACKS/asset).read_text(encoding='utf-8'))
    need(pack.get('version')==version,'pack-version-'+asset,f'{asset}: expected {version}, got {pack.get("version")}')
for asset in ['c_language_pack.json','cpp_language_pack.json','windows_api_device_pack.json']:
    pack=json.loads((PACKS/asset).read_text(encoding='utf-8'))
    funcs=find(pack,'GetProcAddress')
    need(len(funcs)==1,'getproc-count-'+asset,f'{asset}: expected one GetProcAddress, got {len(funcs)}')
    if funcs:
        params={p.get('name'):p for p in funcs[0].get('parameters',[])}
        mode=params.get('lookupMode',{})
        need(mode.get('insertValueMap',{}).get('name')=='{{procedureName}}','getproc-name-map-'+asset,f'{asset}: missing name map')
        need(mode.get('insertValueMap',{}).get('ordinal')=='MAKEINTRESOURCEA({{ordinal}})','getproc-ordinal-map-'+asset,f'{asset}: missing ordinal map')
        need(params.get('procedureName',{}).get('enabledWhen',{}).get('equals')=='name','getproc-name-condition-'+asset,f'{asset}: missing name condition')
        need(params.get('ordinal',{}).get('enabledWhen',{}).get('equals')=='ordinal','getproc-ordinal-condition-'+asset,f'{asset}: missing ordinal condition')
    need(not find(pack,'GetProcAddress by name'),'old-name-'+asset,f'{asset}: obsolete by-name card remains')
    need(not find(pack,'GetProcAddress by ordinal'),'old-ordinal-'+asset,f'{asset}: obsolete by-ordinal card remains')
# Lightweight emulation of mapped template rendering
def render(func,values):
    out=func.get('insertText','')
    for p in func.get('parameters',[]):
        name=p['name']; raw=values.get(name,p.get('defaultValue',''))
        cond=p.get('enabledWhen') or {}
        if cond.get('parameter') and values.get(cond['parameter'], next((x.get('defaultValue','') for x in func.get('parameters',[]) if x.get('name')==cond['parameter']),'')) != cond.get('equals'):
            raw=''
        raw=(p.get('insertValueMap') or {}).get(raw,raw)
        out=out.replace('{{'+name+'}}',raw)
    return out
c=json.loads((PACKS/'c_language_pack.json').read_text(encoding='utf-8'))
g=find(c,'GetProcAddress')[0]
need(render(g,{'module':'module','lookupMode':'name','procedureName':'"Plugin_Run"','ordinal':'7'})=='GetProcAddress(module, "Plugin_Run")','preview-name','name preview mismatch')
need(render(g,{'module':'module','lookupMode':'ordinal','procedureName':'"Plugin_Run"','ordinal':'7'})=='GetProcAddress(module, MAKEINTRESOURCEA(7))','preview-ordinal','ordinal preview mismatch')
result={'status':'ok' if not errors else 'error','version':'1.3.10','compileStatus':'not-run: Linux environment does not provide Visual Studio 2026, MSBuild or the Visual Studio SDK.','errors':errors}
out=ROOT/'docs/VISUALSTUDIO_DLL_HELPER_COMPATIBILITY_1.3.10_VALIDATION.json'
out.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2,ensure_ascii=False))
sys.exit(0 if not errors else 1)
