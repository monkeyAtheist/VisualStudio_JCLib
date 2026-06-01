# Visual Pack Editor WPF avancé — JC Lib Visual Studio 0.8.0

## Objectif

Cette version étend l'éditeur minimal 0.7.0 afin de permettre la création d'un pack exploitable sans modifier manuellement le JSON.

## Création et duplication

La Tool Window principale expose :

```text
Créer un pack
Dupliquer le pack
```

La création génère un pack de démarrage contenant :

```text
Custom
└── Custom Library
    └── General
```

La duplication accepte aussi le pack embarqué. La copie est enregistrée dans un dossier externe global ou lié à la solution et devient éditable.

## Édition hiérarchique

Le Visual Pack Editor permet maintenant :

```text
+ Environnement
+ Bibliothèque
+ Catégorie
+ Groupe
+ Élément
Supprimer le nœud
Supprimer l'élément
```

La suppression d'un nœud hiérarchique est récursive après confirmation. Le renommage se fait dans le panneau de droite.

## Paramètres structurés

Chaque élément dispose d'un panneau `Paramètres structurés` :

```text
nom
type
editorType
defaultValue
presets
options
```

Les listes `presets` et `options` acceptent une valeur par ligne. Elles restent compatibles avec le moteur de génération dynamique déjà présent dans le navigateur.

Lorsqu’un environnement, une bibliothèque ou une catégorie est renommé, les métadonnées dénormalisées des éléments descendants sont synchronisées avant sauvegarde afin que le chemin affiché dans le navigateur reste cohérent.

## Validation

La sauvegarde est bloquée pour :

```text
métadonnée obligatoire vide
environnement dupliqué
bibliothèque dupliquée dans un environnement
catégorie dupliquée dans une bibliothèque
groupe dupliqué dans un même parent
élément dupliqué dans un même parent
paramètre dupliqué dans un même élément
nom vide après normalisation
```

Les espaces invisibles de début et de fin sont supprimés au moment de la sauvegarde.
