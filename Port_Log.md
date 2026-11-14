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

## The user's review of the native window

On 2026-11-06, after the leftovers of W and S, the user looked at the native view in the window:

- **Fixed:** turning the native view off now gives its camera to ActiViz, as turning it on already did.
- **Fixed:** edges no longer show through the faces at a large zoom (the line offset is gone; stream G's log).
- **Changed:** the model edges and the hatch of a section view are 1 px wide, the element edges stay 0.5 px. The
  comparison of the widths is `VtkPort/Logs/Edge_Width_Variants.png`.
- **Left as is:** the scale bar is drawn a little lower; the user accepted it.

**Checks.** The full `Run_Port_Checks.sh` after the width change: all passed, S4 with its one known difference.

## The user's second review (2026-11-07)

- **Changed:** the element edges are 1 px wide too.
- **Fixed:** the tick of a hidden edge at a corner of a steep face: the polygon offset of VTK 5.8, (1, 1), instead of
  (2, 1) (stream G's log).
- **Fixed:** the lines of the scale bar and the other widgets are black: they lay on the border between two pixel rows
  (stream W's log).
- **Fixed:** the colored surfaces of the loads, boundary conditions and constraints were missing in the native window
  when they had been drawn before it was switched on (vtkControl, stream G's log); `hinge_surface_loads` checks it.
- The checks: a limit for the scenes that show the mesh, the widget compares and the element-wise section scenes are
  adjusted, each with its reason in the stream logs.

**Checks.** The full `Run_Port_Checks.sh`: all passed, S4 with its one known difference.

## The user's third review (2026-11-07)

- **Fixed:** the undeformed wireframe of the results is smoothed (stream G's log).
- **Fixed:** the standard views of the toolbar end in the native window where they animate, and a hover over the tree no
  longer flickers the view: the ActiViz window painted over the native view, and the two cameras are kept together
  (stream W's log).
- **Fixed:** the colors annotated by mesh type, material and section are as bright as in ActiViz and the legend
  (stream G's log).
- New checks: `hinge_annotate_materials`, `hinge_result_undeformed_wireframe`, and four camera checks in
  `integration_window`.

**Checks.** The full `Run_Port_Checks.sh`: all passed with the fourth review below, S4 with its one known difference.

## The user's fourth review (2026-11-07)

- **Fixed:** a double click on a part or the background, and on the minimum or maximum of the scalar bar.
- **Fixed:** a click on the triad animates the camera.
- **Fixed:** the hover highlight covers the black border of a widget.
- **Fixed:** no flicker of the highlight on a right click in the geometry view.
- **Fixed:** the transparency of the parts (a new native call; stream G's log).
- The details are in the stream logs (W and G). Not done: a renamed part keeps its old name in the native view; the raw
  selection mode without the widgets area reports no double clicks.

**Checks.** The full `Run_Port_Checks.sh`: all passed, S4 with its one known difference.

## The user's fifth review (2026-11-07)

- **Fixed:** no flash of the highlight on a right click, and none on a click with translucent parts (the native pick
  showed its own render).
- **Fixed:** translucent parts show their edges dark, not as white lines.
- **Fixed:** the undeformed wireframe is no longer drawn gray over the parts: the native view blends translucent
  geometry as VTK 5.8 did, without the order independent translucency of VTK 9.
- **Changed:** the widget highlight is translucent again; the black border of the widget is hidden under it.

**Checks.** The full `Run_Port_Checks.sh`: all passed, S4 with its one known difference.

## The user's sixth review (2026-11-07)

- **Fixed:** switching the exploded view off no longer shows the parts unexploded for a frame before the animation.
- **Fixed (after the user named it the red highlight):** the selection highlight blinked on a right click, in the results
  view most of all. A click clears the highlight and draws it again, and each `RenderScene` handed the camera of the
  control to the native view, whose `SetViewParameters` renders at once: the frame without the highlight was shown.
  The native view keeps its own camera distance, so the camera was handed over every time; it is handed over now only
  when it shows another view (stream W's log).

**Checks.** `geometry_g4`, `geometry_g10_exploded` and `integration_window` passed with the exploded view fix; the full
`Run_Port_Checks.sh` with both: all passed, S4 with its one known difference.

## The user's seventh review (2026-11-07)

- **Fixed:** the arrow keys turn the native view (the keyboard went to the ActiViz window behind it).
- **Fixed:** the box zoom animates.
- **Fixed:** the mesh lines of a selected surface are black over its color.
- **Fixed:** reference points are yellow inside the black ring and stay visible when zoomed.
- **Fixed:** renaming a part (a new native call).
- **Fixed:** the follower view and transformation points in the highlight colors.
- **Fixed:** a section view on a result with a symmetry, and a new result field on the transformed copies.

The details are in the stream logs (W and G). **Checks.** The full `Run_Port_Checks.sh`, from a copy of the build while
the user's PrePoMax held the build folder: all passed, S4 with its one known difference.

## The user's eighth review (2026-11-08)

- **Fixed:** the arrow keys turn the native view; the main form handed them to vtkControl, which fed the ActiViz view.
- **Fixed:** the mesh lines on a surface symbol are the part's own, not two lines over each other.

The details are in the stream logs (W and G). **Checks.** The full `Run_Port_Checks.sh`: all passed, S4 with its one known difference.

## The user's ninth review (2026-11-10)

- **Confirmed:** the arrow keys work.
- **Changed:** the colored center of a reference point is 2 px smaller, so its black ring is thicker (Stream G).
- **Fixed:** round dots (reference points, nodes) have a smoothed edge instead of a square one (Stream G).
- **Fixed:** a reference point stays drawn with every part hidden, also after its highlight (Stream G).
- **Fixed:** the space hides and shows the parts selected in the view (Stream W); confirmed by the user.
- **Changed:** with the smooth dots, the colored center of a reference point is a pixel larger again (Stream G).

**Checks.** The full `Run_Port_Checks.sh`: all passed, S4 with its one known difference.

## The native view by default (plan step I.2, first half; 2026-11-10)

On the user's "Yes, make it default" after the ninth review.

- **The VTK 9 view is on by default**: once the viewport is shown, FrmMain switches the native view on in the window
  with every area (`vtkControl.SetNativeView`, which starts it from the camera of the ActiViz view; the caller
  redraws). The ActiViz view keeps running underneath, as the review mode had it.
- **The fallback**: Settings > Graphics > Renderer > Viewport, "VTK 9" or "ActiViz (VTK 5.8)"
  (`GraphicsSettings.ActiVizView`, applied by `Controller.ApplySettings` through `FrmMain.SetNativeView`). The field
  is `[OptionalField]` and off by default, so a settings file of an older build loads and reads as the VTK 9 view.
- **Not done**: the plan's run with the mirror reversed (ActiViz off screen behind a native window that holds the
  data) is not a mode of this code; the native view in the window is still fed by the ActiViz path, which the checks
  compare it with. That reversal comes with removing ActiViz (step I.2, second half).
- Next: a day of normal use (review round 4), then the removal of ActiViz.

**Checks.** At startup the native view is in the window with every area, both parts of the hinge mirrored, no mirror
error. The full `Run_Port_Checks.sh` with the default on: all passed, S4 with its one known difference. `Run_Tests.py --filter Test_Api Test_Selection Test_Workflow Test_Model_Parts Test_Result_Field Test_Rpc_Live`: 13 of 13 passed (with `Test_Analysis`).

## The user's day of use (2026-11-11)

- **Fixed:** the outlines in the secondary highlight color (a constraint's second surface) were white (Stream G).

**Checks.** The full `Run_Port_Checks.sh`, from a copy of the build while the user's PrePoMax held the build folder:
all passed, S4 with its one known difference.

## master merged into the port (2026-11-13)

On the user's "Yes, merge into port": master's commits up to `10a711f35` (taken from the master of the
"PrePoMax v3.0.n dev" clone, three of them not yet pushed) are merged into `port/vtk-native` so that they can be
tested in the VTK 9 view; master itself is unchanged and stays without the native view. The merge had no conflicts.
The user's uncommitted contact-pair files in this folder were an earlier draft of master's `f3d57527f`, `fbb50d1ea`
and `ab029c2bf` and are kept in `git stash` ("Contact pair draft superseded by master"). Master can be merged in again
whenever it moves on; `port/vtk-native` must not be rebased or squashed, so that the final merge into master stays a
plain one.

- **Fixed, a gap the merge opened:** master's `24624d451` adjusts the camera of the ActiViz view once at the end of a
  redraw of the model symbols (`vtkControl.SuspendCameraAdjustment`), because every adjustment measures every actor.
  The native view still adjusted its camera after every added actor, so a redraw of 1344 ties on 512 cubes took 70 s
  in the VTK 9 view against 0.45 s in ActiViz. The suspension now reaches the native view
  (`vtkn_view_set_camera_adjustment_suspended`, `View::AdjustCameraAfterAdd`), and the same redraw takes 6.2 s.
- **Found, not fixed:** the remaining 6 s are the construction of the actors, about 1.7 ms each for a surface of 20
  points. The VTK 9.7.1 libraries are built without optimization: the first configure of `vtk-9.7.1-build-x64` found
  no compiler, CMake kept the empty compiler flags in its cache, and the Release projects have no `/O2` (the 87 MB
  `vtkCommonCore-9.7.dll`; `vtk-8.2.0-build-x64` has `/MD /O2 /Ob2 /DNDEBUG`). A rebuild of VTK 9.7.1 with the
  default flags is for the user to decide.

**Checks.** After the merge: the full `Run_Port_Checks.sh`: all passed, S4 with its one known difference;
`Run_Tests.py` with the groups master's changes reach (contact search, composite sections and results, constraints,
equations, regions, split cells, selection, workflow, job, results, API, RPC, audits): 37 of 37 passed.
`Test_Window_Events.py` checks that an actor added while the camera adjustment is suspended leaves the native camera
until the suspension ends.

## The optimized VTK build and the ambient occlusion (2026-11-14)

On the user's "Yes, rebuild VTK" and "look into the KitwareVTK folder where ambient occlusion was added using the new
ActiViz and bring it to this VTK".

- **VTK 9.7.1 with optimization:** `vtk-9.7.1-build-x64-opt` beside the old build, from the same source with the same
  options (shared, x64, Release, the StandAlone, Rendering and Imaging groups, no FFMPEG) and the default compiler
  flags; 160 DLLs, `vtkCommonCore-9.7.dll` 22.8 MB instead of 87.8 MB. `vtkNative/build` is configured with its
  `VTK_DIR` (`Vtk_Native_Pilot_Plan.md`, Inputs). The old build is untouched. The redraw of the symbols of 1344
  ties on 512 cubes: 1.7 s (6.2 s with the unoptimized libraries, 0.35 s of it the ActiViz view underneath). The
  picking time of `selection_s4` did not change (10.4 s), so that was not the libraries.
- **Ambient occlusion in the VTK 9 view** (Settings - Graphics - Lighting: Ambient occlusion, its radius relative to
  the model size and its samples; `GraphicsSettings` fields are `[OptionalField]` and off by default). Ported from
  `vtkControl.cs` of `KitwareVTK/PrePoMax-v2.6-ActiViz-9.7.1-test` to `vtkNative/src/Geometry/
  vtkNativeAmbientOcclusion.cpp`: the occlusion pass on the opaque pass of the base layer, inside a supersampling pass,
  the radius from the diagonal of the visible model before every render, the line widths and point sizes of the base
  layer scaled for the supersampling, and the picks through the plain passes. It needs the patch of `vtkSSAOPass.cxx`
  that the VTK source already had from that work, now also `vtkNative/patches/vtk-9.7.1-ssao-background.patch`.
  Three things differ from that copy:
  - the window keeps its multisampling, so the symbols, the highlights and the widgets of the other layers stay
    smooth (that copy switched it off);
  - the 2D actors of the base layer (captions, labels of coordinate systems) are drawn after the supersampling, or
    they would come out sqrt(5) times smaller;
  - a magnified picture is rendered in tiles that overlap by the sampling radius (`ReadPixelsInOverlappingTiles`).
    That copy rendered it once in a window enlarged to the picture, which leaves everything sized in pixels (symbols,
    widgets, the scale bar) at the size of the screen; the tiles of `vtkWindowToImageFilter` magnify as before, but
    the occlusion leaves a step of up to 222 levels where two tiles meet.
  The ActiViz (VTK 5.8) view has no ambient occlusion; the settings are hidden when it is chosen.
- **Cost** on 512 parts with 1344 ties in a 1200 x 800 view: a render 94 ms without and 132 ms with the occlusion, a
  pick 181 ms and 203 ms.
- **Not checked:** the look in the user's own window, a results view, a section view and translucent parts with the
  occlusion on.

**Checks.** `Test_Window_Events.py` with the occlusion: the model is shaded, the background kept, 25 picks find what
they find without it, a picture magnified twice has no step through its middle (it fails with the overlap of the
tiles removed: 19829 against a typical 5501), and switching it off gives the picture from before. The full
`Run_Port_Checks.sh` on the optimized VTK: all passed, S4 with its one known difference.
After the last change (the render before a pick without the occlusion passes): the selection suites, `geometry_g1`,
`geometry_g5` and `Test_Window_Events.py` passed again, and `Run_Tests.py --filter Test_Api Test_Selection
Test_Workflow Test_Model_Parts Test_Result_Field Test_Rpc_Live Test_Model_Interactions Audit_`: 17 of 17 passed. The
dots and the names of reference points are drawn with the occlusion on as without it.

## The user's review of the ambient occlusion (2026-11-14)

On the user's "the section view works but moving the slider causes the scene to flicker - when it flickers the scalar
bar jumps up/down for 1-2 px" and "the transparency works but using 240 it is obvious that the depth sorting is not
working".

- **Fixed:** the flicker while the slider of the section view moves. It was the picture of the ActiViz window under
  the native view: of 295 captures of the screen during 60 steps of the slider, 29 showed the ActiViz picture (no
  occlusion, its own fonts, the scalar bar one pixel lower). `WS_CLIPSIBLINGS` on the ActiViz window does not hold
  back the buffer swap of an OpenGL window, so behind the native view the ActiViz window no longer swaps
  (`KeepActiVizWindowBehindNativeView`); its pickers and `RenderToPNG` read the back buffer as before. The native
  view itself rendered one frame per step with the scalar bar in place. After the fix: 261 captures, none with the
  other picture. The flicker was there with every render of the control since the native view is shown in the
  window; the occlusion made the two pictures differ enough to see it, and the section view renders both views at
  every step.
- **Fixed:** translucent faces are blended by depth. Both views blended the translucent geometry in the order of its
  actors, so a part drawn later covered a part in front of it and every hidden edge showed through a face of any
  opacity; which part won changed from run to run in the ActiViz view too. The base layer of the native view now
  peels the translucent geometry by depth (`vtkDualDepthPeelingPass`, four peels, so the eight outer layers are
  sorted) whenever a translucent face is visible, with and without the occlusion
  (`vtkNative/src/Geometry/vtkNativeAmbientOcclusion.cpp`):
  - peeling draws into buffers without multisampling, so the base layer is supersampled then, as with the occlusion;
    the mesh lines are a little softer while a part is translucent;
  - the edge lines of the opaque parts, which are translucent always, are blended once before the peeling and kept
    out of it by a key on their props (`MarkPropsForDepthSorting`), or every peel would draw them again;
  - without a translucent face the base layer is drawn as before: no pass without the occlusion, the plain
    translucent pass with it;
  - the occlusion shades the opaque faces only, so a translucent part casts and receives none.
  The ActiViz (VTK 5.8) view is unchanged.
- **Changed:** the scripts of the checks switch the ambient occlusion off (`vtkport.py`, the recorders of the pilot
  slices), since the settings of the build folder they copy may have it on and the ActiViz view they compare with has
  none.
- **Cost** of the sorting on 512 parts with 1344 ties in a 1200 x 800 view: a render of the opaque model 95 ms, and
  133 ms with the occlusion, as before; with one part translucent 149 ms (90 ms unsorted) and 214 ms with the
  occlusion (127 ms); with all 512 translucent 439 ms (95 ms unsorted) and 508 ms with the occlusion (128 ms), since
  every peel draws every translucent part again.
- **Not checked:** the look in the user's own window; a results view and a section view with translucent parts.

**Checks.** `Test_Window_Events.py`: the ActiViz window does not swap behind the native view; parts at alpha 250 look
as the opaque ones by the mean colors of 8 x 8 blocks (25 blocks differ, 673 with the old library), and the same
with the occlusion on as without it. Both fail on the build from before. The full `Run_Port_Checks.sh` on a copy of
the build with the user's settings (occlusion on): all passed, S4 with its one known difference. `Run_Tests.py
--filter Test_Api Test_Selection Test_Workflow Test_Model_Parts Test_Result_Field Test_Rpc_Live
Test_Model_Interactions Audit_`: 17 of 17 passed.
