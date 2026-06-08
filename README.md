## Version 1.3.9 — compatibilité Web HTML et ASP.NET Core Razor / CSHTML

- Synchronisation des catalogues autonomes Web 1.6.0 et C# / ASP.NET Core Razor CSHTML 1.3.0.
- Ajout des catalogues `web_language_pack.json` et `csharp_language_pack.json` aux ressources distribuées pour import explicite.
- Portage de la suppression des attributs HTML / CSHTML cités laissés vides (`id=""`, `class=""`, etc.).
- Portage de l'espacement automatique du fragment libre `attributes`.
- Conservation du rôle Visual Studio : import, lecture et insertion ; génération des packs intégrés réservée à VS Code.

## Version 1.3.8 — environnement, alias réseau et alias OpenSSH

Cette mise à jour synchronise le fallback `Scripting / System` 1.9.0 avec les commandes de chemins et variables d’environnement, la gestion des alias locaux `hosts`, les alias OpenSSH associés à une clé privée locale et les recettes d’enrôlement de clé publique. Le bouton `Exécuter dans une console` conserve ses confirmations avant les modifications persistantes. La génération des packs intégrés reste réservée à VS Code.

## Version 1.3.7 — exécution console et compatibilité Scripting / System 1.8.0

- Ajout du bouton `Exécuter dans une console` pour les cartes `command` et les recettes `Scripting / System`.
- Ouverture d'une console externe adaptée : PowerShell pour `PowerShell 7`, WSL/Bash lorsque disponible pour les bibliothèques Linux et CMD pour les autres cartes.
- Ajout du catalogue autonome `system_scripting_pack.json` 1.8.0 pour import explicite.
- La génération des packs intégrés reste réservée à l'extension VS Code.

## Version 1.3.6 — navigation library-first et validation des multi-sélections

Cette version synchronise JC Lib VS Code `0.7.66`, le pack Build `1.2.3` et le fallback `2.10.0`. Les packs library-first sont importables directement. Les anciens conteneurs d'environnement restent compatibles mais ne sont plus imposés comme premier niveau visible. Les sélecteurs multiples prennent en charge un nombre minimal de choix, une valeur par défaut restaurée à l'application et le grisement des valeurs incompatibles.

## Version 1.3.5 — espacement explicite des arguments Build

Cette version synchronise le pack Build `1.2.2`. Les arguments optionnels de recherche GCC compatibles sont rendus sous forme de tokens séparés : `-I <directory>` et `-L <directory>`. La boîte WPF de choix structurés fermait déjà la fenêtre au clic sur `Appliquer`.

## Version 1.3.4 — aperçu des cartes Build sous forme de commandes

Les cartes Build utilisent désormais `symbolKind: command`. Les previews GCC, G++, Clang, MinGW, CMake, Make, Ninja et outils associés sont rendues comme des lignes de commande ou des instructions CMake, jamais comme de faux appels de fonctions C.

## Version 1.3.3 — paramètres Build conditionnels

Le fallback embarqué utilise maintenant `default_pack.json` 2.07.0. Le catalogue autonome `Assets/Packs/build_pack.json` distingue les chemins de headers `-I`, les chemins de bibliothèques `-L`, les bibliothèques liées `-lfoo` et les options du linker. Les champs de dossier associés à `-I` et `-L` sont désactivés tant que le préfixe correspondant n'est pas sélectionné.

# JC Lib — Visual Studio 1.3.1

Portage Visual Studio de JC Lib sous la forme d'une Tool Window WPF ancrable, avec navigateur multi-pack, insertion de snippets, accès rapides, couleurs configurables et Visual Pack Editor WPF avancé.

## Ergonomie quotidienne ajoutée en 1.1.0

- raccourci global `Ctrl+Alt+J` pour ouvrir JC Lib ;
- entrée `JC Lib` dans le menu contextuel de l'éditeur de code ;
- favoris persistants ;
- historique des éléments récemment copiés ou insérés ;
- filtres par environnement et bibliothèque ;
- filtre `Favoris uniquement` ;
- insertion rapide avec les valeurs par défaut ;
- double-clic dans les recherches, favoris ou récents pour insérer rapidement ;
- conservation de la dernière recherche, des filtres et du dernier élément sélectionné ;
- panneau `Apparence` avec couleurs configurables ;
- correction explicite du texte noir sur fond sombre dans les arbres WPF ;
- propagation du thème au navigateur, au Visual Pack Editor et aux boîtes de dialogue associées.

Les préférences sont stockées localement dans :

```text
%LOCALAPPDATA%\JCLib\VisualStudio\user-preferences.json
```

