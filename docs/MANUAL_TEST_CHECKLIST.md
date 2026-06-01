# Checklist manuelle — JC Lib Visual Studio 1.0.0

## Préparation

1. Compiler `JCLib.VisualStudio.sln` en `Debug | Any CPU`.
2. Lancer l'instance expérimentale avec `F5`.
3. Ouvrir `Affichage > Autres fenêtres > JC Lib`.
4. Créer ou dupliquer un pack externe de test.
5. Cliquer sur `Éditer le pack externe`.

## Déplacement d'une catégorie complète

1. Créer deux bibliothèques : `SourceLib` et `DestinationLib`.
2. Ajouter une catégorie `Mesures` sous `SourceLib`.
3. Ajouter un groupe puis deux éléments dans `Mesures`.
4. Sélectionner la catégorie `Mesures` sans cocher d'élément.
5. Cliquer sur `Changer de parent`.
6. Choisir `DestinationLib`.
7. Vérifier que la catégorie, son groupe et ses éléments ont été déplacés ensemble.
8. Vérifier que les parents ouverts restent développés.

## Protection contre une boucle hiérarchique

1. Créer `ParentGroup > ChildGroup`.
2. Sélectionner `ParentGroup`.
3. Cliquer sur `Changer de parent`.
4. Vérifier que `ChildGroup` n'est pas proposé comme destination.

## Duplication d'un groupe

1. Créer un groupe `Diagnostics` avec deux éléments dont un possède un paramètre structuré.
2. Sélectionner `Diagnostics`.
3. Cliquer sur `Dupliquer le sous-arbre`.
4. Vérifier l'apparition de `Diagnostics Copy`.
5. Vérifier que les éléments et paramètres sont présents dans la copie.

## Export et import partiels

1. Sélectionner `Diagnostics Copy`.
2. Cliquer sur `Exporter le sous-arbre`.
3. Enregistrer le fichier `Diagnostics_Copy.jclib-fragment.json`.
4. Sélectionner une autre catégorie.
5. Cliquer sur `Importer un fragment`.
6. Sélectionner le fichier exporté.
7. Vérifier que le groupe complet apparaît sous la catégorie cible.
8. Sauvegarder et fermer l'éditeur.
9. Vérifier dans le navigateur principal que les éléments importés sont recherchables et insérables.

## Import incompatible

1. Exporter une catégorie.
2. Sélectionner un groupe comme destination.
3. Cliquer sur `Importer un fragment`.
4. Vérifier qu'un message explique que le fragment `category` exige une bibliothèque cible.

## Non-régression

1. Cocher deux éléments.
2. Les dupliquer.
3. Les déplacer vers un groupe cible.
4. Les réordonner.
5. Les supprimer avec `Supprimer la sélection`.
6. Modifier le snippet d'un élément et sauvegarder.
7. Vérifier que les parents du TreeView restent développés.
