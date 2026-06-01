# JC Lib Visual Studio 1.0.0 — sous-arbres et fragments partiels

La version 1.0.0 complète le Visual Pack Editor WPF avec trois capacités destinées à maintenir des packs volumineux sans éditer manuellement leur JSON.

## Déplacement de sous-arbres

Le bouton `Changer de parent` conserve son comportement historique pour les éléments cochés. Lorsqu'aucun élément n'est coché et que le nœud courant est structurel, il déplace désormais le sous-arbre complet :

- une bibliothèque vers un autre environnement ;
- une catégorie vers une autre bibliothèque ;
- un groupe vers une autre catégorie ou un autre groupe.

Un groupe ne peut pas être déplacé dans lui-même ni dans l'un de ses descendants. Le déplacement conserve les fonctions, sous-groupes, paramètres structurés et propriétés JSON spécialisées.

## Duplication de sous-arbres

Le bouton `Dupliquer le sous-arbre` clone profondément un environnement, une bibliothèque, une catégorie ou un groupe. Le nom de la copie est rendu unique automatiquement :

```text
Mesures
Mesures Copy
Mesures Copy2
```

## Fragments JC Lib partiels

Le bouton `Exporter le sous-arbre` génère un fichier `*.jclib-fragment.json` pour une bibliothèque, une catégorie ou un groupe.

Le format est explicite :

```json
{
  "format": "jclib.partial.v1",
  "kind": "group",
  "exportedFromPack": "jclib.example",
  "exportedAtUtc": "2026-06-01T00:00:00.0000000Z",
  "node": {
    "name": "Diagnostics",
    "functions": [],
    "groups": []
  }
}
```

Le bouton `Importer un fragment` insère ensuite le sous-arbre sous un parent compatible :

| Fragment | Parent cible |
|---|---|
| `library` | environnement |
| `category` | bibliothèque |
| `group` | catégorie ou groupe |

Le nom importé est rendu unique sans écraser le contenu existant.

## Compatibilité

La structure des packs complets ne change pas. Les packs créés avec les versions précédentes restent lisibles. Le format de fragment est volontairement séparé afin d'éviter de confondre un pack installable avec une exportation partielle.
