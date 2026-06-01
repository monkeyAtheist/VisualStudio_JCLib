# JC Lib Visual Studio 0.2.1 — suppression de la dépendance System.Web

## Symptôme

La compilation de la version 0.2.0 échouait avec :

```text
Could not find assembly 'System.Web, Version=4.0.0.0, Culture=neutral,
PublicKeyToken=b03f5f7f11d50a3a'.
```

## Cause

Le premier navigateur utilisait `System.Web.Script.Serialization.JavaScriptSerializer`.
Cette implémentation nécessitait la référence `System.Web.Extensions` et entraînait la
résolution de la pile `System.Web` pendant la génération du VSIX.

JC Lib ne développe pas une application Web : cette dépendance était inutile.

## Correction

Le chargeur utilise désormais :

```csharp
System.Runtime.Serialization.Json.DataContractJsonSerializer
```

Le pack JSON est désérialisé vers des DTO privés fortement typés. Les groupes restent
parcourus récursivement et les champs JSON supplémentaires sont ignorés.

Dans le projet :

```xml
<Reference Include="System.Runtime.Serialization" />
```

remplace :

```xml
<Reference Include="System.Web.Extensions" />
```

## Nettoyage recommandé avant recompilation

Supprimer les dossiers :

```text
src\JCLib.VisualStudio\bin
src\JCLib.VisualStudio\obj
```
