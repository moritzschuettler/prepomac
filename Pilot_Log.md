# VTK native pilot - log

Plan: [Vtk_Native_Pilot_Plan.md](Vtk_Native_Pilot_Plan.md) (copied from `PrePoMax v3.0.n dev`).
Pilot folder: `C:\Pc\Programiranje\PrePoMax\PrePoMax v3.0.n dev_vtk-pilot`, branch `pilot/vtk-native` (from `master`
at `39dcc3d39`). The clone carried uncommitted contact pair search work on `master`; it is left as it is and is not part
of the pilot.

Pilot files live in `VtkPilot/`.

## Summary table

| Phase | Start | End | Agent turns | Usage (5 h / weekly, % of plan) | Fix cycles | C# replaced | C++ / C API written | User time |
|---|---|---|---|---|---|---|---|---|
| 0. Reference | 2026-11-05 09:18 | 2026-11-05 09:27 | 1 user turn, ~45 tool calls | 5 % -> 6 % / 4 % -> 4 %; context 119k -> 188k | 4 (harness only) | 0 | 0 (393 lines of Python harness) | |
| 1. Groundwork | 2026-11-05 11:00 | 2026-11-05 11:18 | 1 user turn, ~60 tool calls | 13 % -> 17 % / 5 % -> 6 %; context 194k -> 344k | 3 (+3 tooling) | 0 (groundwork adds, replaces nothing) | C API 127, C++ 889, CMake 76; C# P/Invoke layer 639; 49 lines in existing files; 444 lines of test scripts | |
| 2. Slice A | | | | | | | | |
| 3. Slice B | | | | | | | | |
| 4. Slice C | | | | | | | | |

Usage is read from the session usage card (`get_usage`): the 5 hour and weekly percentages of the Team plan at the
start and end of each phase, plus the context tokens of the session. Token counts per phase are not reported by the
app directly, so the percentage difference is the cost measure. The plan percentages are account wide: between
phase 0 (ended at 6 %) and phase 1 (started at 13 %) other work used the same plan, so only the difference within
a phase belongs to the pilot.

## Phase 0 - Reference

Started 2026-11-05 09:18. Session usage at start: 5 h window 5 %, weekly 4 %, context 119k tokens.

### Approach

- No change to the application. `VtkPilot/Phase0/Record_Reference.py` runs as a `--script` in a hidden session
  (`-g No`) and reaches the live `vtkControl` and `Controller` by reflection through `Program._mainForm`. A probe
  confirmed the hidden form has a real viewport (987 x 445 by default) and that private fields are reachable from
  pythonnet.
- Viewport size is fixed by resizing the main form until `vtkControl.ClientSize` is exact: 800 x 600 and 960 x 540.
- Views: Isometric, Front, Top, IsometricNegative, each followed by zoom to fit. The camera (10 values from
  `pmx.view.get_parameters`) is stored next to every image, so the new path can be compared both with its own zoom to
  fit and with the identical camera.
- Images through `pmx.view.save_image`, which calls `vtkControl.RenderToPNG`.
- Picks for slice C at 800 x 600, isometric: a 16 x 12 grid of point picks and four boxes (inside / crossing, small and
  large) per select mode - node, element, edge (node item), surface, part. `vtkControl.Pick(x1, y1, box, x2, y2)` is
  called on the UI thread with VTK display coordinates (origin bottom left). For every pick the raw screen-to-3D output
  of `OnMouseLeftButtonUpSelection` (point, direction, frustum planes, part names, completely inside) and the ids
  `Controller.GetSelectionIds()` resolves are stored in `<case>_picks.json`.
- Per mode one highlight image (after a pick) and one hover image (`Style_PointPickedOnMouseMoveEvt` by reflection).

### Cases

