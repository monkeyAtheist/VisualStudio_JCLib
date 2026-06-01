# JC Lib Visual Studio 0.6.0 — gestion opérationnelle des packs

## Objectif

Cette itération transforme le chargement multi-pack en un mécanisme exploitable au quotidien. Les packs externes peuvent être activés, désactivés et supprimés depuis la Tool Window. Les conflits sont résolus selon une politique explicite.

## Sources et priorité

```text
solution (300) > global utilisateur (200) > embarqué (100)
```

Le pack embarqué reste obligatoire et en lecture seule. Les packs externes peuvent être désactivés sans supprimer leur fichier JSON.

## Persistance locale

Les chemins désactivés sont conservés dans :

```text
%LOCALAPPDATA%\JCLib\VisualStudio\disabled-packs.txt
```

Cette liste est propre au poste de développement. Elle n'est pas ajoutée à la solution et ne modifie pas les fichiers JSON.

## Résolution des conflits

Pour chaque chemin logique `Environment > Library > Category > Group > Element`, l'entrée issue du pack le plus prioritaire est retenue. En cas d'égalité de priorité, le tri sur le chemin source garantit un résultat déterministe.

Les entrées masquées restent consultables dans la section `Sources et diagnostics`.

## Suppression

La suppression depuis l'interface ne concerne que les packs externes. Le fichier JSON est supprimé après confirmation. Le pack embarqué ne peut pas être supprimé.
