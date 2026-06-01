# JC Lib Visual Studio 0.9.1 — conservation de l’état du TreeView

## Symptôme

Dans le Visual Pack Editor 0.9.0, une reconstruction du TreeView pouvait refermer les parents développés après une modification ou une sauvegarde. La navigation dans un pack volumineux devenait inutilement répétitive.

## Correctif

- Ajout de `IsExpanded` et `IsSelected` dans `PackEditorNode`.
- Liaison TwoWay de ces propriétés avec `TreeViewItem`.
- Capture de l’état visuel avant chaque `RebuildTree()`.
- Restauration par identité stable `JObject`, y compris après réordonnancement.
- Restauration du nœud sélectionné et développement automatique de ses ancêtres.
- Sélection automatique du nœud nouvellement créé après un ajout.

Le format JSON des packs ne change pas. L’état visuel reste uniquement en mémoire pendant l’ouverture de l’éditeur.
