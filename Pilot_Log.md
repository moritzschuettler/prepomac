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
| 2. Slice A | 2026-11-05 11:48 | 2026-11-05 12:08 | 1 user turn, ~75 tool calls | 23 % -> 31 % / 7 % -> 8 %; context 356k -> 560k | 3 render (+2 check) | ~1,450 (non-blank lines) | C++ ~1,190, C API +85; C# P/Invoke +423; mirror harness 104; checks 259 | |
| 2a. Slice A edge quality (after review) | 2026-11-05 14:39 | 2026-11-05 14:47 | 2 user turns, ~30 tool calls | 2 % -> 3 % / 9 % -> 9 % (5 h window reset); context 131k -> 170k (after a compaction) | 1 render (5 variants measured) | 0 | C++ +19 / -24 | user found it in review |
| 3. Slice B | 2026-11-05 15:06 | 2026-11-05 15:42 | 1 user turn, ~75 tool calls | 4 % -> 14 % / 9 % -> 10 %; context 178k -> 520k | 5 port/render (+3 harness/check) | ~2,000 (non-blank lines) | C++ 1,561 new + ~360 in API/view; C API +91; C# 472 new + ~190 changed; checks 607 | |
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

## Phase 2 - Slice A: geometry and views

Started 2026-11-05 11:48 after the user's go-ahead. Session usage at start: 5 h window 23 %, weekly 7 %, context 356k.

### What was ported

| Old C# (ActiViz 5.8) | Native (VTK 9.7) | Old lines* |
|---|---|---|
| `vtkMaxActor`: `CreatePolyFromData`, `AddNodeAndCellDataToPoly`, `AddCellDataToPoly` (triangulation of tri, quad, quadratic tri and quad; lines, quadratic edges, vertices; unique element edges), `ComputeNormals`, `UpdateColor` (no color table, no contours) | `vtkNativePartActor.cpp` | 567 |
| `vtkControl`: `AddCells`, `AddActor`, `AddActorGeometry`, `AddActorEdges` (base layer), the three `ApplyEdgeVisibilityAndBackfaceCulling*` | `View::AddPartActor`, `PartActor::ApplyEdgesVisibility` | 222 |
| `vtkControl` renderer setup of `OnHandleCreated` / `InitializeControl`: layers, three camera lights, smoothing, samples, background, projection, polygon offset | `View::View` | 47 |
| `Views` region: view parameters, front/back, top/bottom, left/right, isometric (closest quadrant, closest up), axis view (closest axis up), normal, vertical axis, vertical, rotation pivot, keep zoom around the pivot, zoom factor, zoom to fit, box zoom, `ResetCamera` over base and overlay, bounds, `FinishViewChange` without animation | `vtkNativeViewCamera.cpp` | 529 |
| `vtkInteractorStyleControl`: `AdjustCameraDistanceAndClipping`, `AdjustCameraDistance`, `ResetClippingRange`, `ComputeVisiblePropBounds` | `vtkNativeViewCamera.cpp` | 86 |
| **Total** | | **~1,450** |

\* Non-blank lines without empty `//` separators, counted on the committed sources.

Written: C++ 1,190 lines (part actor 488, camera 509, view +144, API +104, minus the replaced bits), C API +85 (two
structs, two enums, 14 functions, API version 2), C# P/Invoke +423 (`VtkNativeMesh`, `VtkNativeMeshData`,
`VtkNativePinnedMesh`, `VtkNativePartStyle`, `VtkNativeOrientation`, view methods). The pilot harness in `vtkControl`
(the mirror, 104 lines) is not port code.

Not ported in this slice although it sits in the same code: the camera animation (`AnimateCamera*`, ~300 lines,
interaction), the color table and result contours of `UpdateColor` (results), the nodal actor, section view hatch,
overlay and selection layers of `AddActor*`, the symbol actors (`AddOriented*`, ~1,000 lines), captions. The plan's
"about 5k lines" for slice A counts all of those; the measured rate below is per ported line.

### How it is checked (`bash VtkPilot/Phase2/Run_SliceA.sh`, 17 s)

