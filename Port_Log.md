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
| I. Integration (merge) | 2026-11-06 07:55 | 2026-11-06 08:15 | 1 user turn | 3 % -> 4 % / 19 %; context 198k -> 205k | 1 compile + 1 behavior + 2 check | 0 (merge) | window check 125, routing 10 | |
| G6 - G10. Rest of geometry | 2026-11-06 15:05 | 2026-11-06 16:20 | 1 user turn, 7 commits | 8 % (15:49) -> 12 % / 20 % -> 21 %; context 529k at the end | 2 render + 3 compile + 6 check | ~2,100 (the list the stream left), plus ~200 reused from C# | C++ ~1,280, C# ~320, harness ~410 | |
| W + S. Leftovers of the widgets and selection | 2026-11-06 16:22 | 2026-11-06 17:25 | 1 user turn | 12 % -> 22 % / 21 % -> 22 %; context 529k -> 730k | 2 port (copies picked, then reverted; box picks) + 2 found (selection box, box zoom) + 5 check | ~250 | C++ ~260, C# ~110, checks ~400 | |

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

## Integration - merging the streams

Started 2026-11-06 07:55 on the user's go-ahead. The three branches were merged into `port/vtk-native` in the order
G, W, S, so that S's highlights could move onto G's selection layer.

### Conflicts and what was kept

- **Text conflicts**, each resolved by keeping both sides: the suites in `Run_Port_Checks.sh`, the VTK components
  in `CMakeLists.txt` (the union of G's and S's filter modules), the compile items in `vtkControl.csproj`, the parts
  only branch of `vtkControl.SetNativeMirrorPartsOnly` (G's layer drawing and W's hiding of the native widgets) and
  the private members of `vtkNativeView.h`.
- **One selection layer.** G and S each made a renderer for layer 2. S's `GetHighlightRenderer` now returns G's
  `SelectionRenderer` (made in `InitializeGeometry`), and S's `struct Highlight` became `HighlightEntry`, since G
  holds a member named `Highlight`. W's style now draws the rubber band of the box selection in that layer too, as
  `vtkInteractorStyleControl` does, instead of the widget layer.
