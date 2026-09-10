# Prerequisites
*  Visual Studio 2026 Community (development environment) - https://www.visualstudio.com/downloads/
*  ActiViz OpenSource Edition 5.8.0 (64-bit Windows XP or later) (3D library used for graphics - must be installed only on the development PC; users do not need it) - https://prepomax.fs.um.si/downloads/

# PrePoMax Visual Studio setup
*  Download a Master branch of the PrePoMax package and extract it to a PrePoMax folder
*  Open the solution: "PrePoMax\PrePoMax.sln"

# Recreate the references to the VTK library
First delete the two existing references to the VTK library:
*  Open Solution Explorer: View->Solution Explorer
*  In Solution Explorer find the vtkControl project and then find the References branch
*  Delete the references starting with Kitware. (right click and Delete).
 
Secondly add the two references again using the following procedure:
*  Right click on References from vtkControl project in the Solution Explorer Window and selected Add Reference
*  A Reference manager window opens where you select Browse on the left side and then press the Browse button on the bottom right
*  Browse for the file Kitware.VTK.dll which should be in the ActiViz installation folder: "C:\Program Files\ActiViz.NET 5.8.0 OpenSource Edition\bin\Kitware.VTK.dll"
*  Repeat the procedure for the file Kitware.mummy.Runtime.dll: "C:\Program Files\ActiViz.NET 5.8.0 OpenSource Edition\bin\Kitware.mummy.Runtime.dll"

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
(GPL-2.0-or-later, with an exception), Mmg (LGPL-3.0-or-later), and NetGenMesher (GPL-3.0), which in
turn links Netgen, OpenCASCADE and pthreads-win32 (all LGPL-2.1). Their license texts live in
[Licenses/](Licenses/) and the build copies them into the output folder next to the binaries they
cover, so a build from this repository carries them automatically.

See [Licenses/README.md](Licenses/README.md) for the full inventory, the corresponding-source
obligation, and the handful of libraries bundled with the OpenCASCADE distribution whose terms are
still to be established.

# Driving PrePoMax from an AI agent

PrePoMax serves an MCP endpoint itself, on this computer only, while its automation server is on. An
agent client connects to an address rather than starting a process, so nothing is left running when
PrePoMax is closed.

1. In PrePoMax, switch on **Tools - Automation Server**, or leave it on with
   **Tools - Settings - Automation - Start with the application**.
2. Right click the *Access token* entry in **Tools - Settings - Automation** and choose *Generate the
   access token*, then read it and the address from that page.
3. Point the client at them. For Claude Code:

```bash
claude mcp add --transport http prepomax http://127.0.0.1:8756/mcp --header "Authorization: Bearer <token>"
```

The token is kept in that folder's `settings.bin` and can only be generated, never typed. The server
will not start without one, and there is no command line switch for it, so a folder has to be opened
once with its window before an unattended run can serve.

A newly unpacked PrePoMax starts without a token on purpose: `settings.bin` is part of the
redistributable, and a token left in it would be shipped to everyone. The first run in a folder
clears whatever token its settings carry - PrePoMax knows it is the first run because its own
`trace.info` marker is absent - and the settings page has a *Clear the access token* action to use
before preparing a release. After an update, generate a token in the new folder, or copy
`settings.bin` forward as you would to keep any other setting.

**A client that cannot connect means PrePoMax is closed, or its automation server is off.** Nothing
answers on the address unless the session is open - that is what serving it from the application
buys - so open PrePoMax and switch the server on. It is worth telling the agent as much in the
project's own instructions, so that it asks rather than working around it.

An earlier version shipped `PrePoMaxMcp.exe`, a bridge the client started on stdio. It is gone: a
process the client starts outlives the application it serves, and with PrePoMax closed every open
agent session still held an idle one. Remove the old `prepomax` entry from the client and add the
address instead; the tools and their behaviour are unchanged.

`Python_API_Overview.md`, section *Agent integration*, documents the tools, the security gates, the
port, and the pipe the same server also keeps for a client that speaks its protocol directly.

# Structure

The PrePoMax is a solution which consists of 15 projects:
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

The PrePoMax project has a Forms folder and inside it is a FrmMain.sc file/class. This is the main form. The form communicates exclusively with the Controller.sc file/class which holds all data about the model. The program records all user actions in order to be able to repeat them later (while running PrePoMax select Edit -> Regenerate) so all needed user functions/subroutines are not called directly but via Commands. There is a special Command class for each user action...
