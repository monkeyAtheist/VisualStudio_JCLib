# Checklist manuelle — JC Lib Visual Studio 1.3.0

1. Exécuter `powershell -ExecutionPolicy Bypass -File tools\build_vs2026_release.ps1 -Configuration Release`, ou compiler `JCLib.VisualStudio.sln` en `Release | Any CPU` sous Visual Studio 2026.
2. Lancer l'instance expérimentale et ouvrir `Affichage > Autres fenêtres > JC Lib`.
3. Importer `docs/example_packs/jclib_advanced_parameter_pack.json` comme pack global utilisateur.
4. Ouvrir `configure_device` et vérifier l'affichage des trois descriptions de paramètres.
5. Cliquer sur `...` pour `path` et sélectionner un fichier. Vérifier que le snippet ne contient pas de doubles guillemets.
6. Cliquer sur `Choisir...` pour `mode`, sélectionner `MODE_FAST` et vérifier l'aperçu.
7. Cliquer sur `Choix multiples...` pour `flags`, cocher `FLAG_LOG` et `FLAG_RETRY`, puis vérifier `FLAG_LOG | FLAG_RETRY`.
8. Saisir une variable de retour et vérifier la génération `result = configure_device(...);`.
9. Ouvrir `structured declaration`, modifier `name` et `fields`, puis vérifier le remplacement des placeholders hors fonction.
10. Ouvrir `optional fragment`, sélectionner `No optional flag`, puis vérifier que la valeur vide n'est pas remplacée par un identifiant artificiel. Répéter après avoir cliqué sur `Effacer`.
11. Ouvrir le Visual Pack Editor sur le pack de test et vérifier l'édition de `description`, `placeholder`, `optional`, des options documentées et de `pickerConfig`.
12. Recharger les catalogues, fermer puis rouvrir la fenêtre afin de vérifier la persistance normale de l'extension.