- **Mirror:** `vtkControl.EnableNativeMirror` creates an off screen native view; every part `AddCells` adds on the
  base layer (`IsAPart`) also goes to the native view through `vtkn_view_add_part_actor`, `Clear` and `SetLighting`
  follow, and window size, background, smoothing and edges mode are synchronized. A `Controller.Redraw` fills it.
  Mirroring Joint_Hinge (2 parts) adds nothing measurable to the 0.04 s redraw.
- **Same scene:** `SetNativeMirrorPartsOnly` hides on the ActiViz side every prop of the base, overlay and selection
  layers that is not a mirrored part, and stops the widget layer drawing.
- **Same operations:** for each case, viewport (800 x 600, 960 x 540) and view (Isometric, Front, Top,
  IsometricNegative, Right, Bottom) both paths start from the same camera, run the standard view and zoom to fit,
  and write their picture and camera.
- **Cases:** L-bracket geometry (CAD faces, model edges as element edges), L-bracket meshed (tetrahedra surface),
  Joint_Hinge FE model (two parts).
- `Compare_SliceA.py`: camera equal to 1e-9 of the model size; the shapes cover each other within one pixel of the
  outline (>= 0.999); at most 2 % of the pixels differ by more than 24 levels; side by side and difference images.

### Result: 36 of 36 views pass

- **Cameras: bit identical** in all 36 views (difference 0.0). The ported view logic, including the odd world/screen
  mix of `vtkControl.ResetCamera`, frames every view exactly as ActiViz does.
- Coverage within a pixel 1.0000 everywhere; plain IoU 0.9675 to 1.0000, the difference being the antialiased
  silhouette of small parts.
- Pixels differing by more than 24 levels: 0.19 % to 0.79 %, at the element edges and on the curved fillet.

### Fix cycles

1. (check) The first run framed the views 0.2 % differently. The parts only mode had stopped the overlay layer
   drawing but left its props visible, and their bounds took part in the old framing. Hiding them made the cameras
   identical.
2. (render) Element edges came out dashed: a 0.5 pixel line, which VTK 5.8 drew smoothed as a full line of partial
   coverage, hits only some samples of VTK 9's multisampling. Lines below a pixel are now drawn one pixel wide with
   the width folded into the opacity. (Superseded by follow-up 2a below: the real cause was the translucent pass.)
3. (render) The edges still sank into oblique faces. VTK 9.7's coincident topology defaults leave polygons in place
   (offset 0, 0) and pull lines forward by a constant (0, -4), which loses to the depth slope of an oblique face.
   VTK 5.8 pushed polygons back by factor 1, units 1. The native view sets those values.
4. (render) Specular spots on flat faces. VTK 9.7's shader takes the view direction per fragment
   (`normalize(-vertexVC)`) even under parallel projection, while VTK 5.8 lit with a viewer at infinity; PrePoMax
   puts the camera just outside the model, so the highlight wandered across faces. A post shader replacement on the
   part actors sets the view direction to (0, 0, 1).
5. (check) IoU failed small parts on a one pixel antialiasing border; replaced by coverage within one pixel. The
   identical cameras show that the geometry itself lines up.

### Findings for the full port

- **VTK 9 rendering defaults are part of the port.** Polygon offset, sub-pixel line width and the local viewer are
  global behaviors every ported actor inherits; they live once in the native view and part actor, but each new actor
  kind (symbols, glyphs, results) must be checked against them.
- **Shader replacements are the tool** for VTK 5.8 looks that VTK 9 no longer offers as a setting.
- **VTK 9 numbers poly data cells vertices, lines, polygons, whatever the insertion order** (VTK 5.8 kept insertion
  order). `BuildPolyData` writes the global cell ids in VTK 9 order; slice C's cell picking depends on it.
- **The comparison harness is reusable:** mirror, parts only, same start camera, compare cameras and pictures. The
  next actor kinds only need their own `vtkn_*` call in the mirror.
- **Struct marshalling convention:** blittable structs by `ref`, optional ones as an `IntPtr` (null when absent),
  arrays inside structs pinned with `GCHandle` for the duration of the call (no `unsafe` code needed).

