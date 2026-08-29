# Mmg (vendored binaries)

The Mmg remeshing executables PrePoMax shells out to. They are not built from this repository and
had no home in it before - they only survived inside build-output folders, so a clean checkout could
not produce a working `Meshers\Mmg`.

| File | Version | Notes |
|---|---|---|
| `mmgs.exe` | MMGS 5.5.2 (Nov 17, 2020) | surface remeshing |
| `mmg3d.exe` | MMG3D 5.7.0 (Dec 13, 2023) | volume remeshing, level-set splitting |
| `vcruntime140.dll` | Visual C++ redistributable | the only non-system import of either exe |

The two executables are deliberately different releases - that is what has been shipped and tested.

PrePoMax is portable, so the Visual C++ runtime cannot be assumed present on the target machine and
travels with the executables. The UCRT (`api-ms-win-crt-*.dll`) is part of Windows 10/11 and is not
included.

Upstream: https://github.com/MmgTools/mmg - LGPL-3.0-or-later. The license texts that must ship
alongside these binaries are in `Licenses/mmg/`, not here.

The `PostBuildEvent` in `PrePoMax.csproj` copies this folder into `$(TargetDir)Meshers\Mmg`, so a
build produces a working Mmg folder without copying anything from a released version.
