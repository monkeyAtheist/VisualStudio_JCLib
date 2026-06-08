# JC Lib Visual Studio 1.3.3 — Build conditional parameters

- Synchronizes the bundled fallback to `default_pack.json` 2.07.0.
- Includes standalone `Assets/Packs/build_pack.json` 1.2.0.
- Adds `CatalogEnabledWhen` and `pickerConfig.defaultTargetIndex`.
- Disables dependent WPF parameter editors until their source selector is active.
- Initializes the associated folder when `-I` or `-L` is selected from a structured picker and the target field is empty.
- Preserves the existing structured-choice, multi-select and file-browser behavior.

Static validations pass. Compile the project on Windows with Visual Studio 2026 and run `docs/MANUAL_TEST_CHECKLIST_1.3.3.md`.
