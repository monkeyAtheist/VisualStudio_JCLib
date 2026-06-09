# Visual Studio 2026 manual test checklist — 1.3.11

## Préparation

1. Compiler les sources sous Windows avec `tools/build_vs2026_release.ps1`.
2. Installer le VSIX généré dans l'instance expérimentale Visual Studio 2026.
3. Ouvrir `Affichage > Autres fenêtres > JC Lib` ou utiliser `Ctrl+Alt+J`.

## Navigateur principal

1. Charger plusieurs packs externes ou activer temporairement le fallback embarqué.
2. Vérifier que l'arbre affiche des icônes et badges distincts pour `pack`, `env`, `lib`, `cat`, `group` et les éléments terminaux.
3. Déplier une librairie C puis vérifier que les fonctions affichent `fn`, les macros `macro` et les snippets `snippet` lorsque le catalogue les distingue.
4. Vérifier que les compteurs apparaissent dans un badge séparé et restent corrects après filtrage par environnement ou bibliothèque.
5. Vérifier la légende compacte `E / L / C / G / ƒ` au-dessus de l'arbre.
6. Utiliser la recherche puis vérifier que les résultats remplacent proprement l'arbre sans casser la mise en page.

## Visual Pack Editor

1. Ouvrir un pack externe éditable.
2. Vérifier la même palette pour pack, environnement, bibliothèque, catégorie, groupe et élément.
3. Ajouter puis supprimer un groupe ; vérifier que le compteur du parent est mis à jour.
4. Ajouter un élément ; vérifier que son icône terminale apparaît et que la sélection multiple reste utilisable.

## Apparence

1. Ouvrir `Apparence`.
2. Vérifier la section `Palette de la hiérarchie`.
3. Modifier la couleur `Bibliothèque`, cliquer `Appliquer`, puis vérifier l'arbre principal et le Visual Pack Editor.
4. Utiliser `Thème sombre accessible`, appliquer et vérifier le retour à la palette par défaut.
5. Fermer puis rouvrir Visual Studio ; vérifier la persistance dans `%LOCALAPPDATA%\JCLib\VisualStudio\user-preferences.json`.

## Régressions

1. Vérifier l'insertion d'un snippet paramétré.
2. Vérifier une popup multi-choix.
3. Vérifier un champ multi-ligne `DllMain`.
4. Vérifier une carte HTML avec suppression de `id=""` et `class=""`.
5. Vérifier le bouton `Exécuter dans une console` sur une commande Scripting / System.
