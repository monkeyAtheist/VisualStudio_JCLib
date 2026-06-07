# JC Lib Visual Studio 1.3.1 — correctif de compilation

## Correction

- Remplacement de la chaîne LINQ ambiguë fondée sur `.Reverse()` dans `PackEditorWindow.ReadChoiceArray()`.
- Le code retire désormais explicitement les champs optionnels vides en fin de ligne avant l’appel à `string.Join`.
- Ce correctif évite l’erreur `CS0023` observée avec le projet `net472` lorsque la résolution de surcharge sélectionne une méthode `Reverse()` retournant `void`.

## Compatibilité

Les fonctionnalités ajoutées en 1.3.0 sont conservées : paramètres documentés, sélections multiples, `pickerConfig`, navigateurs fichier ou dossier et éditeur de packs enrichi.
