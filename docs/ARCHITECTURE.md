# Architecture du socle minimal

## Choix retenu

Le premier incrément utilise le VSSDK historique en mode `AsyncPackage`, avec un projet SDK-style et une interface WPF.

Ce choix évite d'introduire simultanément WebView2, la communication JavaScript/C# et la lecture des packs JSON. Il valide d'abord l'intégration à l'IDE : enregistrement du package, commande VSCT, création d'une Tool Window et affichage dans l'instance expérimentale.

## Flux actuel

```text
Affichage > Autres fenêtres > JC Lib
             |
             v
        commande VSCT
             |
             v
      JCLibPackage.cs
             |
             v
       JCLibToolWindow
             |
             v
 JCLibToolWindowControl.xaml
```

## Phase suivante

Ajouter un service `PackCatalogService` qui désérialise `Assets/Packs/default_pack.json`, puis afficher une première arborescence WPF en lecture seule.
