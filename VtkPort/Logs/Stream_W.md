# VTK native port - stream W: widgets and interaction

The log of stream W of `Vtk_Native_Port_Plan.md` (slices W1 - W3), run in its own worktree from `port/vtk-native`
at `0a7748af4`, at the same time as streams G and S. Measured as the pilot was (`Pilot_Log.md`).

## Summary table

| Slice | Start | End | Usage (5 h / weekly, account wide) | Fix cycles | Old C# ported (non-blank) | Written | User time |
|---|---|---|---|---|---|---|---|
| Setup | 18:12 | 18:18 | 29 % / 13 % at the start | 2 (toolset, `Tests/`) | - | - | |
| W1 | 18:18 | 18:50 | 29 % -> 47 % / 13 % -> 16 % | 3 port (+2 harness/check) | ~2,480 | C++ 2,089 new + ~730 in existing files; C# 440 new + 215; checks 455 | |
| W2 | 18:50 | 19:01 | 47 % -> 50 % / 16 % -> 17 % | 2 port (+1 check) | ~1,150 | C++ 1,023 new + API/view ~200; C# 145 new + ~60; checks 606 (with W3) | |
| W3 | 19:01 | 19:06 | 50 % -> 51 % / 17 % | 1 port (+1 harness) | routing of ~560 (no VTK in it) | C# interface and adapter 45 + ~40 | |
| **Stream** | **18:12** | **19:12** | **29 % -> 51 % / 13 % -> 17 %** | **6 port (+6 harness/check)** | **~3,600 ported, ~1,100 routed** | | |

The plan's share for the stream was 2 - 3 h of agent time for about 6,100 lines; the stream took about an hour of
wall clock: W1 about 60 % of its share, W2 and W3 far under theirs. No slice ran over, so nothing was split.

The plan percentages are account wide: streams G and S used the same plan at the same time, so a difference is an
upper bound for this stream.

## Setup

- The runtime base folder copied into the worktree, CMake configured, the solution built. Two findings for every
  worktree (left in the shared notes folder for G and S): `GmshCommon` needs the v145 toolset, which only the
  MSBuild of Visual Studio 18 has, and the worktree has neither `packages` nor `Tests/` (gitignored), so both are
  copied from the main checkout; without `Tests/Joints` the groundwork, the hinge scenes and slice B fail.
- `Run_Port_Checks.sh` copies the build to `$TEMP/VtkPortBuild`; with three streams at once they would overwrite
  each other, so the stream runs it with its own `TEMP`.
- Baseline before any change: all suites pass (groundwork, geometry scenes 36 of 36, slice A, slice B 20 of 20 and
  the embedded check, slice C 980 of 980).

## W1 - The remaining widgets

Started 18:18 (5 h window 29 %, weekly 13 %), ended 18:50 (47 %, 16 %), against a share of about 55 minutes of the
plan's 2 - 3 h for the stream.

### What was ported

