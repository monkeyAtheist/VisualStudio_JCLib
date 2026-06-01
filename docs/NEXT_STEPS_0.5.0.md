# Étapes suivantes après JC Lib Visual Studio 0.5.0

La prochaine itération pourra ajouter la gestion opérationnelle des packs :

- activation et désactivation sélective d'un pack ;
- suppression d'un pack externe depuis l'interface ;
- priorité explicite solution > global > embarqué ;
- politique d'override contrôlée pour les éléments en conflit ;
- création d'un pack vide ;
- préparation du portage du Visual Pack Editor.

Le Visual Pack Editor complet pourra ensuite être porté soit en WPF natif, soit via une Tool Window hybride WebView2 réutilisant une partie de l'interface HTML existante.
