# PrePoMax

PrePoMax is developed on GitHub: https://github.com/PrePoMax/PrePoMax. The former GitLab project,
https://gitlab.com/MatejB/PrePoMax, is archived and no longer updated - open issues and pull requests
on GitHub.

# Prerequisites
*  Visual Studio 2026 Community (development environment) - https://www.visualstudio.com/downloads/ - with the
   workloads ".NET desktop development" and "Desktop development with C++", including its component
   "C++ CMake tools for Windows" (the CMake that builds vtkNative)

# PrePoMax Visual Studio setup
*  Download a Master branch of the PrePoMax package and extract it to a PrePoMax folder
*  Open the solution: "PrePoMax\PrePoMax.sln"

# vtkNative and the VTK SDK
The 3D view is `vtkNative`, a C++ library on VTK 9.7.1 (folder [vtkNative/](vtkNative/)) that `vtkControl` reaches
through a C API. Building `vtkControl` builds it with CMake.
*  VTK comes as a prebuilt SDK (VTK 9.7.1, shared, x64, Release, patched). The first build downloads it from the
   GitHub release named by `VtkNativeSdkUrl` in `vtkControl/vtkControl.csproj` (about 60 MB) and unpacks it into
   `vtkNative\sdk`, which git ignores. Nothing else needs to be installed.
*  The build writes `vtkNative.dll` and the VTK DLLs it loads to `vtkNative\build\runtime`, and the post-build
   event of PrePoMax copies them, with the VTK license texts, into the `lib` folder of the output.
*  `VtkNativeCMake` is the path of `cmake.exe` (default: the one installed with Visual Studio); `VtkNativeVtkDir`
   points at a local VTK build instead of the SDK. Both can be set as environment variables.

## Build VTK and publish a new SDK
Needed only to change the VTK version or its patches.
*  Download the VTK 9.7.1 sources from https://vtk.org/download/ and apply
   [vtkNative/patches/vtk-9.7.1-ssao-background.patch](vtkNative/patches/vtk-9.7.1-ssao-background.patch)
   (the ambient occlusion keeps the background).
*  Configure and build VTK shared, x64, Release, with the module groups StandAlone, Rendering and Imaging and without
   FFMPEG, e.g. `cmake -S <vtk source> -B <vtk build> -A x64 -DBUILD_SHARED_LIBS=ON
   -DVTK_GROUP_ENABLE_Rendering=YES -DVTK_GROUP_ENABLE_StandAlone=YES -DVTK_GROUP_ENABLE_Imaging=YES
   -DVTK_MODULE_ENABLE_VTK_IOFFMPEG=NO` and `cmake --build <vtk build> --config Release`.
*  Pack it with `vtkNative\Package-VtkSdk.ps1 -VtkBuildDir <vtk build> -OutputDir <folder> -SdkName <name>`, upload
   the zip as the asset of a new GitHub release, and set `VtkNativeSdkName` and `VtkNativeSdkUrl` in
   `vtkControl/vtkControl.csproj` to the new name and release. A new name makes every machine download the new SDK.

At last change the active solution platform using the main menu: **Build** -> **Configuration Manager** and select **x64** as the **Active solution platform**.

Start the compilation and execution of the project by pressing the Start button...

Compiling PrePoMax produces a working meshing setup on its own. The Netgen, Mmg and Gmsh binaries
are vendored in [ThirdParty/](ThirdParty/) and the build copies them into `Meshers\Netgen`,
`Meshers\Mmg` and `lib`, together with the license texts from [Licenses/](Licenses/). Nothing has to
be copied by hand for meshing to work.

The rest of the base folder (Models, Solver, Python, Tools, Help) still has to be taken from the
latest released version of PrePoMax's base folder. Then set the working folder and the solver
(CalculiX) executable file name in Settings -> Calculix.

# License

PrePoMax is licensed under the GNU General Public License v3 - see [LICENSE](LICENSE).

