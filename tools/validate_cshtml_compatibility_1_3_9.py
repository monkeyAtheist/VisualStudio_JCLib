#!/usr/bin/env python3
from __future__ import annotations
import json,re,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
PROJ=ROOT/'src/JCLib.VisualStudio'
PACKS=PROJ/'Assets/Packs'
errors=[]

def need(cond,kind,detail):
 if not cond: errors.append({'type':kind,'detail':detail})
# versions
manifest=(PROJ/'source.extension.vsixmanifest').read_text(encoding='utf-8')
package=(PROJ/'JCLibPackage.cs').read_text(encoding='utf-8')
xaml=(PROJ/'ToolWindows/PackEditorWindow.xaml').read_text(encoding='utf-8')
need('Version="1.3.9"' in manifest,'manifest-version','VSIX manifest must use 1.3.9')
need('"1.3.9"' in package,'package-version','InstalledProductRegistration must use 1.3.9')
need('1.3.9' in xaml,'editor-version','Visual Pack Editor title must use 1.3.9')
# csproj assets
csproj=(PROJ/'JCLib.VisualStudio.csproj').read_text(encoding='utf-8')
ET.parse(PROJ/'JCLib.VisualStudio.csproj')
for asset in ['default_pack.json','cvi_pack.json','build_pack.json','system_scripting_pack.json','web_language_pack.json','csharp_language_pack.json']:
 need((PACKS/asset).exists(),'missing-asset',asset)
 need(f'Assets\\Packs\\{asset}' in csproj,'missing-csproj-content',asset)
# service implementation
service=(PROJ/'Services/SnippetParameterService.cs').read_text(encoding='utf-8')
for token in ['EmptyQuotedHtmlAttributeRegex','NormalizeHtmlMarkupText','IsHtmlMarkupEntry','Razor / CSHTML','CSHTML markup','replacement = " " + replacement']:
 need(token in service,'missing-runtime-port',token)
# static model pack validation
csharp=json.loads((PACKS/'csharp_language_pack.json').read_text(encoding='utf-8'))
web=json.loads((PACKS/'web_language_pack.json').read_text(encoding='utf-8'))
need(csharp.get('version')=='1.3.0','csharp-pack-version',csharp.get('version'))
need(web.get('version')=='1.6.0','web-pack-version',web.get('version'))
env=next(e for e in csharp['environments'] if e['name']=='C# / .NET')
libs={l['name']:l for l in env['libraries']}
need('ASP.NET Core Razor / CSHTML' in libs,'missing-cshtml-library',sorted(libs))
def walk(groups):
 for g in groups or []:
  for fn in g.get('functions',[]) or []: yield fn
  yield from walk(g.get('groups',[]) or [])
cshtml=[]
for cat in libs.get('ASP.NET Core Razor / CSHTML',{}).get('categories',[]):
 cshtml += list(cat.get('functions',[]) or []) + list(walk(cat.get('groups',[]) or []))
need(len(cshtml)==205,'cshtml-card-count',len(cshtml))
attrs=[p for fn in cshtml for p in fn.get('parameters',[]) or [] if p.get('name')=='attributes']
need(len(attrs)==156,'cshtml-attributes-count',len(attrs))
for p in attrs:
 cfg=p.get('pickerConfig') or {}
 items=[x for sec in cfg.get('sections',[]) for group in sec.get('groups',[]) for x in group.get('items',[])]
 need(cfg.get('multiSelect') is True and len(items)==136,'attributes-picker','expected 136 multi-select choices')
# emulate normalization and spacing port
rx=re.compile(r'''\s+[A-Za-z_:][-A-Za-z0-9_:.]*=(?:"\s*"|'\s*')''')
footer=rx.sub('', '<footer id="" class="">Footer content</footer>')
need(footer=='<footer>Footer content</footer>','empty-html-normalization',footer)
fragment='title="Status" aria-label="Footer"'
if fragment and not fragment[0].isspace(): fragment=' '+fragment
need('<footer'+fragment+'>Footer content</footer>'=='<footer title="Status" aria-label="Footer">Footer content</footer>','attributes-spacing',fragment)
result={'status':'ok' if not errors else 'error','version':'1.3.9','distributedAssets':sorted(p.name for p in PACKS.glob('*.json')),'csharpPackVersion':csharp.get('version'),'webPackVersion':web.get('version'),'cshtmlCards':len(cshtml),'cshtmlAttributeFields':len(attrs),'emptyFooterPreview':footer,'errors':errors,'compileStatus':'not-run: Linux environment does not provide Visual Studio 2026, MSBuild or the Visual Studio SDK.'}
print(json.dumps(result,indent=2,ensure_ascii=False))
raise SystemExit(1 if errors else 0)
