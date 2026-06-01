# Checklist manuelle — JC Lib Visual Studio 0.6.0

## Préparation

1. Fermer l'instance expérimentale précédente.
2. Compiler `JCLib.VisualStudio.sln` en `Debug | Any CPU`.
3. Lancer avec `F5`.
4. Ouvrir `Affichage > Autres fenêtres > JC Lib`.

## Régression fonctionnelle

1. Rechercher `LoadPanel`.
2. Modifier un paramètre.
3. Insérer le snippet dans un fichier C.
4. Vérifier la conservation de l'indentation.

## Import global

1. Importer `docs/example_packs/jclib_sample_pack.json` avec `Importer un pack global`.
2. Rechercher `jc_sample_log`.
3. Vérifier la provenance `global utilisateur`.

## Désactivation et activation

1. Déplier `Gestion des packs`.
2. Sélectionner `JC Lib Sample Pack` global.
3. Cliquer sur `Activer / désactiver`.
4. Vérifier que `jc_sample_log` disparaît de la recherche.
5. Réactiver le pack.
6. Vérifier que l'entrée réapparaît.

## Override solution > global

1. Ouvrir une solution Visual Studio.
2. Importer le même pack avec `Importer pour la solution`.
3. Rechercher `jc_sample_log`.
4. Vérifier qu'une seule entrée est visible.
5. Vérifier la provenance `solution`.
6. Déplier `Sources et diagnostics` et vérifier qu'une entrée globale est masquée.

## Suppression

1. Sélectionner le pack solution dans `Gestion des packs`.
2. Cliquer sur `Supprimer le pack externe`.
3. Confirmer la suppression.
4. Vérifier que la version globale redevient visible.
5. Supprimer ensuite la version globale et vérifier que la recherche ne retourne plus `jc_sample_log`.

## Pack embarqué

1. Sélectionner le pack embarqué.
2. Vérifier que les boutons d'activation et de suppression restent désactivés.
