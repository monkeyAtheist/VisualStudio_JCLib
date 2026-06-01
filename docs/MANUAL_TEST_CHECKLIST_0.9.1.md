# Checklist manuelle — JC Lib Visual Studio 0.9.1

## Conservation des branches ouvertes

1. Ouvrir un pack externe volumineux dans le Visual Pack Editor.
2. Développer plusieurs niveaux : environnement, bibliothèque, catégorie et groupe.
3. Sélectionner un élément profond.
4. Modifier son nom, sa description ou son snippet.
5. Cliquer sur `Sauvegarder`.
6. Vérifier que les parents restent développés et que l’élément reste sélectionné.

## Reconstruction après opérations avancées

1. Dupliquer un élément profond.
2. Vérifier que les parents restent développés.
3. Déplacer la copie vers un autre groupe.
4. Vérifier que les branches précédemment ouvertes ne se referment pas.
5. Ajouter un groupe puis un élément.
6. Vérifier que le nouveau nœud est sélectionné et que ses ancêtres sont développés.

## Non-régression

- Vérifier la sélection multiple.
- Vérifier `Monter`, `Descendre`, `Changer de parent` et `Supprimer la sélection`.
- Vérifier `Sauvegarder et fermer`.
