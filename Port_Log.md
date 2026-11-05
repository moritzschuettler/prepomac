# VTK native port - log

The log of the full port, run from `Vtk_Native_Port_Plan.md` on branch `port/vtk-native`. The pilot before it is
in `Pilot_Log.md`.

## Summary table

| Step | Start | End | Agent turns | Usage (5 h / weekly, % of plan) | Fix cycles | Old C# ported | Written | User time |
|---|---|---|---|---|---|---|---|---|
| 0. Preparation | 2026-11-05 17:58 | 2026-11-05 18:09 | 1 user turn, ~45 tool calls | 25 % -> 28 % / 12 % -> 13 %; context 697k -> 763k | 3 (harness) | 0 (restructure) | split of the API, C# split, switches 80, harness 391 | |
| G. Geometry | 2026-11-05 18:12 | 2026-11-05 19:59 | 1 session turn, 7 commits | 29 % -> 52 % / 13 % -> 18 % (account wide) | 6 render + 8 check | ~4,150 of ~11,800 | C++ 3,273, C# 933, `vtkControl.cs` 66, harness 211 | |
| W. Widgets and interaction | 2026-11-05 18:12 | 2026-11-05 19:12 | 1 session turn, 2 commits | 29 % -> 51 % / 13 % -> 17 % (account wide) | 6 port + 6 harness | ~3,600 ported, ~1,100 routed | C++ ~4,000, C# ~950, checks ~1,060 | |
| S. Selection | 2026-11-05 18:12 | 2026-11-05 18:52 | 1 session turn, 3 commits | 29 % -> 47 % / 13 % -> 16 % (account wide) | 2 port + 5 harness | ~833 | C++/C API ~670, C# ~710, harness ~520 | |
| I. Integration | | | | | | | | |

The plan's target: 8 - 10 h of agent time, 4 - 6 h of wall clock with three streams, 4 review rounds.

## Step 0 - Preparation

