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
| 1. Groundwork | | | | | | | | |
| 2. Slice A | | | | | | | | |
| 3. Slice B | | | | | | | | |
| 4. Slice C | | | | | | | | |

Usage is read from the session usage card (`get_usage`): the 5 hour and weekly percentages of the Team plan at the
start and end of each phase, plus the context tokens of the session. Token counts per phase are not reported by the
app directly, so the percentage difference is the cost measure.

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

