# PrePoMax for macOS

[![Build](https://github.com/moritzschuettler/prepomac/actions/workflows/build.yml/badge.svg)](https://github.com/moritzschuettler/prepomac/actions/workflows/build.yml)

A native macOS port of [PrePoMax](https://prepomax.fs.um.si) 3.0, the open-source pre- and post-processor for the
CalculiX finite element solver, including the composites modeler of
[Yvan Blanchard's fork](https://gitlab.com/yvanblanchard/pre-po-max-composites): composite shell sections, laminates,
ply orientations and the Tsai-Wu, maximum stress and other failure criteria.

It runs as a normal Mac app on Apple Silicon, without Windows, a virtual machine or Wine. It is a community port, not
an official PrePoMax release. PrePoMax is written by Matej Borovinšek (University of Maribor), and the composites
modeler by Yvan Blanchard.

**Status:** usable, still being tested. 77 of the 79 API test scripts pass; see [Known limitations](#known-limitations).
Bug reports are very welcome: [open an issue](https://github.com/moritzschuettler/prepomac/issues) with the
steps, a small model (.pmx or the CAD file), and your macOS version and chip. Pull requests are welcome too, see
[CONTRIBUTING.md](CONTRIBUTING.md).

## Contents

- [Install](#install) (and [Linux](#linux-experimental))
- [Using PrePoMax on a Mac](#using-prepomax-on-a-mac)
- [Code_Aster as a second solver](#code_aster-as-a-second-solver)
- [Driving PrePoMax from an AI agent](#driving-prepomax-from-an-ai-agent)
- [Validation](#validation)
- [Known limitations](#known-limitations)
- [Development](#development)
- [License](#license)

## Install

### Download (Apple Silicon)

The [releases](https://github.com/moritzschuettler/prepomac/releases) carry `PrePoMax-macOS-arm64.zip`, an app that
needs nothing else installed: OpenCASCADE, gmsh, lp_solve, CalculiX, a Python with NumPy and the .NET runtime are
inside it. Unzip it and move `PrePoMax.app` to /Applications.

The app is not signed with an Apple developer certificate, so macOS refuses to open it the first time. Either open
System Settings > Privacy & Security and click *Open Anyway* after the first try, or clear the download flag once in
the terminal:

```sh
xattr -dr com.apple.quarantine /Applications/PrePoMax.app
```

### Build from the source

Building takes about 15 minutes, most of it for installing the tools. Tested on Apple Silicon with macOS 15; macOS 13
or newer should work, Intel Macs are untested.

#### 1. The tools

```sh
xcode-select --install                          # compilers for the native parts
# .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
brew install cmake opencascade gmsh lp_solve    # Homebrew: https://brew.sh
brew install costerwi/calculix/calculix-ccx     # CalculiX
```

Any CalculiX `ccx` works; PrePoMax finds it in /opt/homebrew/bin, /usr/local/bin or the PATH.

#### 2. Build and install the app

```sh
git clone https://github.com/moritzschuettler/prepomac.git
cd prepomac/macOS
./make-app.sh
```

The clone carries the whole history of PrePoMax (about 260 MB); `git clone --depth 1 ...` is quicker.

`make-app.sh` builds the Release configuration, packs it into `PrePoMax.app` and installs it in /Applications (or
~/Applications when /Applications is not writable). PrePoMax then opens from the Launchpad, the Dock or Finder. To
update, run `git pull` and `./make-app.sh` again.

The command line program, for scripts and batch runs, is `/Applications/PrePoMax.app/Contents/MacOS/PrePoMax.com`:

```sh
PrePoMax.com --script my_study.py -g No -x Yes -w ~/PrePoMax/Work
```

### Linux (experimental)

The same sources build and run on Linux, as [@oofsdfs](https://github.com/oofsdfs) found out on Arch Linux
([#2](https://github.com/moritzschuettler/prepomac/issues/2)). The build, the CAD import, both meshers and the Python
scripts are checked without a window on Debian 13 (arm64); the user interface has been seen running on Arch Linux with
X11. Labels may be cut off, because the port replaces the Windows fonts by those of macOS.

Arch Linux:

```sh
sudo pacman -S git base-devel cmake opencascade lpsolve dotnet-sdk python
yay -S gmsh-bin                                  # or: the gmsh SDK from gmsh.info, with GMSH_DIR=<its folder>
```

Debian 13 / Ubuntu (OpenCASCADE 7.8 or newer):

```sh
sudo apt install build-essential cmake git python3 libpython3-dev libgmsh-dev liblpsolve55-dev \
    libocct-data-exchange-dev libocct-modeling-algorithms-dev libocct-modeling-data-dev libocct-foundation-dev \
    libocct-ocaf-dev
# .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
```

Then:

```sh
git clone https://github.com/moritzschuettler/prepomac.git
cd prepomac/macOS
./build.sh Release
./run.sh Release                                 # or PrePoMax/bin/Release/net10.0/PrePoMax.com for scripts
```

CalculiX `ccx` is found in the PATH.

## Using PrePoMax on a Mac

The program works as on Windows; the [PrePoMax documentation and tutorials](https://prepomax.fs.um.si) apply. The
differences:

- **Folders:** the work directory is `~/PrePoMax/Work`; settings, history and the material library are in
  `~/Library/Application Support/PrePoMax`.
- **Shortcuts:** the ⌘ command key does what the Ctrl key does on Windows.
- **Solver:** CalculiX is found automatically (the downloaded app carries its own); Tools > Settings > Solver changes
  it. The macOS builds of ccx only have the SPOOLES matrix solver, which is the default.
- **Meshing:** the Netgen mesher of Windows is replaced by gmsh. The Netgen mesh items stay in the menus and are
  meshed by gmsh with its Netgen 3D algorithm, so meshes differ from Windows ones.

### Navigating the 3D view

| Action | Mouse | Trackpad |
|---|---|---|
| Zoom at the pointer | wheel (with ⌘ in finer steps) | two-finger scroll, or pinch |
| Rotate | drag with the middle button | ⌥ option + drag |
| Pan | ⇧ shift + drag with the middle button | ⌥ option + ⇧ shift + drag |
| Zoom by dragging | ⌘ + drag with the middle button | ⌥ option + ⌘ + drag |
| Zoom to a box | hold Z and drag a box with the left button | |
| Fit the model | "Zoom to fit" in the toolbar | |
| Rotate in steps | arrow keys | |

Tools > Settings > Navigation sets the speeds and the directions.

### Python scripts and recording

- **Run scripts:** View > Side Panels > Python Script Editor. *Run script*, *Run selection*, or *Record and run*,
  which also adds the script to the command history of the model.
- **Record:** every action in the program is recorded in the command history; File > Export > Python session journal
  writes it as a Python script that repeats it.
- The Python API is documented in [Python_API_Overview.md](Python_API_Overview.md). Example studies are in
  [assets_composites/](assets_composites/), among them a carbon bike frame with ply optimization
  ([bike_frame_study.py](assets_composites/bike-frame/bike_frame_study.py)).

## Code_Aster as a second solver

[Code_Aster](https://code-aster.org) 18.1 can solve the same models in Docker. It keeps composite shells as true
layered shells (DKT, DEFI_COMPOSITE), where CalculiX expands every ply block into solid elements. The results come back
as a CalculiX .frd file with the displacements, the stresses, and for composite shells the Tsai-Wu index, reserve factor
and critical ply.

```sh
brew install colima docker
colima start aster --vm-type vz --vz-rosetta                      # amd64 through Rosetta on Apple Silicon
DOCKER_CONTEXT=colima-aster macOS/Native/CodeAster/build.sh       # builds the image prepomax/code_aster:18.1
```

Choose CodeAster as the solver of an analysis. Tools > Settings > Code_Aster sets the docker executable, the context
(`colima-aster`) and the image. colima shares only the home folder, so the work directory must be inside it.
`colima start` makes its context the current docker context; `docker context use default` switches back.

Supported: static steps with fixed and displacement boundary conditions, concentrated forces and moments, gravity;
shells (plain and composite) and solids (linear and quadratic tetrahedra, linear hexahedra and wedges).

## Driving PrePoMax from an AI agent

PrePoMax serves an MCP endpoint itself, on this computer only, while its automation server is on. An agent client
connects to an address rather than starting a process, so nothing is left running when PrePoMax is closed.

1. Switch on **Tools > Automation Server**. The server needs an access token; the first time there is none, PrePoMax
   offers to generate one. To have the server on after every start, set **Tools > Settings > Automation > Start with
   the application**.
2. Read the *Access token* and the *Address* from **Tools > Settings > Automation**. The port is 8756 unless that port
   was taken; then the server answers on the next free one, which the *Address* shows.
3. Point the client at them. For Claude Code:

```sh
claude mcp add --scope user --transport http prepomax http://127.0.0.1:8756/mcp --header "Authorization: Bearer <token>"
```

Another MCP client takes the same address and header in its configuration:

```json
{"mcpServers": {"prepomax": {"type": "http", "url": "http://127.0.0.1:8756/mcp",
                             "headers": {"Authorization": "Bearer <token>"}}}}
```

A client that speaks only stdio needs a bridge to HTTP, such as `mcp-remote`. So does a client in a browser: the
endpoint refuses every request with an `Origin` header, which keeps web pages out.

The token is kept in `settings.bin` in `~/Library/Application Support/PrePoMax` and can only be generated, never typed.
A client that cannot connect means PrePoMax is closed or its automation server is off.

The application serves the agent its own guide through the tool **`get_guide`**: the order of the work, what a number
without a unit means, how to find the face that was meant, and what to do when a call is refused.
[AgentSkill/prepomax/SKILL.md](AgentSkill/prepomax/SKILL.md) is a Claude Code skill that points an agent at it; copy it
to `~/.claude/skills/prepomax/SKILL.md`. [Python_API_Overview.md](Python_API_Overview.md), section *Agent integration*,
documents the tools and the security gates.

## Validation

[assets_composites/plate-validation](assets_composites/plate-validation) compares the composite shells with classical
laminate theory, for CalculiX with layered shells, CalculiX with the smeared laminate of the bike frame study, and
Code_Aster (`./run_validation.sh`). The loads are scaled to an analytic Tsai-Wu reserve factor of 2:

| Case | CalculiX layered | CalculiX smeared | Code_Aster |
|---|---|---|---|
| Simply supported [0/90]s plate, Navier: deflection / reserve | +0.2 % / −0.2 % | +1.2 % / −9.2 % | −0.1 % / 0.0 % |
| [±45/0/90]s strip in tension | +1.3 % / −7.5 % | −0.6 % / 0.0 % | +0.3 % / 0.0 % |
| The same strip as a cantilever | −6.5 % / −3.7 % | −54 % / +28 % | −8.5 % / −4.0 % |

The cantilever formula assumes a strip free to twist, which the clamp prevents, so both solvers are a few percent
stiffer than it while agreeing with each other within 2 %. The smeared laminate holds for membrane loads only, so it is
meant for quick optimization loops, with a final check of the layered model.

## Known limitations

- Two API tests fail: the mirrored revolve mesh of `revolve_end_cap.brep`, and the second-order tetrahedra of
  `sweep_plate_overhang.brep`, whose volume is about 1 % off (gmsh 4.15 of Homebrew, 4.13 on Windows).
- Netgen meshing is done by gmsh (its 3D algorithm is the Frontal/Netgen one). Structured (transfinite) four-sided
  faces, three-sided faces as three blocks ("Mapped") or polar, and the numbers of elements set on edges are supported.
- QuadWild is not available: PrePoMax ships modified Windows binaries of QuadWild-BiMDF whose source is not public.
- The macOS builds of ccx have only the SPOOLES direct solver (no PARDISO). Badly conditioned models, such as a body
  held only by compression-only supports, can be found singular, and large models are slower.
- On the carbon bike frame model, CalculiX and Code_Aster differ by about 50 % in stiffness, although they agree on the
  plates above; the cause is not found yet (shell junctions or supports are suspected).
- The trackpad navigation is new and needs testing.
- The help page of the Python editor is not built yet; [Python_API_Overview.md](Python_API_Overview.md) has its content.
- Files of the composites modeler of Yvan Blanchard (PrePoMax 2.4.5 dev) open; their laminae keep the out-of-plane
  constants of that version (v23 = v12).

## Development

```sh
cd macOS
./build.sh              # Debug build (./build.sh Release for Release)
./run.sh                # starts macOS/PrePoMax/bin/Debug/net10.0/PrePoMax
```

`build.sh` uses a .NET 10 SDK even when the `dotnet` of the PATH is an older one. The build also compiles the native
parts: `Native/Win32Compat.c`, `Native/NetGenMesher` (the CAD and mesher process on OpenCASCADE and gmsh) and
`Native/Mmg` (downloaded and built once). How to contribute a change is in [CONTRIBUTING.md](CONTRIBUTING.md); every
push and pull request is built on a macOS runner of GitHub Actions.

### Structure

The shared PrePoMax sources are in the folders of the repository root; the projects in [macOS/](macOS/) compile them
and replace only what depends on Windows.

| Folder | Content |
|---|---|
| `PrePoMax` | the user interface: `Forms/FrmMain.cs` is the main form, `Controller.cs` holds the model |
| `CaeModel`, `CaeMesh`, `CaeResults` | the FE model (materials, sections, steps, ...), the mesh, the results |
| `CaeJob` | running the solvers |
| `CaeGlobals` | classes shared by all projects |
| `APIs` | the Python API and the automation server (RPC, MCP) |
| `UserControls`, `vtkControl` | the model tree and the other controls, the 3D view |
| `GmshCaller`, `STL`, `3MF` | the gmsh process, STL import, 3MF export |
| `macOS/WinFormsCompat` | Windows Forms on [Avalonia](https://avaloniaui.net) |
| `macOS/DrawingCompat` | System.Drawing on SkiaSharp |
| `macOS/vtkControl/Mac/Vtk` | the VTK classes PrePoMax uses, on OpenGL |
| `macOS/FastColoredTextBox`, `macOS/ZedGraph` | the code editor and the charts |
| `macOS/Native` | NetGenMesher, Mmg, the Win32 stubs, the Code_Aster image |
| `APIs/Python/Test_Scripts` | the test suite |

The macOS projects list the shared files one by one, so a new source file in a shared folder is added to the project
of its folder in `macOS/`.

PrePoMax records every user action as a command, so that a model can be rebuilt (Edit > Regenerate History): user
functions are not called directly but through a command class for each action, in `PrePoMax/Commands`.

### Testing

The Python test suite runs every test script without a window:

```sh
cd APIs/Python/Test_Scripts
python3 Run_Tests.py --exe ../../../macOS/PrePoMax/bin/Debug/net10.0/PrePoMax.com --settings <settings file>
```

The automation server (RPC, MCP) serves only with an access token. Its tests need a settings file that carries one:
generate it in Tools > Settings > Automation, or give the tests a copy of the settings with a token (`--settings`).

`PPM_AUTOMATION=<script>` runs a GUI test script after the start (see `macOS/WinFormsCompat/Forms/Automation.cs` for
the commands: menu, click, set, select, file, snapshot, dump, ...). `PPM_TRACE=1` writes the first chance exceptions
with their stack traces to the error output.

### Rendering

The views are rendered by a 60 Hz timer; `PPM_DISPLAY_LINK=1` uses the display link of macOS instead. A session
without a window (`-g No`) runs on the headless platform of Avalonia, so scripts, regenerations and the automation
server also run while the display sleeps or the screen is locked; its pictures are rendered by an OpenGL context of
their own (`PPM_NATIVE_BATCH=1` uses the native platform instead).

## License

PrePoMax is licensed under the GNU General Public License v3, see [LICENSE](LICENSE), and so is this port. The
third-party components keep their own licenses; [Licenses/README.md](Licenses/README.md) lists them, and the app carries
their texts in `Contents/Resources/Licenses`.
