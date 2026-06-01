# Checklist manuelle — JC Lib Visual Studio 0.4.0

## Chargement général

- [ ] Compiler en `Debug | Any CPU`.
- [ ] Lancer avec `F5`.
- [ ] Ouvrir `Affichage > Autres fenêtres > JC Lib`.
- [ ] Vérifier l'affichage de `jerryLib v1.18.0 — 2 042 éléments`.

## Fonction CVI paramétrable

- [ ] Rechercher `LoadPanel`.
- [ ] Sélectionner `LoadPanel` dans `CVI > User interface > Panels`.
- [ ] Vérifier la présence des champs `parentPanel`, `fileName` et `panelResourceId`.
- [ ] Vérifier le snippet initial `LoadPanel(0, "", panelResourceId);`.
- [ ] Modifier `panelResourceId` et vérifier la mise à jour immédiate du snippet.
- [ ] Utiliser le bouton `...` du champ `fileName` et vérifier l'échappement des antislashs dans la chaîne C.

## Retour de fonction

- [ ] Rechercher une fonction non `void`, par exemple `LoadPanel`.
- [ ] Saisir `panelHandle` dans `Variable de retour`.
- [ ] Vérifier le snippet `panelHandle = LoadPanel(...);`.

## Booléens et suggestions

- [ ] Rechercher `SetKeyPressEventKey`.
- [ ] Sélectionner la suggestion `0` ou `1` pour le paramètre booléen.
- [ ] Vérifier la mise à jour du snippet.

## Insertion

- [ ] Ouvrir un fichier source.
- [ ] Insérer un appel paramétré à la position du curseur.
- [ ] Vérifier le remplacement d'une sélection active.
- [ ] Vérifier la conservation de l'indentation sur un snippet multiligne.
