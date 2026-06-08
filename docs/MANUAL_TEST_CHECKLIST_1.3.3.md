# Checklist manuelle JC Lib Visual Studio 1.3.3

1. Compiler la solution en `Release`.
2. Lancer l'instance expérimentale Visual Studio.
3. Importer `Assets/Packs/build_pack.json`.
4. Ouvrir `gcc shared library`.
5. Vérifier que `includeDirectory` est désactivé au chargement.
6. Ouvrir le choix documenté de `includePathPrefix` et sélectionner `-I`.
7. Vérifier que `includeDirectory` devient éditable et reçoit `include` lorsqu'il était vide.
8. Vérifier que le navigateur de dossier `...` est disponible sur `includeDirectory`.
9. Sélectionner `-L` pour `libraryPathPrefix` et vérifier le déverrouillage de `libraryDirectory`.
10. Ouvrir le sélecteur multiple `linkedLibraries` et générer une valeur telle que `-lm -lpthread`.
11. Vérifier la génération finale : `-Iinclude`, `-Llib`, bibliothèques liées et options du linker sont insérés séparément.
12. Refaire le contrôle avec `g++ shared library` et `MinGW DLL import library`.
