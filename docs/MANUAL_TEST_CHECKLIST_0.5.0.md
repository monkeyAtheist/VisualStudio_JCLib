# Checklist manuelle — JC Lib Visual Studio 0.5.0

## Régression du socle

- [ ] Compiler en `Debug | Any CPU`.
- [ ] Lancer avec `F5`.
- [ ] Ouvrir `Affichage > Autres fenêtres > JC Lib`.
- [ ] Vérifier le chargement du catalogue embarqué.
- [ ] Rechercher `LoadPanel` et vérifier le paramétrage dynamique.
- [ ] Insérer un snippet dans l'éditeur actif.

## Pack global utilisateur

- [ ] Cliquer sur `Importer un pack global`.
- [ ] Choisir `docs/example_packs/jclib_sample_pack.json`.
- [ ] Vérifier que le résumé affiche deux packs chargés.
- [ ] Rechercher `jc_sample_log`.
- [ ] Vérifier la provenance `JC Lib Sample Pack v1.0.0 — global utilisateur`.
- [ ] Cliquer sur `Ouvrir les packs globaux` et vérifier le dossier `%LOCALAPPDATA%\JCLib\VisualStudio\Packs`.

## Rechargement automatique

- [ ] Laisser cochée l'option `Rechargement automatique`.
- [ ] Modifier la description ou le snippet du pack JSON global.
- [ ] Sauvegarder le fichier.
- [ ] Vérifier le message de détection puis le rechargement automatique après le délai de debounce.
- [ ] Décocher l'option, modifier à nouveau le JSON et vérifier qu'aucun rechargement automatique ne se produit.
- [ ] Cliquer sur `Recharger` et vérifier la mise à jour manuelle.

## Pack propre à une solution

- [ ] Sans solution ouverte, cliquer sur `Importer pour la solution` et vérifier le message explicatif.
- [ ] Ouvrir une solution Visual Studio.
- [ ] Cliquer sur `Importer pour la solution`.
- [ ] Choisir le pack d'exemple.
- [ ] Vérifier la création de `<solution>\.jclib\packs`.
- [ ] Vérifier l'apparition du pack de provenance `solution`.

## Diagnostics de conflit

- [ ] Importer le même pack d'exemple au niveau global et au niveau solution.
- [ ] Déplier `Sources et diagnostics`.
- [ ] Vérifier la détection de l'identifiant de pack dupliqué.
- [ ] Vérifier la détection du chemin logique `Sample > Utilities > Imported pack demo > jc_sample_log` dupliqué.

## JSON invalide

- [ ] Copier un fichier `.json` invalide dans le dossier des packs globaux.
- [ ] Vérifier que le pack embarqué et les packs valides restent disponibles.
- [ ] Vérifier que l'erreur est listée dans `Sources et diagnostics`.
