# JC Lib Build link-argument clarification — 0.7.62

## Scope

The historical Build pack exposed a generic `libraries` text parameter on several GCC, G++ and MinGW cards. That field mixed unrelated concepts: header search directories (`-I`), linker search directories (`-L`), named linked libraries (`-lfoo`) and free-form linker switches.

## Versions

| Component | Before | After |
|---|---:|---:|
| JC Lib VS Code | 0.7.61 | 0.7.62 |
| `default_pack.json` | 2.06.0 | 2.07.0 |
| Build pack | 1.1.0 | 1.2.0 |
| JC Lib Visual Studio sources | 1.3.2 | 1.3.3 |

## Build-pack evolution

| Metric | Before | After |
|---|---:|---:|
| Cards | 98 | 98 |
| Parameters | 303 | 346 |
| Contextual pickers | 128 | 161 |
| Multi-select pickers | 78 | 94 |
| Documented choices | 1202 | 1380 |
| Folder browsers | 21 | 38 |
| Conditional parameters | 0 | 17 |
| Linked picker target indexes | 0 | 17 |

## Corrected model

The affected cards now use separate parameters:

```text
includePathPrefix   -> optional documented `-I` selector
includeDirectory    -> folder browser, enabled only after `-I` is selected
libraryPathPrefix   -> optional documented `-L` selector
libraryDirectory    -> folder browser, enabled only after `-L` is selected
linkedLibraries     -> documented multi-select values such as `-lm -lpthread`
linkerOptions       -> documented free-form or multi-select linker switches
```

The generated GCC shared-library template is now:

```text
gcc {{warnings}} {{optimization}} {{includePathPrefix}}{{includeDirectory}} -fPIC -shared {{sources}} -o "{{output}}" {{libraryPathPrefix}}{{libraryDirectory}} {{linkedLibraries}} {{linkerOptions}}
```

## Affected cards

- `gcc compile executable`
- `gcc compile object`
- `gcc shared library`
- `g++ compile executable`
- `g++ shared library`
- `clang++ compile C++ executable`
- `MinGW gcc executable`
- `MinGW g++ executable`
- `MinGW DLL import library`

## VS Code runtime

The parameter webview now understands `enabledWhen` and dims/disables dependent rows until their source selector becomes active. Optional enum parameters expose an explicit empty choice. Pack saving and starter-pack cloning preserve the conditional metadata.

## Visual Studio synchronization

JC Lib Visual Studio 1.3.3 adds the same `enabledWhen` and `defaultTargetIndex` contract. The WPF parameter editor disables dependent controls and allows a structured picker choice to initialize the associated path field. `Assets/Packs/build_pack.json` is included as a standalone catalog.

## Validation

```text
Node syntax check extension.js                         OK
Runtime artifact synchronization                      OK
VS Code conditional UI regression                     OK
Transversal audit of 21 distributed JSON packs        OK
Visual Studio scaffold validation                     OK
Visual Studio advanced-parameter validation           OK
Visual Studio import of 21 external catalogs          OK
Visual Studio Build conditional regression            OK
```

The Linux validation environment does not contain Visual Studio, MSBuild or the VSIX SDK. The Visual Studio project was therefore validated statically and must still be compiled on Windows.
