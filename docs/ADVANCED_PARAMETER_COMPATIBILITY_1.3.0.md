# JC Lib Visual Studio 1.3.0 — compatibilité des paramètres structurés

Cette version adapte le portage Visual Studio aux catalogues produits par JC Lib VS Code `0.7.60`.

## Schéma reconnu

Les paramètres JSON acceptent désormais :

```text
name
type
description
editorType
defaultValue
placeholder
optional
presets: string[] | documented choice[]
options: string[] | documented choice[]
pickerConfig
```

Une option documentée peut contenir :

```text
value
constant
label
description
detail
defaultValue
sourceTypes
```

La fenêtre structurée lit les sections, groupes et éléments de `pickerConfig`. Elle gère `multiSelect`, `valueSeparator`, `emptyValue` et les descriptions contextuelles.

## Génération des snippets

Les templates `{{parameter}}` sont remplacés avant insertion. Cette logique s'applique aux appels directs mais aussi aux déclarations, structures, classes, commandes et snippets multi-lignes. Pour un appel simple possédant une valeur de retour, la variable de retour facultative reste disponible.

## Navigateurs fichier et dossier

Le navigateur détecte si le template contient déjà les guillemets autour de `{{parameter}}`. Il évite ainsi de produire des doubles guillemets lors de l'insertion d'un chemin.

## Validation locale

Le script `tools/validate_advanced_parameter_compatibility.py` contrôle le fallback embarqué, le pack de test avancé, les hooks sources et la structure XML. La compilation VSIX doit être exécutée sous Windows avec Visual Studio 2026 et la charge de travail **Visual Studio extension development**.

## Validation de tous les packs JC Lib VS Code 0.7.60

Le script générique suivant valide un dossier contenant les exports JSON :

```powershell
python tools/validate_catalog_directory_compatibility.py C:\chemin\vers\data --output catalog-report.json
```

Il contrôle les listes simples et documentées, les fenêtres structurées, les listes multiples, les navigateurs, les choix volontairement vides et les placeholders de templates.

## Compilation Windows Visual Studio 2026

```powershell
powershell -ExecutionPolicy Bypass -File tools\build_vs2026_release.ps1 -Configuration Release
```

Le script localise MSBuild via `vswhere.exe`, restaure les packages, compile la solution et affiche le chemin du VSIX généré.