## Fonctionnalités déjà disponibles

- catalogue embarqué `Assets/Packs/default_pack.json` ;
- packs JSON globaux utilisateur dans `%LOCALAPPDATA%\JCLib\VisualStudio\Packs` ;
- packs JSON propres à une solution dans `<solution>\.jclib\packs` ;
- import, création et duplication de packs externes ;
- rechargement manuel et surveillance automatique des fichiers JSON ;
- activation, désactivation, suppression et ouverture du dossier d'un pack externe ;
- priorité explicite `solution > global utilisateur > embarqué` ;
- diagnostics des fichiers invalides, conflits et entrées masquées ;
- navigation Root → Pack → Environment → Library → Category → Group → Element ;
- recherche multi-mots incluant la provenance du pack ;
- prévisualisation, génération dynamique et insertion directe des snippets ;
- remplacement de la sélection et conservation de l'indentation ;
- Visual Pack Editor WPF pour les packs globaux et solution ;
- création, renommage et suppression d'environnements, bibliothèques, catégories et groupes ;
- ajout, duplication et suppression d'éléments ;
- sélection multiple, suppression groupée et changement de parent ;
- réordonnancement vers le haut ou vers le bas ;
- conservation des branches développées et de la sélection après reconstruction du TreeView ;
- déplacement et duplication de sous-arbres ;
- export et import partiels via `*.jclib-fragment.json` ;
- édition graphique des paramètres structurés ;
- validation avant sauvegarde et normalisation des espaces invisibles ;
- sauvegarde JSON non destructive.

## Test rapide

1. Installer la charge de travail Visual Studio Extension Development.
2. Ouvrir `JCLib.VisualStudio.sln`.
3. Compiler en `Debug | Any CPU`.
4. Lancer avec `F5`.
5. Dans l'instance expérimentale, ouvrir JC Lib avec `Ctrl+Alt+J` ou `Affichage > Autres fenêtres > JC Lib`.
6. Cliquer sur `Apparence`, puis vérifier le thème sombre accessible.
7. Rechercher `QTimer singleShot`, ajouter l'élément aux favoris puis effectuer une insertion rapide.
8. Fermer et rouvrir JC Lib pour vérifier la persistance de la recherche, des filtres et des favoris.
9. Ouvrir un pack externe dans le Visual Pack Editor et vérifier la lisibilité de l'arborescence.

Voir `docs/MANUAL_TEST_CHECKLIST_1.3.1.md` pour les cas détaillés.


## Version 1.2.0 — listes déroulantes et packs externes uniquement

- couleurs dédiées au fond et au texte des listes déroulantes ;
- option `Inclure le pack embarqué (fallback)` désactivée par défaut ;
- utilisation normale possible avec les seuls packs exportés depuis l’extension VS Code ;
- fallback embarqué conservé dans le VSIX pour récupération ou premier démarrage.


## Version 1.3.1 — correctif de compilation et compatibilité avec les packs structurés JC Lib 0.7.60

- lecture des paramètres enrichis `description`, `placeholder`, `optional` et `pickerConfig` ;
- prise en charge des options simples ou documentées (`value`, `label`, `description`, `detail`) ;
- rendu des templates paramétrés `{{parameter}}` pour les fonctions, méthodes, commandes, snippets, classes, structures et déclarations ;
- fenêtre WPF dédiée aux choix structurés et aux sélections multiples ;
- combinaison des choix multiples selon `pickerConfig.valueSeparator` et prise en charge de `emptyValue` ;
- conservation des navigateurs de fichier et de dossier avec adaptation des guillemets lorsque le template les fournit déjà ;
- Visual Pack Editor enrichi : description, placeholder, paramètre facultatif, options documentées et édition JSON de `pickerConfig` ;
- fallback embarqué mis à jour avec `default_pack.json` `2.05.0` issu de JC Lib VS Code `0.7.60`.

## Compilation Visual Studio 2026

Sous Windows avec la charge de travail **Visual Studio extension development** :

```powershell
powershell -ExecutionPolicy Bypass -File tools\build_vs2026_release.ps1 -Configuration Release
```

Le VSIX produit doit ensuite être validé avec `docs/MANUAL_TEST_CHECKLIST_1.3.1.md`.


## Version 1.3.2 — catalogue LabWindows/CVI structuré

Le fallback embarqué utilise maintenant `default_pack.json` 2.06.0 et inclut le catalogue LabWindows/CVI enrichi. Le fichier autonome `Assets/Packs/cvi_pack.json` remplace la logique historique `CVI Helpers`.