### Rate

About 1,450 lines of old C# ported to about 1,190 lines of C++, 85 lines of C API and 423 lines of C# glue in one
20 minute phase, with 3 rendering fix cycles, at 8 % of the 5 hour window.

Phase 2 ended 2026-11-05 12:08. Session usage at end: 5 h window 31 %, weekly 8 %, context 560k tokens. User review
time: to be noted by the user.

### Follow-up 2a: edge quality (after the user's review)

The user's review of the side by side pictures found the native element edges of lower quality than ActiViz's: hard,
unsmoothed one pixel lines. The numeric check had passed them, because they differ in few pixels by much.

Cause: the edges carry an opacity below one (model edges 0.7, element edges 0.4 of the part's), and VTK 9 draws every
such actor in its translucent pass, which renders without multisampling. The workaround of fix cycle 2 (one pixel
width, the width folded into the opacity) only hid the dashes this produces. The user's earlier session on the
ActiViz 9.7 test copy (2026-09-30) had found the same cause and a tested pipeline, which was reused here:

- Edge actors are `SetForceOpaque(true)`: they keep their alpha but are blended in the multisampled opaque pass. The
  original widths 0.8 and 0.5 are restored and the thin line workaround is gone.
- Coincident topology offsets: polygons (2, 1), as VTK 8 had, lines (-1, -8), replacing (1, 1) / (0, 0).
- Multisampling 8 instead of `vtkControl`'s 2. VTK 9's core profile has no line smoothing, so multisampling is the
  only antialiasing a line gets, and with 2 samples the 0.5 pixel lines of the opaque pass come out dotted.

Five variants were measured against the ActiViz pictures (36 views each; differing = more than 24 levels):

| Variant | Mean differing | Worst view | Mean abs. difference | Look |
|---|---|---|---|---|
| Committed slice A (thin lines, translucent pass, old offsets) | 0.43 % | 0.79 % | 0.70 | hard one pixel lines, jagged silhouettes |
| Forced opaque, multisampling 2 | 0.58 % | 1.48 % | 0.76 | element edges dotted |
| Forced opaque with the thin line workaround, multisampling 2 | 0.27 % | 0.50 % | 0.61 | smooth, slightly light |
| **Forced opaque, multisampling 8 (chosen)** | **0.21 %** | **0.45 %** | 0.60 | smooth; edge darkness equal to ActiViz |
| Forced opaque with the thin line workaround, multisampling 8 | 0.16 % | 0.44 % | 0.53 | smooth, edges 0.026 levels lighter on average |

The chosen variant is the user's tested ActiViz 9.7 pipeline unchanged and needs no workaround; the last row is
marginally closer per pixel but keeps a workaround. Slice A still passes 36 of 36 with identical cameras, and the
Phase 1 groundwork checks still pass (ActiViz renders identically after VTK 9.7 rendered).

Findings for the full port:

- **The numeric check misses line quality.** Few pixels, large differences: aliased edges stayed under the 2 % limit.
  The comparison now also reports the mean absolute difference per variant; a visual review of enlarged crops stays
  part of each slice.
- **Prior work transfers directly.** The fix took 8 minutes because the cause and the pipeline were already known
  from the ActiViz 9.7 session. That pipeline also covers symbols, selections and SSAO (depth ladder for symbols drawn
  on faces, SSAA instead of MSAA with SSAO), which slices B and C and the remaining actors should take over.

## Phase 3 - Slice B: the scalar bar widget

Started 2026-11-05 15:06 (5 h window 4 %, weekly 9 %, context 178k tokens). Ended 15:42 (14 %, 10 %, 520k).

### What was ported

| Old C# (ActiViz 5.8) | Non-blank lines | Native port | Lines |
|---|---|---|---|
| `vtkMaxScalarBarWidget` (layout, bar, labels, value label boxes, hover, edit data) | 811 | `ScalarBarWidget` | 540 |
| `vtkMaxBorderWidget` (anchors, keep inside, quadrant on resize, drag, hover) | 714 | `BorderWidget` | 416 |
| `vtkMaxTextWidget`, `vtkMaxWidgetTools` | 181 | `TextWidget` | 148 |
| `vtkMaxWidgetHighlight` | 162 | `WidgetHighlight` | 145 |
| widget part of `vtkInteractorStyleControl` (capture, double click, hover, stacking) | ~170 | `WidgetSet`, `View::HandleMouseEvent`, interactor observer | ~260 |
| `System.Drawing.Rectangle` semantics the widgets compute with | - | `Rect` | 49 |

About 2,000 non-blank lines of old C# became about 1,560 lines of C++ in seven new files plus about 360 lines in the
view and the C API (`vtkNativeApi.cpp` +153, `vtkNativeView.*` +98, `vtkNative.h` +91, API version 3). The C# side
gained `VtkNativeScalarBar` (350 lines) and five small mirror types (122 lines).

The split: the native widget does everything that touches VTK (layout from text sizes, drawing, hit tests, drag,
clamping, resize, highlight, double click detection); C# keeps what is .NET: the label strings (`ToString(format)`
with the user's number formats), the lookup table built from a color function (`CreateLookupTable` without VTK, the
color transfer function passed in as `Func<double, double[]>`), the title text, and the value edit data, which
combines a native hit box with the C# label and limits. The color transfer function of the legend stays in
`vtkControl`: it belongs to the result coloring, not to this slice.