- **A duplicate accessor**: G and S both added `PartActor::GetModelEdges`; one was removed.
- **The mouse selection in the native window had two handlers.** W's style raises the pick, hover and clear events
  of `vtkInteractorStyleControl` (`VTKN_EVENT_POINT_PICKED_ON_*`, `_CLEAR_MOUSE_SELECTION`), and S had wired its
  own handler of the raw button and move events. With all three areas on (S's review script), both handled a click.
  W's handler now also routes the hover (to `HoverWithNativeMirror`) and the clear, and S's raw handler is wired
  only when the widgets area is off. A click still gave one selection node before the fix (the controller took the
  second pick as the same selection), so the cost was double work rather than a wrong selection.

### Checks

- `VtkPort/Harness/Test_Window_Events.py` (suite `integration_window`), new: the native view in the window driven
  by Win32 messages, with and without the widgets area. One selection handler is subscribed, a move over the model
  draws a hover after the style's selection timer, and a click adds one selection node and selects one element. It
  fails on the build before the fix (both handlers subscribed).
- The full `Run_Port_Checks.sh` on the merged build (before the window fix), 07:59 - 08:06, with a private `TEMP`:
  all passed - groundwork, geometry scenes 36/36, slice A 36/36, slice B 20/20 and the embedded check, slice C
  1,960/1,960, G1 60/60, G2 60/60, G3 24/24, G4 72/72, G5 12/12, PNG 4/4, W1 35/35, W2 20/20 and the embedded
  check, W3 6/6, S1 1,960/1,960, S2 74/74, S3 1,372/1,372.
- After the window fix, `--filter widgets selection integration`, 08:07 - 08:13: all passed again, with
  `integration_window`.

### Effort

07:55 - 08:15, about 20 minutes, in one user turn. The usage window had reset before the merge: 3 % -> 4 % of the 5 hour window, the weekly at 19 %; context 198k -> 205k at the end.
Fix cycles: 1 compile (the duplicate accessor), 1 behavior (the window handlers), 2 check (the hinge's saved
camera misses the model, the hover waits for the selection timer).

### Still open after the integration

- The rest of stream G (see "Not finished" above), and the user's decision on `NativeExactPicking`.
- The reviews: camera and widgets (W), geometry (G), selection (S); S's review script is now the one that shows all
  three areas together.

## Stream G finished (G6 - G10)

Started 2026-11-06 at about 15:05 on the user's "Finish G next", on `port/vtk-native`; the details, fix cycles and the
added review points are in `VtkPort/Logs/Stream_G.md` ("Finishing the stream").

- **Ported:** the vector and tensor plots (G6), captions, labels and the in-place updates of measurement annotations
  (G7), the coordinate system symbols on W's triad geometry (G8), the element-wise section view (G9), and in G10 the
  follower view, the directional values of transformed copies and the steps of the exploded view animation. The
  section view of an animated part needed no port (vtkControl makes it again at every frame), only a check.
- **Also fixed:** `RenderScene` now repaints the native view in the window, which a change without a native event
  (an animation step) left undrawn.
- **Check limits added**, each with its reason in `scenes.py`: the plots draw thousands of dots whose smoothed edges
  VTK 9 does not reproduce (7 % differing pixels allowed); scenes with much text cover 0.99 of the outline (FreeType
  descenders); the section seen edge on from the right 0.998. Captions in the base layer are drawn natively, though
  ActiViz never drew them (PrePoMax does not use them).
- **New suites:** `geometry_g6` to `geometry_g10`, `geometry_g10_follower` (`Test_Follower_View.py`),
  `geometry_g10_directional`, `geometry_g10_exploded`.

**Checks.** The full `Run_Port_Checks.sh`, 16:04 - 16:14, with a private `TEMP`: all passed - groundwork, geometry
scenes 36/36, slice A 36/36, slice B 20/20 and the embedded check, slice C 1,960/1,960, G1 60/60, G2 60/60, G3 24/24,
G4 72/72, G5 12/12, PNG 4/4, G6 48/48, G7 36/36, G8 12/12, G9 36/36, G10 24/24, the follower view, directional
copies 24/24, exploded animation 12/12, W1 35/35, W2 20/20 and the embedded check, W3 6/6, S1 1,960/1,960, S2 74/74,
S3 1,372/1,372, the window check.

**Effort.** About 15:05 - 16:20 in one user turn, 7 commits; the 5 hour window at 8 % at 15:49 and 12 % at the end,
the weekly 20 % -> 21 %. Fix cycles: 2 render (unlit result colors, caption text height), 3 compile, 6 check (the
dot and text limits, the base layer caption, float cameras of the element-wise section, the edge-on sliver, numpy
inside PrePoMax).

### Still open

- Stream W: the triad's hand cursor, pausing the style during view animations. Stream S: the probe label during a
  query, picking on section views and transformed copies (the element-wise skins are not picked natively either), box
  selection by drag in the window.
- The user's decision on `NativeExactPicking`, and the reviews (W, G with the additions 10 - 14, S).

## Leftovers of streams W and S

Started 2026-11-06 at about 16:22 on the user's "Port W and S next"; the details are in the two stream logs
("Finished after the integration").

- **W:** the hand cursor of the triad in the native window, and the native style takes no input while the camera of
  the control animates.
- **S:** picks on section views (the plain cut and the element-wise skins and faces) resolve to their parts as in
  ActiViz; area picks also report parts drawn in the area behind other geometry, as the OpenGL selection mode of VTK
  5.8 did; the probe label of a query is shown through the native hover.
- **Found by the new checks:** the selection box setting and the box zoom of the toolbar never reached the native
  style, so a drag in the native window clicked instead of selecting a box. Both are mirrored now.
- **New checks:** `selection_s4`, `selection_s5_probe`, and four more checks in `integration_window` (box selection by
  a drag, the toolbar box zoom, the triad cursor, a click while animating); the cursor, animation and probe checks
  fail with their fix switched off.
- **Left as is, for the `NativeExactPicking` decision:** a pixel on the rim of a hole and a part behind a transformed
  copy resolve to another point than in ActiViz, which rebuilt the point from the depth buffer.

**Checks.** The full `Run_Port_Checks.sh`, 17:05 - 17:18: all passed - every suite of the stream G run above, and
S4 with its one known difference (the hole rim pixel, in the four modes that resolve it to an item; 2,936 of 2,940
picks equal), the probe label, and the window check with its four new checks.

**Effort.** About 16:22 - 17:25 in one user turn; the 5 hour window 12 % -> 22 %, the weekly 21 % -> 22 %.
