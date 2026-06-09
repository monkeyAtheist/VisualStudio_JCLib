# Visual Studio 2026 manual test checklist — 1.3.12

1. Compiler le VSIX sous Windows avec `tools/build_vs2026_release.ps1`.
2. Installer le VSIX puis redémarrer Visual Studio.
3. Appuyer sur `Ctrl+Alt+P` depuis l'éditeur : la fenêtre `Find Symbol` doit s'ouvrir.
4. Vérifier que le curseur est directement placé dans la zone de saisie.
5. Vérifier qu'aucun résultat et aucune preview ne sont affichés avant la première saisie.
6. Saisir `GetProcAddress` puis vérifier que le premier résultat et sa preview apparaissent.
7. Appuyer sur Entrée : l'élément doit s'ouvrir dans le navigateur JC Lib.
8. Vérifier que `Échap` ferme la fenêtre.
9. Ouvrir Outils > Options > Environnement > Clavier et vérifier que le raccourci peut être modifié.
10. Vérifier que le bouton `Find Symbol` de la Tool Window ouvre la même fenêtre.
