# Checklist manuelle — JC Lib Visual Studio 1.1.0

## Compilation

- [ ] Supprimer éventuellement `bin` et `obj`.
- [ ] Restaurer les paquets NuGet.
- [ ] Compiler en `Debug | Any CPU`.
- [ ] Lancer l'instance expérimentale avec `F5`.

## Ouverture rapide

- [ ] Presser `Ctrl+Alt+J`.
- [ ] Vérifier que la Tool Window JC Lib apparaît.
- [ ] Ouvrir un fichier source, clic droit dans l'éditeur.
- [ ] Vérifier que l'entrée `JC Lib` est disponible dans le menu contextuel.

## Apparence

- [ ] Cliquer sur `Apparence`.
- [ ] Cliquer sur `Thème sombre accessible` puis `Appliquer`.
- [ ] Vérifier que les bibliothèques, catégories et éléments sont lisibles dans l'arbre.
- [ ] Modifier la couleur d'accent et vérifier sa propagation dans les sources de résultats.
- [ ] Ouvrir un pack externe dans le Visual Pack Editor.
- [ ] Vérifier que les nœuds de l'arbre restent lisibles.

## Favoris et récents

- [ ] Rechercher `QTimer singleShot`.
- [ ] Sélectionner une entrée et cliquer sur `☆ Ajouter aux favoris`.
- [ ] Déplier `Accès rapides` et vérifier l'entrée dans `Favoris`.
- [ ] Cliquer sur `Copier le snippet` puis vérifier l'entrée dans `Récents`.
- [ ] Double-cliquer sur un récent et vérifier l'insertion rapide.

## Filtres

- [ ] Choisir l'environnement `QT`.
- [ ] Vérifier que l'arbre est filtré.
- [ ] Choisir une bibliothèque.
- [ ] Activer `Favoris uniquement`.
- [ ] Cliquer sur `Réinitialiser les filtres`.

## Persistance

- [ ] Saisir une recherche.
- [ ] Choisir des filtres et un élément.
- [ ] Fermer puis rouvrir Visual Studio expérimental.
- [ ] Vérifier la restauration de la recherche, des filtres, des favoris, des récents et de la fiche sélectionnée.

## Non-régression

- [ ] Insérer un snippet paramétré.
- [ ] Modifier et sauvegarder un pack externe.
- [ ] Déplacer ou dupliquer un sous-arbre.
- [ ] Exporter puis importer un fragment JSON.
