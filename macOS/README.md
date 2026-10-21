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

The Python test suite runs every test script without a window:

```sh
cd APIs/Python/Test_Scripts
python3 Run_Tests.py --exe ../../../macOS/PrePoMax/bin/Debug/net10.0/PrePoMax.com --settings <settings file>
```

The automation server (RPC, MCP) serves only with an access token. The tests of the server need a settings file
that carries one: generate it in Tools > Settings > Automation, or give the tests a copy of the settings with a
token (`--settings`), which the child sessions of the tests then read as well.

## Rendering

The views are rendered by a 60 Hz timer; `PPM_DISPLAY_LINK=1` uses the display link of macOS instead. A session
without a window (`-g No`) runs on the headless platform of Avalonia, so scripts, regenerations and the automation
server also run while the display sleeps or the screen is locked; its pictures are rendered by an OpenGL context of
their own (`PPM_NATIVE_BATCH=1` uses the native platform instead).

## Known limitations

- QuadWild: PrePoMax ships modified Windows binaries of QuadWild-BiMDF whose source is not available, so the
  QuadWild mesher is not available on macOS.
- Netgen meshing is done by gmsh (its 3D algorithm is the Frontal/Netgen one). Structured (transfinite) four-sided
  and polar three-sided faces and the numbers of elements set on edges are supported; three-sided faces meshed as
  three blocks ("Mapped") are meshed unstructured.
- The macOS builds of ccx have only the SPOOLES direct solver (no PARDISO). Models that are badly conditioned, such
  as a body held only by compression only supports, can be found singular by SPOOLES.
- Files of the composites modeler of Yvan Blanchard (PrePoMax 2.4.5 dev) open; their laminae keep the out-of-plane
  constants of that version (v23 = v12).