| Case | Input | Content |
|---|---|---|
| solid | `APIs/Python/Test_Scripts/Models/L_bracket_r.step` | geometry view, then meshed femodel view and picks |
| hinge | `Tests/Joints/Joint_Hinge.pmx` | femodel view (two parts) and picks |
| result | `Tests/Joints/Joint_Hinge.pmx`, `Analysis-1` solved | results view with the scalar bar: DISP ALL, STRESS MISES |

### Progress

- 09:18 Branch `pilot/vtk-native` created, plan copied in, Debug build in `PrePoMax\bin\x64\Debug` (built 09:07 from
  this clone) used as it is.
- 09:20 Reflection probe works in a hidden session. First hinge run: 7.6 s, failed on a pythonnet boxing detail
  (`PyInt` passed to `MethodInfo.Invoke`; needs `System.Int32`).
- 09:21 Hinge recorded, but the out folder argument was split at the space in `PrePoMax v3.0.n` and the files went to
  a new `C:\Pc\Programiranje\PrePoMax\PrePoMax` folder (moved back and removed). Fixed by joining `sys.argv[2:]`.
- 09:21 960 x 540 Isometric and IsometricNegative came out identical. Cause: `vtkControl.SetIsometricView` picks the
  iso quadrant and up axis closest to the **current** camera, so a standard view depends on the view before it. Fixed by
  restoring one recorded start camera before every view. This history dependence is real behaviour the port must keep.
- 09:22 Solid run: `pmx.model.geometry.parts`, not `pmx.geometry`. Its output landed in `C:\c\Pc\...` because a
  Git Bash `/c/...` path was handed to the application, which reads it as `C:\c\...` (moved back and removed).
  `Record_All.sh` now converts every path with `cygpath -w`.
- 09:23 Result run: `Analysis-1` solved by CalculiX in 0.5 s, 16 images.
- 09:24-09:26 Repeatability: a second full recording compared with `Compare_Reference.py`.

### Result

Reference in `VtkPilot/Reference/{solid,hinge,result}`: 60 PNGs, 2 pick files, a `<case>_meta.json` per case with
every image's viewport, render window size and camera. 5.7 MB. Recording all three cases takes 50 s
(`bash VtkPilot/Phase0/Record_All.sh [out dir]`, `PREPOMAX_EXE` selects another build).

| Case | Images | Point picks hitting (of 192 per mode) | Box picks: ids (inside, crossing, inside large, crossing large) |
|---|---|---|---|
| solid (femodel) | 26 | node 24, element 24, edge 19, surface 24, part 24 | node 114/114/1388/1388, element 2/2/222/222, part 1 each |
| hinge (femodel) | 18 | node 30, element 30, edge 13, surface 30, part 30 | node 2888/2888/7540/7540, element 1186/1186/3837/3837, part 2 each |
| result | 16 | - | - |

Noise floor of the old path against itself (second recording, same build, same machine):

- Picks: bit identical, ids and points, all modes and boxes.
- solid: every image bit identical.
- hinge: every image differs by at most 3 of 255 levels; no pixel above the tolerance of 24; coverage IoU 1.0000.
- result: only the status block differs (it prints the solve time), 0.03 % of the pixels. The comparison of old and new
  in one process will see the same text; a comparison across runs must mask that block.

Observations for later slices:

- Box picks in edge and surface mode return no ids and in part mode the crossing and inside boxes return the same; node
  and element boxes give the same ids whichever way the box is dragged. This is current behaviour and is the reference.
- The ActiViz 5.8 render of a fixed camera is deterministic, so the tolerances of the 9.7 comparison measure only the
  VTK difference, not noise.
- The phase needed no change to the application; the same reflection harness can drive the new path in phase 1.

Recurring problems (will recur across the port): standard views depend on the previous camera; Git Bash paths must be
converted before they reach PrePoMax. One-off: pythonnet boxing in reflection calls, the API name of geometry parts.

Phase 0 ended 2026-11-05 09:27. Session usage at end: 5 h window 6 %, weekly 4 %, context 188k tokens. User review
time: to be noted by the user.

## Phase 1 - Groundwork

