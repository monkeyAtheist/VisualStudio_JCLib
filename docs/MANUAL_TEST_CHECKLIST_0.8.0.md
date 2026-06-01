# Checklist manuelle — JC Lib Visual Studio 0.8.0

## 1. Régression du navigateur

- Compiler en `Debug | Any CPU`.
- Lancer l'instance expérimentale avec `F5`.
- Ouvrir `Affichage > Autres fenêtres > JC Lib`.
- Rechercher `QTimer periodic timeout`.
- Vérifier la prévisualisation puis l'insertion dans un fichier source.

## 2. Création d'un pack

- Cliquer sur `Créer un pack`.
- Conserver la destination globale.
- Utiliser un identifiant unique, par exemple `jclib.test.editor080`.
- Utiliser le fichier `jclib_test_editor080.json`.
- Valider.
- Vérifier que le Visual Pack Editor s'ouvre automatiquement.

## 3. Hiérarchie

Dans le pack créé :

- sélectionner `Custom` puis ajouter une bibliothèque ;
- sélectionner la nouvelle bibliothèque puis ajouter une catégorie ;
- sélectionner la catégorie puis ajouter un groupe ;
- renommer les nouveaux nœuds ;
- ajouter un élément dans le groupe ;
- renommer l'élément en `jc_demo_delay` ;
- définir le snippet sur `jc_demo_delay(delayMs);`.

## 4. Paramètres structurés

Pour `jc_demo_delay` :

- ajouter un paramètre ;
- nommer le paramètre `delayMs` ;
- définir le type `int` ;
- définir `editorType` sur `text` ;
- définir la valeur par défaut sur `100` ;
- ajouter les presets `100`, `250`, `500`, chacun sur une ligne ;
- sauvegarder et fermer ;
- rechercher `jc_demo_delay` dans le navigateur ;
- vérifier que la valeur initiale générée est `100` et que le snippet est insérable.

## 5. Validation des doublons

- rouvrir le pack ;
- ajouter deux groupes sous une même catégorie ;
- leur donner le même nom avec une variation d'espace final ;
- vérifier que la sauvegarde est bloquée ;
- corriger le nom ;
- vérifier que la sauvegarde redevient possible.

## 6. Duplication

- sélectionner le pack embarqué dans `Gestion des packs` ;
- cliquer sur `Dupliquer le pack` ;
- choisir un identifiant et un fichier uniques ;
- vérifier que la copie externe apparaît et s'ouvre dans l'éditeur ;
- fermer sans sauvegarder si la copie complète ne doit pas être conservée ;
- supprimer ensuite la copie externe depuis le gestionnaire.

## 7. Suppression récursive

- dans un pack de test, créer un groupe avec un élément ;
- supprimer le groupe ;
- confirmer la boîte de dialogue ;
- vérifier que le groupe et son élément disparaissent.
