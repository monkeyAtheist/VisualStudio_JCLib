# Visual Pack Editor WPF — opérations groupées — JC Lib Visual Studio 0.9.0

## Objectif

La version 0.9.0 rapproche l'éditeur Visual Studio de l'éditeur VS Code en ajoutant les opérations de masse utiles pour maintenir des packs importants.

## Fonctions ajoutées

### Sélection multiple

Les éléments possèdent une case à cocher dans l'arborescence. Les nœuds structurels restent sélectionnés de façon classique afin de conserver une lecture claire de la hiérarchie.

### Duplication

`Dupliquer` clone chaque élément sélectionné avec l'ensemble de ses propriétés JSON, notamment les paramètres structurés et les propriétés spécialisées inconnues de l'interface. Les noms sont rendus uniques avec le suffixe `Copy`, `Copy2`, etc.

### Réordonnancement

`Monter` et `Descendre` déplacent les éléments cochés d'un cran dans leur tableau JSON respectif. Un bloc de plusieurs éléments adjacents reste cohérent. En l'absence de sélection groupée, le bouton agit sur le nœud courant, y compris sur un environnement, une bibliothèque, une catégorie ou un groupe.

### Changement de parent

`Changer de parent` ouvre une fenêtre listant toutes les catégories et tous les groupes du pack. Les éléments sélectionnés sont déplacés vers le tableau `functions` de la destination choisie. Leurs métadonnées `environment`, `library` et `category` sont synchronisées avant sauvegarde.

### Suppression groupée

`Supprimer la sélection` retire les éléments cochés après confirmation. Les nœuds hiérarchiques continuent d'utiliser le bouton existant `Supprimer le nœud`.

## Conservation du format JSON

Les déplacements utilisent les objets `JObject` existants. Les propriétés inconnues restent donc conservées. La sauvegarde continue de passer par un fichier temporaire `.jclib.tmp` avant remplacement du JSON cible.

## Portée volontaire

Cette version traite les opérations groupées sur les éléments. Le déplacement complet d'une bibliothèque, d'une catégorie ou d'un sous-arbre vers un autre parent pourra être ajouté lors d'une passe ultérieure.