Started 2026-11-05 11:00 after the user's go-ahead; phase 0 committed as `63a4bd7f3`. Session usage at start: 5 h
window 13 %, weekly 5 %, context 194k tokens.

### What was built

| Piece | Where | Lines |
|---|---|---|
| C API | `vtkNative/include/vtkNative.h` | 127 |
| Native library: guard and last error, VTK output window redirect, render window, view, exported functions | `vtkNative/src/` (9 files) | 889 |
| CMake project and runtime collection | `vtkNative/CMakeLists.txt`, `vtkNative/CopyRuntime.cmake` | 76 |
| P/Invoke layer: declarations, loader and status checks, view wrapper, WinForms host, enums, exception, event args | `vtkControl/Native/` (8 files) | 639 |
| Hosting in `vtkControl` (hidden host in the designer, `NativeHost`, `ShowNativeView`) | `vtkControl.cs`, `vtkControl.Designer.cs` | 25 |
| Build integration | `vtkControl.csproj` (CMake target), `PrePoMax.csproj` (post build copy) | 24 + 3 |
| Checks | `VtkPilot/Phase1/Test_Groundwork.py`, `Run_Groundwork.sh`, `Toggle_Native_View.py` | 444 |

The C API has 22 functions: library (API version, VTK version, last error, log callback), view lifetime and parent
window, size, event callback, background, render, render info, camera get/set/reset, named mesh actors from a point
array, legacy cell array and cell types, actor names, read pixels, write PNG.

### Build integration

- `vtkNative` is a CMake project against the VTK 9.7.1 build (`Visual Studio 17 2022`, x64, Release; CMake 3.31 has no
  VS 18 generator, VS 2022 is installed beside VS 18). A Debug PrePoMax loads the Release library without trouble.
- `vtkControl.csproj` runs CMake configure (once) and build before it compiles (`BuildVtkNative`), skippable with
  `VtkNativeSkipBuild=true`, and warns instead of failing when CMake or VTK is missing. A no-change build costs a few
  seconds of MSBuild up-to-date checks.