### How it is checked (`bash VtkPilot/Phase3/Run_SliceB.sh`, about 40 s with the solve)

- `vtkControl` mirrors every legend call into the native scalar bar of the mirror. The hooks sit in the methods the
  Controller calls (`SetScalarBarColorSpectrum`, `SetScalarBarText`, `SetScalarBarNumberFormat`,
  `DrawScalarBarBackground/Border`, `InitializeResultWidgetPositions`, the three `CreateLookupTable` calls and the
  four visibility switches, now routed through two helpers). This is where the real port calls the native widget.
- `Record_SliceB.py` solves Joint_Hinge, shows only the scalar bar on both paths, and goes through 8 legend
  configurations: DISP, MISES, smooth, CoolWarm with 5 colors and F2, a manual range with both added colors, the
  maximum color hidden, manual smooth, no frame. Then the same mouse events go to both, the old style's handlers by
  reflection and the native view through `vtkn_view_handle_mouse_event`: hover over the title and the maximum and
  minimum labels, double clicks on both labels and on the title, a drag, a drag into the bottom right corner
  (clamping), a press beside the widget, and a window resize to 960x540 and back.
- `Compare_SliceB.py` requires the layout to be **identical**: widget rectangle, hover highlight, labels, value edit
  boxes with texts and limits, and what each double click opens. Pictures: the dark shapes must cover each other
  within a pixel (near dark by 24 levels), and the differing pixels are reported.
- `Test_Embedded_ScalarBar.py` drives the embedded native view with real Win32 mouse messages, so the events take
  the interactor path: a drag on the bar moves the bar and not the camera, a drag beside it turns the camera, and a
  real double click on a label raises the C# event with that label.

### Result: 20 of 20 checks pass, embedded 7 of 7

- **Layout identical in all 17 states**: every rectangle, highlight, label list and value edit box, including the
  drag (170, 120), the clamp at the window edge (614, 202) and the quadrant move on resize (774, 142).
- **Double clicks identical**: minimum label, maximum label (edit box, text, limits) and title (legend settings).
- Pictures: coverage 1.0000 everywhere; 1-9 % of the pixels of the widget differ by more than 24 levels, all of it
  glyph antialiasing. The colors match within one level, the highlight blend included.
- Slice A (36 of 36) and the groundwork checks still pass with the widget layer added.

### Fix cycles

1. (harness) The color spectrum checks min < max on every assignment; the test sets a large maximum first.
2. (port) The widget came out 4 px narrower. The C# widget joins its lines with `Environment.NewLine`, and VTK 5.8
   measured the carriage return as the invisible CR glyph of Arial, as wide as a space; VTK 9 measures it as
   nothing. Keeping `\r\n` did not help; measuring the CR as a space reproduces every width exactly (two builds).
