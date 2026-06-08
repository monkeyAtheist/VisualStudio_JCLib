#!/usr/bin/env python3
from pathlib import Path
import json, re
ROOT=Path(__file__).resolve().parents[1]
pack=json.loads((ROOT/'src/JCLib.VisualStudio/Assets/Packs/build_pack.json').read_text(encoding='utf-8'))
def walk(x):
 if isinstance(x,dict):
  if 'insertText' in x: yield x
  for v in x.values(): yield from walk(v)
 elif isinstance(x,list):
  for v in x: yield from walk(v)
items=list(walk(pack))
targets=[i for i in items if '{{includePathPrefix}} {{includeDirectory}}' in str(i.get('insertText',''))]
errors=[]
if len(targets)!=9: errors.append(f'expected 9 spaced include templates, got {len(targets)}')
for item in targets:
 text=item['insertText']
 if '{{includePathPrefix}}{{includeDirectory}}' in text: errors.append(f"{item.get('name')}: concatenated include pair remains")
 if 'libraryPathPrefix' in text and '{{libraryPathPrefix}} {{libraryDirectory}}' not in text: errors.append(f"{item.get('name')}: spaced library pair missing")
print(json.dumps({'status':'ok' if not errors else 'error','packVersion':pack.get('version'),'cardsChecked':len(targets),'errors':errors},indent=2))
raise SystemExit(1 if errors else 0)
