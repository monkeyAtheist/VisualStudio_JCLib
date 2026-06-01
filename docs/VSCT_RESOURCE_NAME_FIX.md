# VSCT menu resource fix — 0.1.4

## Symptom

The VSIX is installed in the experimental Visual Studio instance, but `View > Other Windows > JC Lib` is missing.

## Root cause

The package declares:

```csharp
[ProvideMenuResource("Menus.ctmenu", 1)]
```

The `.vsct` item was compiled without an explicit `ResourceName`. The VSSDK documentation requires the VSCT resource name to be set to `Menus.ctmenu`.

## Fix

```xml
<VSCTCompile Include="Commands\JCLibPackage.vsct">
  <ResourceName>Menus.ctmenu</ResourceName>
  <SubType>Designer</SubType>
</VSCTCompile>
```

## Local retest

1. Close the experimental Visual Studio instance.
2. Delete `src/JCLib.VisualStudio/bin` and `src/JCLib.VisualStudio/obj`.
3. Rebuild the solution.
4. Press `F5`.
5. In the experimental instance, open `View > Other Windows > JC Lib`.
6. If the old command table remains cached, reset the experimental instance and rebuild.
