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

As an app: `./make-app.sh` builds the Release configuration and installs `PrePoMax.app` in /Applications (or
~/Applications), which then opens from the Launchpad, the Dock or Finder. The command line program is
`/Applications/PrePoMax.app/Contents/MacOS/PrePoMax.com`.

Navigation of the 3D view:

| | Mouse | Trackpad |
|---|---|---|
| Zoom about the pointer | wheel (with ⌘: finer) | two finger scroll, pinch |
| Rotate | middle button drag | ⌥ option + drag |
| Pan | ⇧ shift + middle button drag | ⌥ option + ⇧ shift + drag |
| Zoom by dragging | ⌘ + middle button drag | ⌥ option + ⌘ + drag |
| Zoom to a box | hold Z and drag a box with the left button | |
| Rotate in steps | arrow keys | |

Tools > Settings > Navigation sets the speeds and the directions. Python: View > Side Panels > Python Script Editor
runs scripts (Run, Run selection, or Record and run, which adds the script to the command history); every action
in the program is recorded in the history, and File > Export > Python session journal writes it as a script.

- The CalculiX solver is found automatically; Tools > Settings > Solver changes it. The macOS builds of ccx only
  have the SPOOLES matrix solver, which is the default.
- Work directory: `~/PrePoMax/Work`; settings, history and the material library:
  `~/Library/Application Support/PrePoMax`.

## Code_Aster (optional second solver)

Code_Aster 18.1 runs in Docker. Composite shells stay true layered shells (DKT, DEFI_COMPOSITE) instead of being
expanded to solids as for CalculiX; the results come back as a .frd file with displacements, stresses and the
Tsai-Wu index, reserve factor and critical ply of composite shells.

```sh
brew install colima docker
colima start aster --vm-type vz --vz-rosetta      # Apple Silicon: amd64 through Rosetta
DOCKER_CONTEXT=colima-aster macOS/Native/CodeAster/build.sh   # builds prepomax/code_aster:18.1 (conda-forge)
```

Choose CodeAster as the solver of an analysis; Tools > Settings > Code_Aster sets the docker executable, the
context (`colima-aster`) and the image. colima shares only the home folder, so the work directory must be inside it.
`colima start` makes its context the current docker context; `docker context use default` switches back.

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
  faces, three-sided faces as three blocks ("Mapped") or polar, and the numbers of elements set on edges are
  supported. The tests of QuadWild skip the QuadWild steps.
- Two sweep cases of the tests differ from Windows (gmsh 4.15 of Homebrew, 4.13 on Windows): the mirrored revolve
  mesh of revolve_end_cap.brep and the second order of the tetrahedral cells of sweep_plate_overhang.brep.
- The macOS builds of ccx have only the SPOOLES direct solver (no PARDISO). Models that are badly conditioned, such
  as a body held only by compression only supports, can be found singular by SPOOLES.
- Files of the composites modeler of Yvan Blanchard (PrePoMax 2.4.5 dev) open; their laminae keep the out-of-plane
  constants of that version (v23 = v12).
