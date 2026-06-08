# Visual Studio 2026 manual test checklist — 1.3.9

1. Build with `tools/build_vs2026_release.ps1` on Windows.
2. Import `Assets/Packs/csharp_language_pack.json`.
3. Expand `C# / .NET` then `ASP.NET Core Razor / CSHTML`.
4. Open `CSHTML HTML <footer> element`, clear `id` and `className`, verify `<footer>Footer content</footer>`.
5. Open `CSHTML input asp-for Tag Helper`, clear `className`, verify `<input asp-for="Device.Name">`.
6. Use the multi-select `attributes` chooser and verify fragments are separated by one space.
