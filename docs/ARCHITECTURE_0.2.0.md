# JC Lib Visual Studio 0.2.0 — navigateur de catalogue

## Flux de chargement

```text
Tool Window WPF
  -> CatalogLoader.LoadDefaultCatalog()
  -> ressource embarquée JCLib.VisualStudio.Assets.Packs.default_pack.json
     ou fallback disque Assets/Packs/default_pack.json
  -> désérialisation JavaScriptSerializer
  -> modèle CatalogNode / CatalogEntry
  -> TreeView WPF + index de recherche
```

## Fonctionnalités

- navigation Pack -> Environment -> Library -> Category -> Group -> Element ;
- prise en charge des groupes imbriqués ;
- recherche multi-token sur nom, chemin, prototype, description et snippet ;
- panneau de prévisualisation ;
- copie du snippet dans le presse-papiers ;
- rechargement du catalogue.

## Limites volontaires

Cette version reste en lecture seule. Elle ne modifie pas les packs et n'insère pas encore le snippet dans l'éditeur Visual Studio actif.
