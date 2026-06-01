# JC Lib — Visual Studio 1.1.0

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

Voir `docs/MANUAL_TEST_CHECKLIST_1.1.0.md` pour les cas détaillés.


## Version 1.2.0 — listes déroulantes et packs externes uniquement

- couleurs dédiées au fond et au texte des listes déroulantes ;
- option `Inclure le pack embarqué (fallback)` désactivée par défaut ;
- utilisation normale possible avec les seuls packs exportés depuis l’extension VS Code ;
- fallback embarqué conservé dans le VSIX pour récupération ou premier démarrage.