| Old C# (ActiViz 5.8) | Non-blank lines | Native port |
|---|---|---|
| `vtkMaxStatusBlockWidget` (layout, the box of the deformation scale factor, its press) | ~160 | `StatusBlockWidget` |
| `vtkMaxScaleWidget` (bar length from the camera, five fields, labels, centered on resize) | ~350 | `ScaleBarWidget` |
| `vtkMaxColorBarWidget` (boxes, labels, title) | ~380 | `ColorBarWidget` |
| `vtkMaxTextWithArrowWidget` (leader and head glyph, the corner kept in the plane of the anchor) | ~370 | `ArrowWidget` |
| `vtkMaxCoorSysWidget` (targets, axis bands, proximity, hover) | ~600 | `CoorSysWidget` |
| `vtkMaxCoorSysGeometry` (projection, cones, disks, labels) | ~300 | `CoorSysGeometry` (also for stream G's symbols) |
| the widget part of the style: camera, pan, rotate, wheel and middle button forwarding, proximity | ~60 | `WidgetSet` |

The native widgets sit behind one handle type, `vtkn_widget`, created by kind (`vtkn_widget_kind`); the probe is a
plain text widget. 28 entry points, `VTKN_WIDGETS_API_VERSION` 2. The scale bar asks C# to format its labels through
a callback (`vtkn_number_formatter`), so the .NET number formats stay in C#; the status block gets its lines
formatted, the color bar its labels after the dash replacement. Triad clicks and hovers are events of the area
(`VTKN_EVENT_WIDGET_TARGET_CLICKED`, `_HOVER_CHANGED`).

The C# side: `VtkNativeWidget` and two enums, and **the mirror lives in the old widgets**: every `vtkMax` widget
forwards each call it takes to its native twin (`SetNativeMirror`, `CopyStateToNativeMirror`), so the ~20 probe
call sites of the Selection region, the Settings methods and the arrow placement reach the native widgets without
a hook each. `vtkControl.NativeWidgets.cs` (a partial class in this stream's folder) creates the twins when the
widgets area is switched on, wires their events to the same handlers (`colorBarWidget_DoubleClicked`, ...) and
ports `coorSysWidget_TargetClicked` onto the native view; `vtkControl.cs` got 8 hook lines.

The shared parts of slice B moved down into the bases, as the pilot found they should: the 2D truncation and the
text shift are `BorderWidget::PlaceActor`, used by every widget, and the carriage return width is
`TextWidget::ToVtkText`. The scalar bar now uses both.

### How it is checked (`Run_Port_Checks.sh --filter widgets_w1`, about 60 s)

`VtkPort/Widgets/Record_W1.py` opens Joint_Hinge, switches the widgets area on with the mirror as the target, shows
only the widgets on both paths and goes through 26 states: the triad and scale bar, the status block, the color bar,
three arrows and their arrangement, the probe, four cameras, a resize to 960x540 and back, hovers on five widgets,
drags of the status block and of an arrow, a press on the scale factor, the triad's proximity and five triad clicks;
then three double clicks. `Compare_W1.py` requires every widget's rectangle, visibility, highlight, scale bar labels
and scale factor box to be identical, positions, sizes and leader lines to agree to 1e-3 px, the triad target on all
5,041 points of a grid around it to agree, each triad click to turn both cameras the same way (1e-9), and the double
clicks to open the same thing. Pictures as in slice B (coverage within a pixel >= 0.98).

### Result: 35 of 35 checks pass, and every other suite still passes

- Layout identical in all 26 states, through four cameras, the resize and the drags; leader lines to 1e-6 px.
- Triad: 5,041 of 5,041 grid points report the same target; the five clicks (a cone, a negative end, a shaft, a
  second cone and the origin) leave the cameras equal to 1.3e-15.
- Pictures: coverage 0.997 - 1.000, 1 - 3 % of the widget box differing by more than 24 levels (glyph antialiasing).
- 3 reported, not failed: a hover highlight one pixel to the left when a widget lies on a whole pixel. Both paths
  have the same position to 1e-6, but ActiViz reaches 112.99999 and VTK 9 113.00001 through the world to display
  step, and the highlight floors it.

### Fix cycles

1. (port) After a resize the native widgets moved one resize late. The native widgets saw the window size first on
   the first resize: the ActiViz window raised its modified event on every render, so a C# widget always knew the
   size before a resize, while the native window raises it only when the size changes. A widget now takes the
   window size when it joins the renderer, and the view checks its widgets after every `SetSize`.
2. (port/harness) The control repositions the triad in its resize handler before the mirror had the new size, so
   the native triad was clamped to the old width. The mirror is resized in the same handler, at the point where the
   ActiViz widgets see the new size.
3. (harness) `RenderScene` is private; the recorder calls it by reflection.
4. (check) The one pixel highlight on a whole pixel position is classified (above) instead of failed.
5. (port) Slice A failed 0 of 36 after W1: its parts only mode stops the ActiViz widget layer from drawing, while
   the new native twins (triad, scale bar) still drew in the mirror. The parts only mode now hides the twins and
   gives each one back the visibility of its widget afterwards.

### Rendering defaults found

- None new: the truncation of 2D positions, the text shift and the carriage return width of slice B were enough for
  every new widget once they sat in the bases. The cell colors with alpha of the triad (the half transparent
  negative axes) and the glyph of the arrow head draw the same in VTK 9 without changes.
- VTK API change met: `vtkTransformPolyDataFilter` is deprecated in 9.7; `vtkTransformFilter` replaces it.

### Latent bugs of the old code met

- `vtkMaxTextWidget.BackgroundVisibilityOn` puts the background over the color boxes of the color bar until the next
  restacking (the bug slice B fixed for the scalar bar); the native color bar restacks the whole widget. The check
  does not show it, because the control turns the background on when it sets the widget up; it shows when the
  background is switched off and on again in the settings while the color bar is visible.

## W2 - The interaction style

Started 18:50 (47 %, 16 %), ended 19:01 (50 %, 17 %).

### What was ported

`vtkInteractorStyleControl` without its widget part (the widget set of slice B and W1) became
`vtkn::InteractorStyle`, a `vtkInteractorStyleTrackballCamera` like the C# one: the middle button turns the camera
about the rotation center (shift pans, ctrl zooms slowly), a middle click picks the rotation center (the model
point through the native pick of slice C, or the center of the visible bounds beside the model), the orange cross
while turning, the wheel zooms at the mouse with the smooth zoom (accumulated notches approached over frames, the
frame budget that switches it off), ctrl for small steps, the arrow keys, escape, z with the box zoom, the left
button's box selection rubber band (green from right to left), the right button press with the widget under it,
the 200 ms mouse move pick timer, the multi touch gesture start and end, and the camera distance and clipping after
every move. Its events (`VTKN_EVENT_POINT_PICKED_ON_LEFT_UP` ... `_MOUSE_LEFT`, 22 - 31) end in the vtkControl
handlers the C# events end in; `PickWithNativeMirror` gets the left up pick, so selecting in the native window works
once stream S switches its area on there.

An embedded view takes the input from its window (the style is attached to the Win32 interactor, the widget set
stays the observer above it); an off screen view gets an interactor of its own that a host feeds through
`vtkn_view_handle_input`, widgets first as in the window. `vtkn_view_navigate` gives the touchpad and the host the
navigation calls. Widgets API version 3.

Not ported, dead code of the C# style: `DrawSphereOnLeft`, `AdjustCameraDistance_` and `AdjustCameraDistance__`,
and the unused event id enum.

### How it is checked (`Run_Port_Checks.sh --filter widgets_w2`, about 50 s)

- `Record_W2.py`: geometry and widgets areas on the mirror, parts only on both paths. Thirteen input sequences, each
  from the same framed camera, go to `vtkInteractorStyleControl` through its handlers and to the native style
  through `vtkn_view_handle_input`: rotate (short and long), pan, the slow zoom (with and without a horizontal
  move), the wheel at two points, the ctrl wheel, the four arrow keys, a rotation center beside and on the model,
  a zoom and then a rotation, the box zoom (the ActiViz path animates it), and the smooth zoom (the ActiViz timer
  runs it to the end, the native one is finished at once). The box selection is drawn and cancelled by escape.
- `Test_Embedded_W2.py`: the native view in the window, driven by real Win32 messages (`WM_MBUTTONDOWN` ...,
  `WM_KEYDOWN`, `WM_MOUSEWHEEL` with screen coordinates), against the same input to the ActiViz style: a middle
  drag, a middle drag with shift, two arrow keys, and three wheel notches with the smooth zoom stepped by the timer
  of the native window.
- `Compare_W2.py`: the cameras must start alike and end alike to 1e-9 of the model size.

### Result: 20 of 20 checks pass

- The cameras agree to 1e-14 or better after every sequence, the embedded ones included (the smooth zoom: 0 and
  6e-16; the many timer steps of either path end at the same camera).
- 2 reported: a rotation center picked on the model leaves the cameras 5e-4 - 8e-4 apart. ActiViz rebuilds the
  picked point from the depth buffer and the native view intersects the ray (slice C's finding). The second case
  is a slow zoom drag without a horizontal move, whose release counts as a click, since the C# style compares the
  x coordinates with each other (`Math.Abs(_clickPos[0] - currClickPos[0]) + Math.Abs(_clickPos[0] - currClickPos[0])`).
  The port keeps that quirk; **a latent bug of the old code**: it should compare y too.

### Fix cycles

1. (port) The embedded smooth zoom never stepped: VTK 9's Win32 interactor does not report which timer fired
   (`GetTimerEventId` is not set), so the style compared against a stale id. The selection timer is stopped while
   the smooth zoom runs, as in C#, so the style takes the timer that runs as the one that fired.
2. (check) The slow zoom case above failed first and was split into the exact case and the reported one.
3. (port, found by the full suite) Slice B's embedded check turned the camera with a left drag beside the bar; with
   the ported style the left button draws the box selection, as in PrePoMax, and the middle button turns. The check
   now turns with the middle button.

### Rendering defaults found

- None. Key input changed: VTK 9 reports an arrow key with a zero key code and its key symbol, where VTK 5.8 gave the
  virtual key as the code; the style maps the symbols to the codes the C# style compared.

## W3 - Touchpad, trackpad and the Widgets region

Started 19:01 (50 %), ended 19:06 (51 %).

### What was ported

- **Touchpad and trackpad**: `PrecisionTouchpad`, `TrackpadGestureRecognizer` and the wheel fallback read Windows
  raw input and wheel messages and contain no VTK, so they stay in C#. `TrackpadNavigation.Apply` now moves an
  `ITrackpadNavigationTarget`: the ActiViz style implements it, and `VtkNativeNavigationTarget` maps it onto
  `vtkn_view_navigate`. vtkControl's gesture flush and end drive both while the widgets area is on.
- **The Widgets region**: everything in it reaches the native twins through the W1 forwarding (interaction and
  highlight settings, the scale bar position, the arrows with their placement, removal and hiding). The arrow
  placement algorithm (`PositionArrowWidget` and its geometry helpers) is pure 2D math over the widget boxes and
  stays in C#; its moves reach the native arrows. The context menus: the native right button press carries the id
  of the widget under the mouse, which vtkControl maps back to its widget and hands to `style_RightButtonPressEvent`,
  so the menus open as before.

### How it is checked (`Run_Port_Checks.sh --filter widgets_w3`, about 25 s)

`Record_W3.py` sends gestures through vtkControl's own path (the recognizer's update, the flush, the end of the
gesture): a two finger rotation, a pinch, rotation with zoom, a two finger pan with a pan sensitivity, and an
inverted axis; and a right click on an annotation through both styles. 6 of 6 pass: cameras to 3e-14, the same
`Form_WidgetPicked` call with the same rectangle.

### Fix cycles

1. (port) An off screen view had no interactor before its first input, and the touchpad's navigation calls came
   first (an access violation): the off screen view gets its interactor when the style is made.
2. (harness) The runner was edited while it ran, which bash reads as it goes; rerun.

W2 and W3 are one commit: they share the hook lines in `vtkControl.cs`, the partial class and the project file.

## Not ported, and what stays in C#

- The gesture recognition of the touchpad and trackpad (Windows raw input; no VTK).
- The arrow placement of the Widgets region (2D math in C#; its moves reach the native arrows).
- .NET formatting everywhere: the status block lines, the scale bar labels (through a callback), the color bar
  labels.
- The hover cursor of the triad (`coorSysWidget_HoverChanged` sets the ActiViz window's cursor); the native event
  `VTKN_EVENT_WIDGET_HOVER_CHANGED` is raised but not yet wired to a cursor of the native window.
- The rubber band of the box selection is drawn in the widget layer, as the native view has no selection layer yet
  (stream S adds it with S2; one line in `View::GetInteractorStyle` moves it there).
- `vtkControl.SetRotationPoint` reaches the native style through a hook in the Views region; the camera animations
  of the Views region (`_animating`) do not yet tell the native style (`VTKN_NAVIGATE_SET_ANIMATING` exists).

## Review list for the user (round 2 of the plan: the camera feel and the widgets)

Run `VtkPort/Widgets/Review_W2_Camera.py` from the Python panel of PrePoMax started from this worktree's build
(`PrePoMax\bin\x64\Debug`); the first run covers the viewport with the native VTK 9.7 view with the parts, the
widgets and the ActiViz camera, the next run closes it. Click into the view first, so that it has the keyboard.

1. Middle drag: the speed and direction of the turn, the rotation about the center, the orange cross.
2. Shift + middle drag: the pan follows the mouse; ctrl + middle drag: the slow zoom.
3. A middle click on the model, then a turn: the model turns about the clicked point; a click beside it: about the
   center of the model.
4. The wheel: the smooth zoom at the mouse (does it feel like the ActiViz one, frame by frame?); ctrl + wheel.
5. Arrow keys (5 degrees), z + left drag (box zoom), a left drag (the rubber band, green from right to left),
   escape.
6. The triad: the negative axes near it, the highlights on hover, a click on each cone, end, shaft and the origin.
7. The widgets on a result (open one first): the status block, the scale bar while zooming, the min/max annotations
   while turning and dragging, the probe, the color bar of colors by part; hover highlights, drags, double clicks
   and right click menus.
8. A touchpad, if the machine has one: two finger turn and pan, pinch.

## Finished after the integration (2026-11-06, with the leftovers of stream S)

- **The hand cursor of the triad**: the hover handler of the native triad sets the hand cursor of its render window, as
  `coorSysWidget_HoverChanged` sets it on the ActiViz window.
- **Animations of the camera**: `AnimateCamera` tells the native style to take no input while it runs
  (`VTKN_NAVIGATE_SET_ANIMATING`), as it sets `vtkInteractorStyleControl.Animating`.
- **Found while checking**: the `SelectBy` and `QuerySelectionInProgress` setters switched the selection box of the
  ActiViz style only, so the native style kept the setting it had when the areas were switched on, and a drag in the
  native window clicked instead of selecting a box; both setters now send the settings. The box zoom of the toolbar
  (`StartBoxZoom`) did not reach the native style either.
- Checked in `Test_Window_Events.py` (`integration_window`), widgets mode: a drag in the window selects the elements the
  same box selects on the ActiViz path, the box zoom of the toolbar zooms and selects nothing, the cursor over the
  triad is the hand and away from it is not, and a click while the camera animates raises no pick. The cursor and the
  animation checks were run once with their fix switched off and failed.

## Changed after the user's review (2026-11-07)

- **The lines of the widgets were gray**: VTK 9 maps a whole display coordinate onto the border between two pixels, so
  with multisampling a one pixel line covered two rows at half intensity (121 and 121 on the scale bar where ActiViz
  drew 110 and 134). The lines of the scale bar, the scalar bar, the color bar and the leader of an arrow are moved by
  half a pixel (`vtkNativePixelLines.h`) and the border of a text widget runs through the centers of its outermost
  pixels, so it stays in the window when the widget lies at its edge. They are black, one pixel wide.
- The hover highlight is translucent over the border, as in ActiViz; with the border now one dark row, the highlighted
  frame shows it as a darker inner row.
- Checks: slice B allows 16 % differing pixels in a widget (the lines lie in one row where ActiViz spread them over two)
  and leaves the three pixel band of the highlight out of the coverage of the dark shapes; W1 covers the shapes within
  two pixels, since the native scale bar also stands a pixel higher (accepted in the review).

## Changed after the user's third review (2026-11-07)

- **A standard view animated and then snapped back; a hover over the tree flickered the view**: the view changes of
  vtkControl (the standard views, a zoom to fit, their animations) moved only the ActiViz camera, and the ActiViz
  window, created by VTK without `WS_CLIPSIBLINGS`, painted each of its renders over the native view that covers it.
  So the user saw the ActiViz animation, then the native view repainted with its own camera; a highlight rendered
  ActiViz once with the camera it had when the native view was switched on. Now, while the native view is in the
  window: the ActiViz window is clipped by its siblings; every camera change of the native view is copied to the
  ActiViz camera (`NativeWindow_CameraEvent`), so a view change starts where the user is; and `RenderScene` hands the
  ActiViz camera to the native view, which covers every view change and each frame of its animation.
- Checked in `integration_window`, both modes: the control's camera follows a native rotation, a front view of the
  control reaches the native view, a highlight leaves the native camera, and the ActiViz window has `WS_CLIPSIBLINGS`.
- `Test_Embedded_W2.py` drives the ActiViz and the native navigation apart from one start in the window and compares
  where they end, which the sync defeats; and the ActiViz navigation picks its center from the depth of its own
  pixels, which a clipped window no longer owns. `vtkControl.NativeWindowIndependent` switches both off for it.
- `Run_Port_Checks.sh` takes the build folder to copy from `PREPOMAX_BIN`, for a run while PrePoMax runs from the
  build folder and its `lib` cannot be updated.

## Changed after the user's fourth review (2026-11-07)

- **A double click on a part or the background did nothing**: the native style counts the clicks and sends the count
  with the pick, but `PickWithNativeMirror` always reported one click; it takes the count now. The raw selection mode
  without the widgets area still reports single clicks.
- **The minimum and maximum of the scalar bar could not be edited**: nothing subscribed to the double click of the
  native scalar bar (slice B attached its own handler, which hid it). It ends in `scalarBarWidget_DoubleClicked` now,
  and slice B checks the callbacks of vtkControl on both paths.
- **A click on the triad did not animate**: in the window it calls the animated views of vtkControl, whose frames
  reach the native view (the camera sync of the third review); the off screen mirror keeps the direct native view.
- **The hover highlight did not cover a black widget border**: the highlight frame is drawn opaque in the color VTK 5.8
  blended from the translucent blue and the half gray border (93, 114, 174).
- **The highlight flickered on a right click on a selected part**: the right click picked through ActiViz, which
  rendered into its window behind the native view; in the window it picks through the native view.
- Checked in `integration_window` (widgets mode): a double click reaches the Controller as one and two clicks, a right
  click picks the part, a triad click animates both cameras (over a hundred camera steps), and the transparency of the
  parts changes the native picture and back.

## Changed after the user's fifth review (2026-11-07)

- **The hover highlight is translucent again** (RoyalBlue at half opacity, as in ActiViz): the opaque frame of the
  fourth review is taken back. While a highlight covers a whole widget, the widget's black border line is hidden, so
  the frame is the same blue on a widget with a border and without one; an inner highlight (a value of the scalar bar)
  leaves the border.

## Changed after the user's sixth review (2026-11-07)

- **The selection highlight blinked on a right click** (the results view most of all): `RenderScene` handed the camera
  of the control to the native view on every call, since the native view sets its own camera distance and the cameras
  never compared equal, and the native `SetViewParameters` renders at once. A selection calls `RenderScene` while the
  highlight is cleared and not yet drawn again, so that frame was shown. The camera is handed over only when it shows
  another view: another direction, view up or scale, or a focal point moved across the view
  (`ShowSameParallelView`). Found by recording every render and invalidation of the native view during the right clicks
  of the Controller's handler. Checked in `integration_window`: two selections through the Controller's handler render
  the native view no time before the paint (6 renders without the fix).
