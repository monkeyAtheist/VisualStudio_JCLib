# Correctif ProductArchitecture — version 0.1.3

## Symptôme

Le build VSIX échoue avec :

```text
The vsixmanifest must contain a value for 'PackageManifest:Installation:InstallTarget:ProductArchitecture'.
```

## Cause

Depuis Visual Studio 2022, chaque `InstallationTarget` du manifeste VSIX doit déclarer l'architecture de l'installation Visual Studio ciblée.

## Correction

Le projet JC Lib est une extension managée `AnyCPU`. Le manifeste déclare donc deux cibles par édition Visual Studio :

```xml
<InstallationTarget Id="Microsoft.VisualStudio.Community" Version="[17.0,)">
  <ProductArchitecture>amd64</ProductArchitecture>
</InstallationTarget>
<InstallationTarget Id="Microsoft.VisualStudio.Community" Version="[17.0,)">
  <ProductArchitecture>arm64</ProductArchitecture>
</InstallationTarget>
```

La même configuration est appliquée aux éditions Professional et Enterprise.
