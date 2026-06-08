# Checklist manuelle JC Lib Visual Studio 1.3.2

1. Compiler la solution en `Release`.
2. Lancer une instance expérimentale Visual Studio 2026.
3. Ouvrir JC Lib et vérifier la présence de l'environnement `CVI`.
4. Parcourir `User interface`, `Advanced Analysis library`, `RS232`, `TCP`, `UDP`, `ActiveX`, `DIAdem connectivity`, `TDM streaming` et `Toolbox`.
5. Ouvrir `NewCtrl` et vérifier la liste documentée des styles de contrôles.
6. Ouvrir une fonction RS232 avec `eventMask` et vérifier la sélection multiple.
7. Ouvrir `LoadPanel` ou `PlotBitmap` et vérifier le navigateur de fichiers.
8. Vérifier que NI-DAQmx et VISA affichent une fiche de disponibilité explicite plutôt qu'une section vide.
9. Importer `Assets/Packs/cvi_pack.json` comme pack externe pour confirmer le chargement autonome.
