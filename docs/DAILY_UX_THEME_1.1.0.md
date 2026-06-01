# JC Lib Visual Studio — ergonomie quotidienne et apparence 1.1.0

## Objectif

La version 1.1.0 améliore l'utilisation quotidienne du navigateur sans changer le schéma JSON des packs. Elle corrige également le contraste insuffisant observé avec le thème sombre de Visual Studio.

## Raccourci et commande contextuelle

La commande existante `JC Lib` est maintenant :

- accessible avec `Ctrl+Alt+J` ;
- placée dans `Affichage > Autres fenêtres` ;
- disponible dans le menu contextuel de l'éditeur de code.

Le raccourci reste modifiable depuis les options clavier de Visual Studio.

## Favoris et récents

Les favoris sont basés sur le chemin canonique logique de l'élément. Ils restent donc exploitables si un pack plus prioritaire masque une variante embarquée.

Les récents sont mis à jour après :

- insertion classique ;
- insertion rapide ;
- copie dans le presse-papiers.

La liste est bornée à 18 entrées.

## Filtres

Le navigateur propose :

- environnement ;
- bibliothèque ;
- favoris uniquement ;
- réinitialisation des filtres.

Les filtres s'appliquent à la fois à l'arborescence et à la recherche.

## Insertion rapide

Le double-clic dans les résultats, favoris ou récents insère immédiatement le snippet avec ses valeurs par défaut. Le bouton `Insertion rapide (défauts)` fournit le même comportement depuis la fiche détaillée.

## Apparence

Le bouton `Apparence` configure :

- fond principal ;
- panneaux ;
- champs et arbres ;
- texte principal ;
- texte secondaire ;
- accent ;
- bordures ;
- texte des boutons.

Le thème sombre accessible par défaut utilise un texte clair explicite pour les `TreeViewItem`. Cette correction évite l'affichage noir sur gris observé lorsque les couleurs héritées du thème hôte ne sont pas propagées de manière homogène.

## Stockage local

Les préférences sont enregistrées dans :

```text
%LOCALAPPDATA%\JCLib\VisualStudio\user-preferences.json
```

Le fichier contient la dernière recherche, les filtres, les favoris, les récents, le dernier élément sélectionné et les couleurs.
