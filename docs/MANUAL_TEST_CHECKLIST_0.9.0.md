# Checklist manuelle — JC Lib Visual Studio 0.9.0

## Préparation

1. Compiler en `Debug | Any CPU`.
2. Lancer avec `F5`.
3. Ouvrir `Affichage > Autres fenêtres > JC Lib` dans l'instance expérimentale.
4. Créer un pack global de test ou dupliquer le pack d'exemple.
5. Ouvrir `Éditer le pack externe`.

## Création du jeu de test

1. Sous `Custom > Custom Library > General`, ajouter deux groupes : `Source` et `Destination`.
2. Sous `Source`, créer trois éléments : `jc_batch_a`, `jc_batch_b`, `jc_batch_c`.
3. Donner à chacun un snippet différent.

## Sélection multiple

1. Cocher `jc_batch_a` et `jc_batch_b` dans l'arborescence.
2. Vérifier que le bandeau affiche `Sélection groupée : 2 élément(s)`.
3. Cliquer sur `Effacer la sélection` et vérifier que les coches disparaissent.

## Duplication groupée

1. Cocher `jc_batch_a` et `jc_batch_b`.
2. Cliquer sur `Dupliquer`.
3. Vérifier la création de `jc_batch_a Copy` et `jc_batch_b Copy`.
4. Vérifier que les copies restent cochées.

## Changement de parent groupé

1. Avec les copies encore cochées, cliquer sur `Changer de parent`.
2. Sélectionner le groupe `Destination`.
3. Vérifier que les deux copies apparaissent sous `Destination`.
4. Sauvegarder puis rechercher les copies dans la Tool Window principale.

## Réordonnancement

1. Cocher deux éléments d'un même groupe.
2. Cliquer sur `Monter`, puis `Descendre`.
3. Vérifier que l'ordre JSON est mis à jour après sauvegarde.
4. Sans case cochée, sélectionner un groupe et vérifier que `Monter` / `Descendre` agit sur ce groupe.

## Suppression groupée

1. Cocher les deux copies.
2. Cliquer sur `Supprimer la sélection`.
3. Confirmer.
4. Vérifier que seules les copies disparaissent.

## Non-régression

1. Modifier un paramètre structuré d'un élément.
2. Sauvegarder et fermer.
3. Rechercher l'élément dans le navigateur principal.
4. Vérifier la génération du snippet et l'insertion dans l'éditeur actif.
