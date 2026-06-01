# Éditeur de paramètres — JC Lib Visual Studio 0.4.0

La version 0.4.0 exploite directement le tableau `parameters` déjà présent dans les packs JC Lib.

## Types d'éditeur pris en charge

- `text` : saisie libre ;
- `handle` : saisie libre avec valeurs usuelles ;
- `boolean` : saisie avec suggestions `0` et `1` ;
- `pathFile` : saisie ou sélection d'un fichier ;
- `pathFolder` : saisie ou sélection d'un dossier.

## Génération de l'appel

Pour une entrée `symbolKind = function`, la prévisualisation est reconstruite sous la forme :

```text
FunctionName(argument1, argument2, ...);
```

Les valeurs initiales sont extraites de `insertText` lorsque celui-ci contient déjà un appel. Les valeurs absentes sont inférées à partir du nom, du type et de l'éditeur du paramètre.

Pour une fonction non `void`, une variable de retour optionnelle peut être renseignée :

```text
result = FunctionName(argument1, argument2, ...);
```

Pour les snippets, mots-clés et déclarations qui ne sont pas des fonctions, `insertText` reste utilisé sans transformation.
