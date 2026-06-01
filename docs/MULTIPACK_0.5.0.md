# Chargement multi-pack — JC Lib Visual Studio 0.5.0

## Sources prises en charge

La Tool Window agrège trois niveaux de catalogue :

1. le pack embarqué `Assets/Packs/default_pack.json` ;
2. les packs globaux utilisateur présents dans `%LOCALAPPDATA%\JCLib\VisualStudio\Packs` ;
3. les packs propres à la solution présents dans `<solution>\.jclib\packs`.

Le pack embarqué reste obligatoire et sert de socle. Les fichiers JSON externes invalides sont isolés : ils apparaissent dans le panneau `Sources et diagnostics` sans empêcher l'utilisation des autres packs.

## Détection des conflits

Deux familles de conflits sont affichées :

- identifiant `id` de pack dupliqué ;
- chemin logique d'élément dupliqué entre plusieurs packs.

La version 0.5.0 ne supprime pas automatiquement les doublons. Les éléments restent visibles avec leur provenance afin de permettre un diagnostic sans perte de données. Une politique d'override explicite pourra être ajoutée dans une version ultérieure.

## Rechargement automatique

Les répertoires globaux et solution sont surveillés avec `FileSystemWatcher`. Les notifications multiples sont regroupées par un délai de 650 ms avant rechargement afin d'éviter plusieurs désérialisations lors d'une sauvegarde unique.

## Solution active

Le chemin de solution est récupéré via le service global `SVsSolution` et l'appel `IVsSolution.GetSolutionInfo(...)`. Lorsqu'aucune solution n'est ouverte, les opérations de niveau solution restent désactivées fonctionnellement et expliquent la cause dans la barre d'état.
