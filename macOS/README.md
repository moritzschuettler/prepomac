# PrePoMax for macOS

The macOS build of PrePoMax 3.0 with the composite functions. The projects in this folder compile the shared
PrePoMax sources of the repository; only the Windows specific parts are replaced (WinForms on Avalonia, VTK on
OpenGL, the Windows API stubs, the NetGenMesher CAD and mesher process).

## Requirements (Apple Silicon or Intel Mac, macOS 13 or newer)

```sh
# .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
brew install cmake opencascade gmsh lp_solve
brew install costerwi/calculix/calculix-ccx   # or any CalculiX ccx, found in /opt/homebrew/bin, /usr/local/bin or the PATH
```

## Build

```sh
cd macOS
./build.sh            # or ./build.sh Release
```

`build.sh` uses a .NET 10 SDK even when the `dotnet` of the PATH is an older one (`dotnet --list-sdks` shows
the installed SDKs; the official installer puts .NET 10 in `/usr/local/share/dotnet`).

The build also compiles the native parts (`Native/Win32Compat.c` and `Native/NetGenMesher`, a C++ program on
OpenCASCADE and gmsh) and copies them into the output folder.

## Run

```sh
cd macOS
./run.sh
```

(the program is `macOS/PrePoMax/bin/Debug/net10.0/PrePoMax`). Questions such as the recovery of an unsaved
session are asked in a window of the program, not in the terminal.

- The CalculiX solver is found automatically; Tools > Settings > Solver changes it. The macOS builds of ccx only
  have the SPOOLES matrix solver, which is the default.
- Work directory: `~/PrePoMax/Work`; settings, history and the material library:
  `~/Library/Application Support/PrePoMax`.

## Testing

`PPM_AUTOMATION=<script>` runs a GUI test script after start (see `WinFormsCompat/Forms/Automation.cs` for the
commands: menu, click, set, select, file, snapshot, dump, ...). `PPM_TRACE=1` writes the first chance exceptions
with their stack traces to the error output.

## Status

Working: model tree, forms, 3D view (OpenGL), .inp import, STEP/IGES/BREP import, Gmsh meshing, analysis with ccx,
results. In progress: Netgen meshing and the remaining CAD operations of NetGenMesher, Mmg and QuadWild, the
composite workflow tests, Python/MCP on macOS and the app bundle.
