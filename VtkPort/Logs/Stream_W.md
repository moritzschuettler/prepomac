# VTK native port - stream W: widgets and interaction

The log of stream W of `Vtk_Native_Port_Plan.md` (slices W1 - W3), run in its own worktree from `port/vtk-native`
at `0a7748af4`, at the same time as streams G and S. Measured as the pilot was (`Pilot_Log.md`).

## Summary table

| Slice | Start | End | Usage (5 h / weekly, account wide) | Fix cycles | Old C# ported (non-blank) | Written | User time |
|---|---|---|---|---|---|---|---|
| Setup | 18:12 | 18:18 | 29 % / 13 % at the start | 2 (toolset, `Tests/`) | - | - | |
| W1 | 18:18 | 18:50 | 29 % -> 47 % / 13 % -> 16 % | 3 port (+2 harness/check) | ~2,480 | C++ 2,089 new + ~730 in existing files; C# 440 new + 215; checks 455 | |

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
