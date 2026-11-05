# VTK native port - log

The log of the full port, run from `Vtk_Native_Port_Plan.md` on branch `port/vtk-native`. The pilot before it is
in `Pilot_Log.md`.

## Summary table

| Step | Start | End | Agent turns | Usage (5 h / weekly, % of plan) | Fix cycles | Old C# ported | Written | User time |
|---|---|---|---|---|---|---|---|---|
| 0. Preparation | 2026-11-05 17:58 | 2026-11-05 18:09 | 1 user turn, ~45 tool calls | 25 % -> 28 % / 12 % -> 13 %; context 697k -> 763k | 3 (harness) | 0 (restructure) | split of the API, C# split, switches 80, harness 391 | |
| G. Geometry | | | | | | | | |
| W. Widgets and interaction | | | | | | | | |
| S. Selection | | | | | | | | |
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
