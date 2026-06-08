# Audit et refonte du pack LabWindows/CVI — phase 1

## Objectif

Remplacer la route historique `CVI Helpers` par un catalogue LabWindows/CVI autonome, exploitable par JC Lib VS Code et par le fallback de JC Lib Visual Studio. Le catalogue fourni par l'utilisateur a été conservé comme socle, corrigé, complété depuis les headers disponibles et enrichi avec des descriptions de paramètres et des listes guidées documentées.

## Versions

| Élément | Avant | Après |
|---|---:|---:|
| JC Lib VS Code | 0.7.60 | 0.7.61 |
| `default_pack.json` | 2.05.0 | 2.06.0 |
| Pack CVI autonome | absent | 1.1.0 |
| JC Lib Visual Studio sources | 1.3.1 | 1.3.2 |

## Évolution du pack CVI

| Mesure | Catalogue joint | Catalogue structuré |
|---|---:|---:|
| Cartes | 459 | 1977 |
| Fonctions | 459 | 1967 |
| Bibliothèques | 18 | 19 |
| Paramètres documentés | partiels | 7541 / 7541 |
| Fenêtres de sélection contextuelles | 0 | 102 |
| Sélecteurs multiples | 0 | 1 |
| Navigateurs fichier | partiels | 83 |
| Navigateurs dossier | partiels | 12 |
| Bibliothèques vides | 16 | 0 |

## Sections complétées

Les familles suivantes sont maintenant représentées : interface utilisateur, patterns CVI, Advanced Analysis, VXI, GPIB-488.2, RS232, TCP, UDP, Network Variable, DDE, ActiveX, connectivité DIAdem, streaming TDMS, interop .NET et Toolbox INI.

`NI-DAQmx` et `VISA` disposent volontairement d'une fiche de disponibilité plutôt que d'une fausse API : leurs headers sont fournis par des installations NI séparées du runtime CVI joint. La section Advanced Analysis est complétée avec le header `advanlys.h` disponible dans le bundle CVI2020 fourni au projet, car il est absent de l'archive CVI2012.

## Listes guidées ajoutées

Les commentaires et choix prédéfinis couvrent notamment les styles de contrôles CVI, les relations et directions d'arbres, les styles de tracés, les styles de points, les types de données graphiques, les couleurs, les filtres et fenêtres Advanced Analysis, la parité RS232, les stop bits, les événements RS232, les événements TCP/UDP et les modes de timeout UDP.

## Contrôles

```text
Parsing du pack CVI                                      OK
Bibliothèques vides                                       0
Catégories vides                                          0
Descriptions de paramètres manquantes                     0
Placeholders non résolus                                  0
Doublons exacts                                           0
Parsing des 21 JSON distribués                           OK
Audit transversal VS Code                                OK
Synchronisation extension.js / out/extension.js          OK
Route cvi_core                                         1977 cartes
Route c_all                                            2903 cartes
Route all_packs                                       14107 cartes
Validation statique Visual Studio 1.3.2                   OK
Compilation MSBuild Visual Studio                         non exécutée dans le conteneur Linux
```

## Limites

Les prototypes ont été contrôlés statiquement à partir des headers disponibles. Ils n'ont pas été compilés contre une installation LabWindows/CVI réelle dans cet environnement. Les sections NI-DAQmx et VISA nécessitent l'installation des drivers correspondants pour être complétées avec leurs APIs réelles.
