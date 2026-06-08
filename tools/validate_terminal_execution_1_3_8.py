#!/usr/bin/env python3
from __future__ import annotations
import json, re, sys
from pathlib import Path
from xml.etree import ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
PROJ=ROOT/'src/JCLib.VisualStudio'
errors=[]
def expect(cond,msg):
 if not cond: errors.append(msg)

xaml=(PROJ/'ToolWindows/JCLibToolWindowControl.xaml').read_text(encoding='utf-8')
cs=(PROJ/'ToolWindows/JCLibToolWindowControl.xaml.cs').read_text(encoding='utf-8')
csproj=(PROJ/'JCLib.VisualStudio.csproj').read_text(encoding='utf-8')
manifest=(PROJ/'source.extension.vsixmanifest').read_text(encoding='utf-8')
package=(PROJ/'JCLibPackage.cs').read_text(encoding='utf-8')
ET.parse(PROJ/'ToolWindows/JCLibToolWindowControl.xaml')
ET.parse(PROJ/'source.extension.vsixmanifest')
expect('x:Name="ExecuteSnippetButton"' in xaml,'missing ExecuteSnippetButton')
expect('Click="OnExecuteSnippetClick"' in xaml,'execute button click handler missing')
for token in ['IsTerminalExecutableEntry','IsPotentiallyDestructiveTerminalText','BuildConsoleStartInfo','ExecuteSelectedSnippetInConsole','OnExecuteSnippetClick','Process.Start(BuildConsoleStartInfo']:
 expect(token in cs,f'missing C# terminal token {token}')
expect('Assets\\Packs\\system_scripting_pack.json' in csproj,'standalone scripting pack is not included in VSIX content')
expect('Version="1.3.8"' in manifest,'manifest version mismatch')
expect('"1.3.8"' in package,'InstalledProductRegistration version mismatch')
pack=json.loads((PROJ/'Assets/Packs/system_scripting_pack.json').read_text(encoding='utf-8'))
expect(pack.get('version')=='1.9.0',f'system scripting pack version mismatch: {pack.get("version")!r}')
# find required SSH cards
cards=[]
def walk(groups):
 for g in groups or []:
  cards.extend(g.get('functions',[]) or [])
  walk(g.get('groups',[]))
for env in pack.get('environments',[]):
 for lib in env.get('libraries',[]):
  for cat in lib.get('categories',[]):
   cards.extend(cat.get('functions',[]) or [])
   walk(cat.get('groups',[]))
names={c.get('name') for c in cards}
for required in ['ssh login with password prompt','ssh remote command','ssh-copy-id install public key','Reference — SSH password login and secret handling','ssh inspect resolved alias','Template — OpenSSH alias with identity file','Recipe — bootstrap SSH alias and Ed25519 key (PowerShell)','Reference — local hosts alias versus SSH Host alias','GetEnvironmentVariable scoped','Add Windows hosts alias']:
 expect(required in names,f'missing SSH card {required!r}')
expect('password' not in next(c for c in cards if c.get('name')=='ssh login with password prompt').get('insertText','').lower(),'password leaked into generated SSH command')
summary={'visualStudioSourceVersion':'1.3.8','systemScriptingPackVersion':pack.get('version'),'executeButton':True,'externalConsoleRouting':True,'integratedPackGenerationAdded':False,'errors':errors}
print(json.dumps(summary,indent=2,ensure_ascii=False))
if errors: sys.exit(1)
