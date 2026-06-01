# JC Lib Visual Studio 1.0.0

## Ajouts

- déplacement d'une bibliothèque vers un autre environnement ;
- déplacement d'une catégorie vers une autre bibliothèque ;
- déplacement d'un groupe vers une autre catégorie ou un autre groupe ;
- protection contre le déplacement d'un groupe dans son propre descendant ;
- duplication profonde d'un environnement, d'une bibliothèque, d'une catégorie ou d'un groupe ;
- export partiel `jclib.partial.v1` d'une bibliothèque, catégorie ou groupe ;
- import partiel dans un parent compatible ;
- conservation des fonctions, sous-groupes, paramètres structurés et propriétés JSON inconnues ;
- restauration de l'état développé du TreeView après les nouvelles opérations.

## Non-régression

- navigation multi-pack ;
- priorité solution > global > embarqué ;
- paramétrage dynamique des snippets ;
- insertion dans l'éditeur actif ;
- sélection multiple d'éléments ;
- duplication, réordonnancement, déplacement et suppression groupés ;
- validation des doublons après normalisation des espaces.
