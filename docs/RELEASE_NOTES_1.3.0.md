# JC Lib Visual Studio 1.3.0 — paramètres structurés JC Lib 0.7.60

## Objet

Cette version adapte l'extension Visual Studio aux packs restructurés produits par JC Lib VS Code `0.7.60`.

## Ajouts

- lecture des options simples ou documentées ;
- prise en charge de `description`, `placeholder`, `optional` et `pickerConfig` ;
- rendu des templates `{{parameter}}` pour les fonctions et les symboles paramétrables non appelables ;
- fenêtre WPF de sélection structurée simple ou multiple ;
- combinaison des valeurs avec `pickerConfig.valueSeparator` ;
- prise en charge de `emptyValue` et des choix volontairement vides documentés ;
- correction des guillemets ajoutés par les navigateurs de fichiers ou dossiers ;
- Visual Pack Editor enrichi pour conserver et éditer les métadonnées avancées ;
- fallback embarqué mis à jour vers `default_pack.json` `2.05.0`.

## Validation

Les scripts suivants sont fournis :

```text
tools/validate_scaffold.py
tools/validate_advanced_parameter_compatibility.py
tools/validate_catalog_directory_compatibility.py
tools/build_vs2026_release.ps1
```

Le dernier script doit être exécuté sous Windows avec Visual Studio 2026 et la charge de travail **Visual Studio extension development**.
