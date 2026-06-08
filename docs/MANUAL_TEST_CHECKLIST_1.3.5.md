# Manual test checklist — JC Lib Visual Studio 1.3.5

1. Compiler la solution en `Release`.
2. Démarrer l’instance expérimentale.
3. Ouvrir ou importer `Assets\Packs\build_pack.json`.
4. Ouvrir `gcc compile object`.
5. Vérifier que le chemin include reste désactivé avant sélection de `-I`.
6. Sélectionner `-I`, renseigner `include`, puis vérifier la preview : `-I include`.
7. Vérifier l’absence de forme concaténée `-Iinclude`.
8. Ouvrir une fenêtre de choix structurée et cliquer sur `Appliquer` ; vérifier que la popup se ferme immédiatement.
