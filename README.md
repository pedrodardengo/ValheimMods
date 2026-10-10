# Valheim Mods Monorepo

This folder contains mods created for valheim.

## Build and package

From the repository root, run this PowerShell script to build a mod and create its Thunderstore ZIP:

```powershell
.\package-mod.ps1 -Mod TrueWeatherSwamp
.\package-mod.ps1 -Mod VisualImpairmentSupport
.\package-mod.ps1 -Mod ExtraBuildMaterialsDrop
```

To build and package both mods, use `-Mod All`. The script searches for `dotnet.exe` in `PATH` and common .NET SDK installation folders. It builds Release by default and writes each ZIP to that mod's folder, named with the version from `manifest.json`.

If Valheim is installed outside the default Steam location, pass its installation folder with `-ValheimDir` or set the `VALHEIM_DIR` environment variable:

```powershell
.\package-mod.ps1 -Mod All -ValheimDir 'D:\Games\Valheim'
```