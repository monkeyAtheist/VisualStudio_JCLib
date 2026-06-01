# Checklist manuelle — JC Lib Visual Studio 0.7.0

## 1. Régression du navigateur

- [ ] Compiler en `Debug | Any CPU`.
- [ ] Lancer l'instance expérimentale avec `F5`.
- [ ] Ouvrir `Affichage > Autres fenêtres > JC Lib`.
- [ ] Vérifier le chargement du catalogue embarqué.
- [ ] Rechercher `LoadPanel`, modifier un paramètre puis insérer le snippet dans un fichier source.

## 2. Ouverture du Visual Pack Editor

- [ ] Importer `docs/example_packs/jclib_sample_pack.json` comme pack global.
- [ ] Déplier `Gestion des packs`.
- [ ] Sélectionner le pack exemple.
- [ ] Vérifier que `Éditer le pack externe` est activé.
- [ ] Cliquer sur ce bouton.
- [ ] Vérifier que la fenêtre `JC Lib — Visual Pack Editor` apparaît.

## 3. Protection du pack embarqué

- [ ] Sélectionner le pack embarqué dans le gestionnaire principal.
- [ ] Vérifier que `Éditer le pack externe` est désactivé.
- [ ] Vérifier également que sa désactivation et sa suppression restent interdites.

## 4. Modification et sauvegarde

- [ ] Dans l'éditeur, sélectionner `jc_sample_log`.
- [ ] Modifier sa description et son snippet.
- [ ] Vérifier que `Sauvegarder` devient actif.
- [ ] Cliquer sur `Sauvegarder et fermer`.
- [ ] Vérifier le rechargement automatique du navigateur principal.
- [ ] Rechercher `jc_sample_log` et vérifier la nouvelle prévisualisation.

## 5. Ajout et suppression

- [ ] Ouvrir à nouveau le pack exemple.
- [ ] Sélectionner le catégorie `Imported pack demo`.
- [ ] Cliquer sur `Ajouter un élément`.
- [ ] Sélectionner `NewElement` dans l'arborescence.
- [ ] Remplacer son nom par `jc_sample_trace`.
- [ ] Remplacer son snippet par `jc_sample_trace("hello");`.
- [ ] Sauvegarder puis fermer.
- [ ] Rechercher `jc_sample_trace` dans le navigateur principal.
- [ ] Ouvrir à nouveau l'éditeur, sélectionner l'élément puis cliquer sur `Supprimer l'élément`.
- [ ] Sauvegarder et vérifier sa disparition.

## 6. Validation des doublons et espaces invisibles

- [ ] Ajouter deux éléments dans le même groupe.
- [ ] Nommer le premier `Capture (...)`.
- [ ] Nommer le second `Capture (...) ` avec un espace final.
- [ ] Vérifier que la validation signale un nom dupliqué.
- [ ] Vérifier que la sauvegarde est bloquée.
- [ ] Renommer ou supprimer l'un des doublons.
- [ ] Sauvegarder et vérifier que les espaces de début et de fin sont normalisés.

## 7. Conservation des propriétés inconnues

- [ ] Vérifier que les paramètres structurés existants du pack ne disparaissent pas après une modification et une sauvegarde.
- [ ] Vérifier que le JSON reste lisible par le navigateur principal.
