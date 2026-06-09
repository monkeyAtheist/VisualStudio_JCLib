# Release notes 1.3.10

## DLL helper compatibility

Visual Studio synchronise le fallback `default_pack.json` 2.25.0 et distribue les catalogues autonomes C 1.12.0, C++ 2.16.0 et Win32 1.9.0.

Le runtime WPF prend désormais en charge :

- `insertValueMap` pour remplacer une valeur de sélecteur par un fragment de template ;
- les champs `multiline`, `textarea` et `code` ;
- `preserveSourceOrder` dans les sélecteurs multi-choix ;
- la carte unique `GetProcAddress` en mode nom ou ordinal ;
- la génération propre des branches `DllMain`.

La génération des packs intégrés reste réservée à VS Code.