- The CMake post build step resolves the DLLs `vtkNative.dll` loads (`file(GET_RUNTIME_DEPENDENCIES)`) and keeps those in
  the VTK `bin\Release`: **48 VTK DLLs, 217 MB**, 84 MB of it `vtkCommonCore-9.7.dll`. The PrePoMax post build event
  copies `vtkNative\build\runtime\*.dll` into `lib\` next to `vtkControl.dll`. Deployment size is a real-port issue:
  a VTK build with fewer array dispatch instantiations, or Release with `/Os`, should be looked at.
- Loading: `VtkNativeLibrary` loads `vtkNative.dll` by full path from the folder of `vtkControl.dll` with
  `LOAD_WITH_ALTERED_SEARCH_PATH`, so the VTK DLLs beside it resolve there, and `[DllImport("vtkNative.dll")]` binds to
  the loaded module. This works the same on .NET Framework 4.8 and .NET 10 and needs no `PATH` or `SetDllDirectory`.
  Load time 0.04 s.

### Checks (`bash VtkPilot/Phase1/Run_Groundwork.sh`, 6 s, inside a hidden PrePoMax with Joint_Hinge open)

All 25 pass:

- One process: `vtkNative` reports VTK 9.7.1 and ActiViz reports 5.8.0; the process holds both `vtkCommon.dll` /
  `vtkRendering.dll` (5.8) and the 48 `*-9.7.dll`. The NVIDIA RTX 3070 OpenGL 4.5 context serves both.
- Off screen: 1008 hexahedra built in 0.01 s, 640 x 480 read back in 0.045 s, x2 magnified read, PNG to a path with
  `š`, camera round trip, actor names with `š` through the string list convention.
- Errors: a cell with a missing point -> `InvalidArgument` with the native message; a PNG into a missing folder ->
  `Vtk` status, and the VTK error arrives at the C# log callback; a disposed view -> `ObjectDisposedException` on the
  managed side; a made up handle -> `InvalidHandle` from the native side; a call from another thread ->
  `InvalidOperationException`.
- Embedded: showing the host creates the view at the host's size, VTK's `vtkOpenGL` window is a child of the host,
  the mesh renders, render end and camera events reach C#, `WM_LBUTTONDOWN` / `WM_LBUTTONUP` sent to the VTK window
  come back as `LeftButtonDown` / `LeftButtonUp` with display coordinates (y flipped), resizing the form resizes the
  native view.
- Old path unchanged: after VTK 9.7 rendered off screen and embedded, ActiViz renders the phase 0 hinge Isometric
  view **bit identical** to the reference.
- Interactive feel of the embedded view is for the user: run `VtkPilot/Phase1/Toggle_Native_View.py` from the
  Python panel of a PrePoMax window (built from this branch) to show and hide the VTK 9.7 view over the viewport.

### Fix cycles

1. **Crash: two VTK versions share one Win32 window class.** The first run died with an access violation in
   `DispatchMessageW` right after the first VTK 9.7 render. VTK registers the class `vtkOpenGL` per module instance
   and reuses one it finds; both versions take the executable's instance (or the parent window's), so VTK 9.7 windows
   got the VTK 5.8 window procedure. Fixed by `vtkNativeRenderWindow`, which sets `ApplicationInstance` to the
   instance of `vtkNative.dll`, so VTK 9.7 registers its own class. **This is the condition for running both versions
   in one process and therefore for a region by region port.**
2. **Lost button release events.** The interactor style grabs the event focus between a press and its release, so a
   normal observer saw the press but not the release. Fixed with a passive observer (`PassiveObserverOn`).
3. **VTK 9.7 logged errors to the console** through `vtkLogger` besides the output window. Turned off in the output
   window install; the log callback is the one channel.

Tooling slips, not counted above: a hand-rolled csproj edit replaced every `</Project>` (restored from git and redone
twice; the Bash tool's heredocs also collapse `\\`, so file edits with backslashes go through the Write tool), and the
first test version created a view on the script thread and swallowed its own failure.

### C API conventions that worked

- Opaque handles, validated against a registry of live objects, so a stale or made up handle is an error, not a
  crash.
- Every function returns a status; the message is a thread local last error read with the same size query as any
  string. A guard around every entry point turns C++ exceptions into statuses, and an error VTK reports through its
  output window during the call fails the call (`VTKN_ERROR_VTK`).
- Arrays in: pointer and count, copied before return. Arrays and strings out: caller buffer, required size returned,
  `BUFFER_TOO_SMALL` on the sizing call. String lists are zero separated with a final zero.
- UTF-8 everywhere, marshalled as `byte[]` on the C# side (no `LPUTF8Str`, which .NET Framework and .NET 10 do not
  share in the same way); file names included, and VTK 9.7 writes to a non ASCII path correctly.
- Explicit `__cdecl` on both sides; `int32` ids and counts, `double` coordinates, `uint8` cell types.
- Callbacks are a function pointer plus user data; the C# delegate is held in a field for as long as the native side
  holds the pointer; neither side lets an exception cross back.
- Thread affinity is checked on the managed side; native views have no finalizer, since the OpenGL context must be
  released on its own thread.
- An API version constant checked at load.

### Recurring problems for the full port

- Window class sharing between the two VTK versions (solved once in the render window; any further Win32 window class
  VTK 9.7 may register, such as `vtkWin32HardwareWindow`, needs the same treatment if used).
- Event focus: the native side owns the interaction style, so host code that wants to see drags must observe
  passively, or the style itself must raise the events. PrePoMax's `vtkInteractorStyleControl` will move to C++.
- Runtime size (217 MB for the minimal module set).

Phase 1 ended 2026-11-05 11:18. Session usage at end: 5 h window 17 %, weekly 6 %, context 344k tokens. User review
time: to be noted by the user.

