# Visual Pack Editor WPF minimal — JC Lib Visual Studio 0.7.0

## Objectif

Cette version introduit un premier éditeur de packs natif WPF. Il sert à valider l'édition JSON depuis Visual Studio avant de porter les fonctions avancées du Visual Pack Editor de l'extension VS Code.

## Périmètre

Le pack embarqué reste strictement en lecture seule. Seuls les fichiers JSON externes chargés depuis les emplacements suivants sont éditables :

```text
%LOCALAPPDATA%\JCLib\VisualStudio\Packs
<solution>\.jclib\packs
```

L'éditeur permet :

- la modification de `id`, `name` et `version` du pack ;
- la navigation Environment → Library → Category → Group → Element ;
- la modification des champs principaux d'un élément : `name`, `symbolKind`, `returnType`, `header`, `signature`, `declaration`, `insertText`, `description`, `longDescription` ;
- l'ajout d'un élément dans une catégorie ou un groupe ;
- la suppression d'un élément ;
- la validation avant sauvegarde ;
- la sauvegarde JSON formatée et le rechargement du navigateur principal.

## Préservation du JSON

L'éditeur utilise `Newtonsoft.Json.Linq.JObject`. Il modifie directement l'arbre JSON original au lieu de reconstruire le fichier à partir de DTO partiels. Les champs non encore édités graphiquement restent donc conservés, notamment :

```text
parameters
presets
options
isStatic
language
readOnly
propriétés spécifiques à certains packs
```

## Validation

La sauvegarde est bloquée lorsque :

- `id`, `name` ou `version` du pack est vide ;
- le pack ne contient aucun environnement ;
- un environnement, une bibliothèque, une catégorie, un groupe ou un élément porte un nom vide ;
- deux éléments d'une même catégorie ou d'un même groupe portent le même nom après `Trim()` et comparaison insensible à la casse.

Au moment de l'enregistrement, les espaces de début et de fin sont retirés des noms hiérarchiques et des noms d'éléments. Ce comportement prévient le retour du cas `Capture (...)` accompagné d'un espace invisible.

## Sauvegarde

La sauvegarde écrit d'abord un fichier temporaire :

```text
<pack>.jclib.tmp
```

puis remplace le fichier cible. Le navigateur principal recharge ensuite les catalogues et applique à nouveau la priorité :

```text
solution > global utilisateur > embarqué
```

## Dépendance ajoutée

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

Cette dépendance permet de conserver les propriétés JSON inconnues sans réintroduire la dépendance problématique à `System.Web`.
