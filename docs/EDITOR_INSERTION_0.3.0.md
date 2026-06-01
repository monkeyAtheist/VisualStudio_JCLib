# JC Lib Visual Studio 0.3.0 — insertion dans l'éditeur

Cette étape ajoute l'insertion native des snippets dans l'éditeur actif Visual Studio.

## API utilisée

La Tool Window récupère le dernier éditeur de code actif avec `SVsTextManager` et `IVsTextManager.GetActiveView(0, null, out view)`. Le paramètre `fMustHaveFocus = 0` est volontaire : au moment où l'utilisateur clique sur le bouton d'insertion, le focus appartient à la Tool Window et non plus à l'éditeur.

Le texte est modifié avec `IVsTextLines.ReplaceLines()`. Cette API remplace la sélection active lorsqu'elle existe et insère autrement le texte à la position du curseur.

## Indentation

L'indentation initiale de la ligne active est lue avant l'insertion. Elle est préfixée à chaque ligne suivante du snippet, sans modifier l'indentation relative déjà présente dans le snippet.

## Cas gérés

- insertion simple au curseur ;
- remplacement de la sélection active ;
- sélection inversée ;
- conservation des tabulations ou espaces de début de ligne ;
- message explicite si aucun éditeur texte n'est ouvert ;
- message explicite lorsqu'un élément ne contient pas de snippet.

## Espace virtuel

Les colonnes renvoyées par la vue peuvent dépasser la longueur physique de la ligne lorsque le curseur est placé dans l’espace virtuel de l’éditeur. Le service borne donc les coordonnées à la longueur réelle de la ligne avant l’appel à `ReplaceLines()`.