3. (mirror) Hiding the maximum color had no effect on the native bar: `vtkMaxColorSpectrum.DeepCopy` does not copy
   `MinColorVisible` / `MaxColorVisible`, and the mirror read them from the copy. The old widget takes them from the
   argument; so does the mirror now. **A latent bug in the old code**: any later reader of `_colorSpectrum` gets the
   default visibility.
4. (render) The bar was one row higher: VTK 5.8 drew a 2D actor at its position truncated to a whole pixel, VTK 9
   draws at the fraction. The native widget truncates the drawing positions and keeps the fractions for the hit
   boxes, as the C# did; the actors are placed again on every move.
5. (render) Text was one pixel left of and one above the 5.8 glyphs at the same position: a fixed text shift
   (+1, -1).
6. (check) A press beside the widget goes on, on the ActiViz path, to pick the model, and the Controller shows the
   hidden parts again; the test hides them before the picture. The dark mask threshold also flipped on line pixels
   at 127 / 140; coverage now allows the same 24 levels as the pixel count.
7. (port) After the scalar bar joined the mirror, slice A showed an empty white box: `BackgroundVisibilityOn` and
   `BorderVisibilityOn` add their actors even to a hidden widget, and the mirror set up hid the bar before setting
   the frame. **A latent bug in the old widget** too (the old init avoids it only by its call order); the port adds
   the frame actors only while the widget is visible.
8. (harness) pythonnet needs typed placeholders for `Rectangle` and `string` out arguments.
9. (port, found in the user's review) In the interactive check the color boxes showed only after the mouse
   entered the bar. Turning the background on puts the background actor last, over the content a subclass adds,
   until the next restacking; the C# widgets restacked only the border and the text. The app hides the legend and
   shows it again after setting its frame, which rebuilds the order, so neither the app nor the comparison showed
   it. The port restacks the whole widget over the background, and the embedded check now tests that the colors
   show before any mouse event. The toggle script also takes the frame from the legend settings (transparent by
   default) instead of a white frame.

### Findings for the full port

- **2D placement is a rendering default like the polygon offset.** The truncated actor positions and the text shift
  apply to every 2D widget (status block, scale bar, color bar, text and arrow widgets, the triad), so they belong in
  the shared `BorderWidget` / `TextWidget` once. One residual: in one of 17 states a multi-line label column sat one
  row higher (the VTK 9 bitmap placement of that text block); it passes the tolerance and shows only side by side.
- **Exact layout is achievable and checkable.** With the text widths and the truncation reproduced, every hit box
  agrees to the pixel, which keeps saved widget positions and the value edit boxes valid.
- **Event-heavy code ports with its event model.** The widget set is a direct port of the widget part of the style.
  The embedded window feeds it from an interactor observer whose priority is above the style and which aborts
  handled events; a host feeds the same path through the C API. Double click timing uses the system double click
  time on the native side.
- **The C# side keeps .NET formatting.** Number formats, culture and the title texts stay in C#; only finished
  strings cross the API. The same holds for the status block, the min/max annotations and the probe widget.
- **Porting finds latent bugs**: two in this slice (the spectrum copy and the frame of a hidden widget). The port
  fixes the second; the first is logged for master.
- **The mirror approach scales to widgets**: hooks in the Controller-facing methods of `vtkControl` plus a "scalar
  bar only" mode, with the old path driven through its style handlers by reflection.

### Rate

About 2,000 lines of old C# ported to about 1,920 lines of C++ and C API and 470 lines of C# in 36 minutes, with
5 port/render fix cycles, at 10 % of the 5 hour window.

User review time: to be noted by the user. For the interactive feel, run `VtkPilot/Phase3/Toggle_Native_ScalarBar.py`
from the Python panel of a visible PrePoMax: the first run shows the VTK 9.7 view with a mesh and the native scalar
bar and leaves it open (double clicks are echoed to the output), the next run closes it. (`Test_Embedded_ScalarBar.py`
is the automated check and closes the view when it is done.)
