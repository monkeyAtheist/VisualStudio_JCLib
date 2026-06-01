# Catalogues JC Lib

`default_pack.json` est le catalogue partagé issu de JC Lib pour VS Code.

Dans la version Visual Studio `0.5.0`, il est :

- inclus dans le VSIX comme contenu ;
- copié dans le dossier de sortie ;
- embarqué comme ressource de secours ;
- chargé avec les packs globaux utilisateur présents dans `%LOCALAPPDATA%\JCLib\VisualStudio\Packs` ;
- chargé avec les packs de solution présents dans `<solution>\.jclib\packs` ;
- présenté avec sa provenance dans l'arborescence WPF et les résultats de recherche.
