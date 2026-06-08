# Manual test checklist — JC Lib Visual Studio 1.3.7

1. Build the VSIX on Windows with `tools/build_vs2026_release.ps1`.
2. Open the JC Lib tool window and import `Assets/Packs/system_scripting_pack.json`.
3. Select `ssh login with password prompt`; confirm the preview is `ssh robot@192.168.1.50`.
4. Click **Exécuter dans une console** and confirm an external console opens with the command.
5. Select a PowerShell card and confirm PowerShell is used.
6. Select a Bash/Linux card and confirm WSL or Bash is used when available.
7. Select `docker system prune`; confirm a warning dialog appears before launch.
8. Confirm the Visual Studio extension does not expose integrated-pack generation.
