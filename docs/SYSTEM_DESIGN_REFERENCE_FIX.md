# Correctif System.Design — version 0.1.2

## Symptômes

```text
Le type MenuCommandService est défini dans un assembly qui n'est pas référencé.
Vous devez ajouter une référence à System.Design.

OleMenuCommandService ne contient pas de définition pour AddCommand.
```

## Cause

`Microsoft.VisualStudio.Shell.OleMenuCommandService` dérive de `System.ComponentModel.Design.MenuCommandService`. Dans un projet SDK-style ciblant `net472`, la référence `System.Design` doit être déclarée explicitement. Tant que la classe de base ne peut pas être résolue, sa méthode héritée `AddCommand(MenuCommand)` n'est pas visible par le compilateur.

## Correction

Ajouter au fichier `.csproj` :

```xml
<ItemGroup>
  <Reference Include="System.Design" />
</ItemGroup>
```
