# JC Lib Visual Studio 1.3.7 — console execution compatibility

This source update adds an **Exécuter dans une console** button beside insertion and copy actions. The button is enabled for `command` cards and `Scripting / System` recipes. It opens an external shell appropriate to the selected library: PowerShell for the `PowerShell 7` library, WSL/Bash when available for Linux-oriented libraries, and CMD for other cards.

Potentially destructive or multi-line previews require confirmation before launch.

The standalone `system_scripting_pack.json` 1.8.0 catalog is included for explicit import and compatibility checks. Visual Studio remains a pack consumer/importer: integrated-pack generation stays exclusive to the VS Code extension.