Started 2026-11-05 17:58, ended 18:09 (11 minutes against the plan's 45).

### What was done

- **Branch** `port/vtk-native` from `pilot/vtk-native`, with the plan committed.
- **The C API split by area.** `vtkNative.h` now only includes `vtkNativeCore.h` (library, views, cameras, pictures),
  `vtkNativeGeometry.h`, `vtkNativeWidgets.h` and `vtkNativeSelection.h`. The entry points are in
  `vtkNativeApi.cpp` and `src/<Area>/vtkNativeApi<Area>.cpp`, sharing the guard in `vtkNativeGuard.h`; the C++
  sources moved into `src/Geometry`, `src/Widgets` and `src/Selection` (git renames, history kept). Each area has
  its own API version (`VTKN_<AREA>_API_VERSION`, all 1; the core is 5), returned by `vtkn_get_area_api_version`
  and checked one by one at load.
- **The C# layer split the same way**: `VtkNativeMethods` and `VtkNativeView` are partial classes with one file per
  area in `vtkControl/Native/<Area>/`, and the area types moved there.
- **Per-area switches.** `vtkControl.SetNativeAreas(VtkNativeArea areas, bool inWindow)`: the areas that are on send
  their calls to a native view, either the off screen mirror the checks compare or the native view in the window
  for a review. `EnableNativeMirror` is now the shorthand for all areas on the mirror. ActiViz keeps running
  underneath; the plan was corrected on this point (its parts still serve the old selection, symbols and widgets).
- **The harness** in `VtkPort/Harness`:
  - `vtkport.py`, the in-process helpers the pilot recorders each copied (fields and methods by reflection, the
    user interface thread, viewport size, switching areas and target, framing both paths, picture pairs);
  - `scenes.py`, named scenes (model, launch arguments, setup): solid geometry and mesh, the hinge, the hinge result
    with DISP and MISES; a stream adds its own;
  - `Record_Scene.py`, the generic recorder: a scene through both paths in two viewport sizes and six views;
  - `checks.py`, the comparisons (cameras, pictures with the slice A thresholds, side by side pictures);
  - `Run_Port_Checks.sh [--filter <part> ...]`, one runner for every suite, on a copy of the build folder;
  - `Toggle_Native_Window.py`, the review script: shows the native window with the ported areas and the ActiViz
    camera, and hides it on the next run.

### Checks

`bash VtkPort/Harness/Run_Port_Checks.sh`, 3.5 minutes: groundwork passes; the new `geometry_scenes` suite (geometry
area alone) 36 of 36; slice A 36 of 36; slice B 20 of 20 and the embedded check; slice C 1,960 of 1,960 picks.
The window target was checked by running the review script twice in one session (shown with the parts and the
ActiViz camera, then hidden with nothing left behind).

### Fix cycles (all harness)

1. `git mv` needs the target folder to exist.
2. The recorder's `pmx` is not in `__main__` inside PrePoMax; the recorder hands it to `vtkport`.
3. The repository path has spaces; the runner quotes the pilot runners' paths.

### Notes for the streams

- Each stream owns its area's header, API source, `src/<Area>` and `Native/<Area>` folders, and its regions of
  `vtkControl.cs` (the plan lists them); it raises only its own area version.
- A stream adds scenes to `scenes.py` and a suite to `SUITES` in `Run_Port_Checks.sh`; `--filter <stream>` runs its
  checks.
- Selecting by clicking in the native window is not wired yet: the selection highlights (S2) are not ported, so a
  click there would select without showing it. Stream S adds it with S2.

## Streams G, W and S

The three streams ran at the same time from 18:12, each in its own worktree cut from `port/vtk-native` at
`0a7748af4`; each has its own log on its branch (`VtkPort/Logs/Stream_<X>.md`), which the integration brings here.
The usage figures are account wide, so each stream's difference includes the other two.

| Stream | Branch | Commits | Checks (full `Run_Port_Checks.sh`) |
|---|---|---|---|
| G | `claude/port-geometry-aec7b3` | `6d220f524` .. `606de0115` (7) | all earlier suites; G1 60/60, G2 60/60, G3 24/24, G4 72/72, G5 12/12, PNG 4/4 |
| W | `claude/port-widgets-401a67` | `bcefb2da8`, `1eed38b95` | all earlier suites; W1 35/35, W2 20/20, W3 6/6 |
| S | `claude/port-selection-010fc6` | `cf3b2e10f`, `8c316d5a2`, `9eaaa2b65` | all earlier suites; S1 1,960/1,960, S2 74/74, S3 1,372/1,372 |

### Not finished

- **Stream G** ported about 4,150 of its ~11,800 lines. Still missing: coordinate-system symbols (~820 lines, sharing
  their geometry with W's triad), captions and labels, vector and tensor glyphs (~450), the element-wise section
  view, the follower view, the section view of an animated part, directional values on transformed copies and
  in-place updates of measurement annotations.
- **Stream W:** the triad's hand cursor in the native window, the rubber band in the selection layer (waits for S),
  pausing the style during view animations. Gesture recognition and arrow placement stay in C# by design.
- **Stream S:** the probe label during a query (W), picking on section views and transformed copies (G4), box
  selection by dragging in the native window (W2); the highlight after a click shows only once G is merged.

### For the integration

- Files touched by more than one stream: `vtkControl.cs`, `vtkControl.csproj`, `vtkNative/CMakeLists.txt`,
  `vtkNativeView.h` (all three), `vtkNativeView.cpp` and `vtkNativeViewCamera.cpp` (G and W),
  `vtkNativePartActor.h` (G and S), `Run_Port_Checks.sh` (all three).
- G and S each add a selection-layer renderer; keep G's `SelectionRenderer` and draw S's highlights into it.
- Slice B's embedded check now turns the camera with the middle button (W's style uses the left button for box
  selection), and the parts-only mode hides the native widgets.
- A new worktree needs copies of `packages/` and `Tests/` (both ignored by git), and parallel runs must use a private
  `TEMP`: the shared check folders collided between streams.

### Decisions and reviews for the user

- `vtkControl.NativeExactPicking` (S, on by default): exact point under the pixel and a 7 px catch distance, against
  the depth buffer point; the off setting is built but unchecked. Same question as the pilot's tolerance picks.
- Review round 2 (W): camera feel and widgets, `VtkPort/Widgets/Review_W2_Camera.py`.
- Geometry review (G): `Toggle_Native_Window.py` with `AREAS = ["geometry"]`, the 9-point list in `Stream_G.md`.
- Selection review (S): `VtkPort/Selection/Toggle_Native_Selection.py`, the list in `Stream_S.md`.
