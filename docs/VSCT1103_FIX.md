# Correctif VSCT1103 — version 0.1.1

## Symptôme

```text
VSCT1103 Undefined Parent/@id attribute IDM_VS_MENU_VIEW_OTHERWINDOWS
```

## Cause

`IDM_VS_MENU_VIEW_OTHERWINDOWS` n'est pas un symbole VSSDK déclaré par `vsshlids.h`.

## Correction

Le bouton `JC Lib` est rattaché directement au groupe Visual Studio prédéfini :

```xml
<Parent guid="guidSHLMainMenu" id="IDG_VS_WNDO_OTRWNDWS1" />
```

Ce groupe correspond au sous-menu `Affichage > Autres fenêtres`.