It also ships third-party mesher binaries that are not built from this repository: Gmsh
(GPL-2.0-or-later, with an exception), Mmg (LGPL-3.0-or-later), and NetGenMesher (LGPL-2.1,
https://github.com/PrePoMax/NetgenMesher), which in turn links Netgen, OpenCASCADE and pthreads-win32
(all LGPL-2.1). Their license texts live in
[Licenses/](Licenses/) and the build copies them into the output folder next to the binaries they
cover, so a build from this repository carries them automatically. The 3D view ships VTK 9.7.1 (BSD-3-Clause)
and the libraries bundled in it, whose license texts the build copies into `lib\vtk-licenses`.

See [Licenses/README.md](Licenses/README.md) for the full inventory, the corresponding-source
obligation, and the handful of libraries bundled with the OpenCASCADE distribution whose terms are
still to be established.

# Driving PrePoMax from an AI agent

PrePoMax serves an MCP endpoint itself, on this computer only, while its automation server is on. An
agent client connects to an address rather than starting a process, so nothing is left running when
PrePoMax is closed.

1. In PrePoMax, switch on **Tools - Automation Server**. The server needs an access token, and the
   first time there is none PrePoMax offers to generate one - answer yes. To have the server on after
   every restart, set **Tools - Settings - Automation - Start with the application**.
2. Read the *Access token* and the *Address* from **Tools - Settings - Automation**. A token can also
   be generated there, or generated again, from the context menu of the *Access token* entry. The
   address is also written to the output window when the server starts. Copy it rather than assuming
   the port: it is 8756 unless that port was taken, and then the server answers on the next free one,
   which the *Address* shows while the server runs.
3. Point the client at them. For Claude Code, with the address you read:

```bash
claude mcp add --scope user --transport http prepomax http://127.0.0.1:8756/mcp --header "Authorization: Bearer <token>"
```

`--scope user` registers the server for every project; without it Claude Code registers it for the
current directory only. `/mcp` in Claude Code shows the connection and reconnects it - after
PrePoMax was closed and opened again, the client has to reconnect.

Another MCP client takes the same address and header in its configuration, usually in this form:

```json
{"mcpServers": {"prepomax": {"type": "http", "url": "http://127.0.0.1:8756/mcp",
                             "headers": {"Authorization": "Bearer <token>"}}}}
```

A client that speaks only stdio needs a bridge to HTTP, such as `mcp-remote`. So does a client that
runs in a browser: the endpoint refuses every request that carries an `Origin` header, which is what
keeps web pages out.

The token is kept in that folder's `settings.json` and can only be generated, never typed. The server
will not start without one, and there is no command line switch for it, so a folder has to be opened
once with its window before an unattended run can serve. Unpack PrePoMax into a folder inside your
user profile, such as `C:\Users\<you>\PrePoMax`: other local accounts can read a folder created
directly under `C:\`, and with it the token in `settings.json`.

A newly unpacked PrePoMax starts without a token on purpose: `settings.json` is part of the
redistributable, and a token left in it would be shipped to everyone. The first run in a folder
clears whatever token its settings carry - PrePoMax knows it is the first run because its own
`trace.info` marker is absent - and the settings page has a *Clear the access token* action to use
before preparing a release. After an update, generate a token in the new folder, or keep the old
one: start the new version once and close it, then copy `settings.json` over from the old folder - or
the `settings.bin` of a version before the JSON settings, which is read once and rewritten as
`settings.json`. A settings file copied in before that first start loses its token, because the first
start clears it.

**A client that cannot connect means PrePoMax is closed, or its automation server is off.** Nothing
answers on the address unless the session is open - that is what serving it from the application
buys - so open PrePoMax and switch the server on. It is worth telling the agent as much in the
project's own instructions, so that it asks rather than working around it.

The application serves the agent its own guide: **`get_guide`** is how the work is ordered, what a
number without a unit means, how to find the face that was meant, and what to do when a call is
refused. It travels with the version, so there is nothing to keep in step on the client side.
`Help\Python\SKILL.md`, in the installation folder (and in [AgentSkill/prepomax/](AgentSkill/prepomax/)
in this repository), is a Claude Code skill that points an agent at it. Copy it to
`<project>/.claude/skills/prepomax/SKILL.md` to have it in one project, or to
`~/.claude/skills/prepomax/SKILL.md` to have it in all of them.

`Python_API_Overview.md`, section *Agent integration*, documents the tools, the security gates, the
port, and the pipe the same server also keeps for a client that speaks its protocol directly.

# Structure

The PrePoMax is a solution which consists of 14 projects:
*  APIs: classes for interfacing with PrePoMax from other programs (Python, ...)
*  CaeGlobals: global classes for all other projects to use
*  CaeJob: classes for running the analysis
*  CaeMesh: classes for FE mesh: nodes, elements, sets, ...
*  CaeModel: classes for FE model. Model contains FE mesh + materials, sections, ...
*  CaeResults: classes for FE results. Results contain FE mesh + field outputs, ...
*  GmshCaller: a stand alone executable used for running the Gmsh routines
*  GmshCommon: wrapper for the Gmsh mesher from https://github.com/tsvilans/gmsh_common 
*  PrePoMax: classes for user interface
*  PrePoMaxCmd: the console subsystem launcher deployed as PrePoMax.com, so that a command line run
   waits and receives the exit code
*  STL: classes for stl geometry import
*  ThreeMF: classes for 3MF geometry export
*  UserControls: classes for more complex user controls, as model tree view...
*  vtkControl: classes for 3D visualization

PrePoMax is compiled in an exe file all the other projects are compiled in dll files.

Alongside the projects, [ThirdParty/](ThirdParty/) holds the binaries that are not built here and
are deployed by the build: `netgen/` (NetGenMesher and its OpenCASCADE runtime), `mmg/` (the Mmg
executables) and `gmsh-4.13.0-Windows64-sdk/` (the Gmsh sdk that GmshCommon links against and whose
dll is shipped).

The internal structure of the program is quite complex and there are almost no comments in the code (no time to write them) but I am using very descriptive names. Each class has its own file with .cs extension. You can browse the files in the Solution Explorer.

The PrePoMax project has a Forms folder and inside it is a FrmMain.cs file/class. This is the main form. The form communicates exclusively with the Controller.cs file/class which holds all data about the model. The program records all user actions in order to be able to repeat them later (while running PrePoMax select Edit -> Regenerate History) so all needed user functions/subroutines are not called directly but via Commands. There is a special Command class for each user action...
