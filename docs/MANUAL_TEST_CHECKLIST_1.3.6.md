# Manual test checklist — JC Lib Visual Studio 1.3.6

1. Compiler la solution en Release puis lancer l'instance expérimentale.
2. Vérifier que l'arborescence principale commence par Library sous chaque pack, sans conteneur General visible.
3. Importer `lua_pack.json` et vérifier les quatre bibliothèques racines Lua.
4. Importer `build_pack.json`, ouvrir `gcc compile object`, puis sélectionner plusieurs warnings.
5. Vérifier que `-Wall -Wextra` reste présent après application.
6. Effacer tous les warnings, appliquer, puis vérifier la restauration du jeu par défaut.
7. Ouvrir une fiche MSVC et vérifier que `/W3`, `/W4` et `/Wall` se grisent mutuellement.
8. Créer un nouveau pack : vérifier qu'aucun langage ni environnement n'est demandé.
9. Ouvrir le Visual Pack Editor : vérifier que `+ Bibliothèque` est disponible directement au niveau du pack.
