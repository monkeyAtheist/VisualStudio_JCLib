# Visual Studio 2026 manual test checklist — 1.3.10

1. Construire la solution sous Windows avec `tools/build_vs2026_release.ps1`.
2. Importer `Assets/Packs/c_language_pack.json`.
3. Ouvrir `C DLL Helpers > Windows Loader API > GetProcAddress symbol resolution > GetProcAddress`.
4. Vérifier le mode `name` : `GetProcAddress(module, "Plugin_Run");`.
5. Sélectionner `ordinal`, saisir `7`, vérifier : `GetProcAddress(module, MAKEINTRESOURCEA(7));`.
6. Vérifier que le champ non pertinent est désactivé.
7. Ouvrir `DllMain configurable entry point`.
8. Sélectionner plusieurs notifications dans un ordre arbitraire.
9. Vérifier une preview multi-ligne propre dans l’ordre PROCESS_ATTACH, THREAD_ATTACH, THREAD_DETACH, PROCESS_DETACH.
10. Vérifier l’import des catalogues C++ et Win32 autonomes.
