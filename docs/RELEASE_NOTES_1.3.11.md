# Release notes 1.3.11

## Hiérarchie visuelle WPF du navigateur

La navigation principale et le Visual Pack Editor disposent maintenant d'une grammaire visuelle commune inspirée de l'explorateur JC Lib VS Code.

Chaque niveau possède :

- une icône compacte colorée ;
- un badge explicite (`pack`, `env`, `lib`, `cat`, `group`) ;
- un compteur distinct pour les nœuds conteneurs ;
- une info-bulle rappelant le rôle du nœud et son chemin.

Les éléments terminaux utilisent leur nature réelle lorsque le catalogue la fournit : `fn`, `method`, `macro`, `cmd`, `snippet`, `kw`, `class`, `struct`, `enum`, `tag`, etc.

## Palette configurable

La fenêtre `Apparence` expose une section dédiée à la hiérarchie :

- racine du catalogue ;
- pack ;
- environnement ;
- bibliothèque ;
- catégorie ;
- groupe ;
- élément terminal ;
- fond des badges ;
- texte des icônes.

Les préférences existantes restent compatibles. Lorsqu'un fichier local antérieur ne contient pas ces propriétés, les couleurs accessibles par défaut sont injectées automatiquement.

## Périmètre

Les catalogues JSON distribués restent identiques à ceux de la version 1.3.10. La génération des packs intégrés reste exclusivement réservée à l'extension VS Code. Cette livraison porte uniquement l'interface WPF Visual Studio 2026.
