# Manual test checklist — JC Lib Visual Studio 1.3.8

1. Build the Visual Studio extension on Windows with `tools/build_vs2026_release.ps1`.
2. Open `Scripting / System → OpenSSH & Secure Remote Access` and select `ssh inspect resolved alias`.
3. Confirm the preview is `ssh -G "myraspi"`.
4. Select `Recipe — bootstrap SSH alias and Ed25519 key (PowerShell)` and confirm the execution button is enabled.
5. Confirm a modal warning appears before executing a multi-line alias-enrollment recipe.
6. Select `Add Windows hosts alias` and confirm a warning appears before console execution.
7. Verify integrated-pack generation remains absent from Visual Studio.
