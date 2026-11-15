# Removing ActiViz - stream RW: the widgets

The log of stream RW of the ActiViz removal (`Port_Log.md`, "Removing ActiViz"), run in its own worktree from
`port/vtk-native` at `3e8c0b9ef`, at the same time as streams RG (geometry) and RS (selection, interaction style,
ActiViz window).

## What was removed

- `vtkMax/Widget`: every ActiViz actor, mapper, text mapper, coordinate and render window observer of the border,
  text, scale bar, color bar, status block, text with arrow and triad widgets, with their own layout, text measuring,
  hit tests, mouse handling, hover highlights, scale bar and triad geometry, the leader line and the arrow head.
  The native widgets (`vtkNative/src/Widgets`) already did all of it for the view the user sees.
- `03_vtkMaxScalarBarWidget.cs` (870 lines): the native scalar bar (`VtkNativeScalarBar`) answers for itself.
- `vtkMaxWidgetTools.cs`: `GetNativeTextStyle` is gone (the text style is held as a `VtkNativeTextStyle`);
  `GetTextSize`, which only the ActiViz coordinate system symbols of stream RG still use, moved to
  `vtkMaxCoorSysSymbol`.
- `00_vtkMaxWidgetHighlight.cs`: the ActiViz highlight actors; the class keeps the colors, opacities and the line
  width that PrePoMax (`Controller`, `PreSettings`) and the ActiViz style read.
- `00_vtkMaxCoorSysGeometry.cs`: the ActiViz drawing (`AppendOrigin/Shaft/End`, `InsertCone/Disk/Color`,
  `GetCameraBasis(vtkCamera)`) moved to `01_vtkMaxCoorSysSymbol.cs`, its only user; the geometry class is plain C#.
- `vtkMaxColorSpectrum.cs`: an unused `using Kitware.VTK`.
- In `vtkControl.cs`: the ActiViz widget set up in `InitializeControl`/`InitializeScalarBar`, the ActiViz scalar bar
  (`_scalarBarWidget`) and every branch that read it, `coorSysWidget_HoverChanged` (the native view sets the hand
  cursor), `style_KeyPressEvt` (an ActiViz scalar bar refresh), the widget detach and re-attach of `SetSmoothing`,
  the ActiViz background of `SetBackground`, the style calls of the widget highlight setters, the ActiViz prop hiding
  of the test hooks `SetNativeMirrorWidgetsOnly` and `SetNativeMirrorScalarBarOnly`.

About 4,000 lines of `vtkMax/Widget` went (4,400 -> about 560).

## What was rewritten and how

- The vtkMax widgets are state holders that forward to their native twin (`VtkNativeWidget`). Each keeps what the
  control set (visibility, frame and its colors, anchor and position, text style, padding, text, number format, the
  status lines, the color bar colors and labels, the scale bar width, unit and label format, the arrow anchor) and
  hands it to the twin when the widgets area of the native view attaches it (`SetNativeMirror`,
  `CopyStateToNativeMirror`), then forwards every call, in the same order as before. The twin is the master of what
  a user changes in the view (a drag) and of what it measures: `GetWidth`, `GetHeight`, `GetRectangle`,
  `GetBoundingBox` and the arrow's `GetLeaderLine` and `GetAnchorPointBox` read it. Without a twin a widget has no
  size yet.
- The public surface of the widgets that `vtkControl` and the selection code use stays (`SetPosition`,
  `SetTopLeftPosition`, `SetHorizontallyRelativePosition`, `VisibilityOn/Off`, `GetVisibility`, `SetText`,
  `GetText`, `GetNumberFormat`, `SetAnchorPoint`, `OffsetPosition`, `ResetPositionFromAnchor`, ...); the input
  handlers that only the ActiViz style called are gone.
- The scalar bar is state in `vtkControl` (visibility, label format, frame, the last title and table range), from
  which `CreateNativeMirrorScalarBar` makes the native bar; `ScalarBarAddMin/MaxColor`,
  `TryGetScalarBarValueEditData` and `GetUnitAbbreviation` are answered without ActiViz.
- Read-backs: `ResetScaleBarPosition` centers the scale bar with the width of its twin; before the twin exists (at
  startup, before `SetNativeView`) the centering waits for the twin. `InitializeResultWidgetPositions` takes the
  width of the native status block. `ResetCoorSysWidgetPosition` keeps the constant side of the triad. The arrow
  arrangement (`PositionArrowWidget` and the rest, unchanged 2D math) takes the boxes and leaders of the twins and
  the size of the native view.
- The right click on a widget: the sender of the scalar bar is the native bar (`VtkNativeScalarBar`), and the
  arrow's rectangle is its twin's. The arrow double click goes through `arrowWidget_DoubleClicked`.

No native API was added or changed.

## Edits in regions of other streams (minimal, to keep the build)

- `vtkInteractorStyleControl.cs` (RS): the style fed the vtkMax widgets with the mouse; the widget type it holds is
  now a nested abstract `StyleWidget` declaring the members it calls (a rename of `vtkMaxBorderWidget` in the file).
  No widget is added any more, so the list stays empty. The file goes with RS.
- `01_vtkMaxCoorSysSymbol.cs`, `vtkMaxCoorSysSymbols.cs` (RG): received the ActiViz drawing of the coordinate system
  symbols from `00_vtkMaxCoorSysGeometry.cs` and `GetTextSize` from `vtkMaxWidgetTools` (two call sites changed).
  They go with RG's removal of the ActiViz coordinate system symbols.
- `vtkControl.cs`, `InitializeControl` (RS's style part): the line `_style.KeyPressEvt += style_KeyPressEvt` removed.
- `vtkControl.cs`, `SetSmoothing` (render window RS's): only the widget detach/re-attach removed.
- `vtkControl.cs`, `SetNativeMirrorPartsOnly`: the scalar bar visibility is read from the kept state.
- `vtkControl.Designer.cs`, `OnHandleDestroyed`: the triad's `RemoveInteractor` and dead comments removed.

## Recorders

`Record_SliceB.py`, `Record_W1.py`, `Record_W2.py`, `Record_W3.py` and `Test_Embedded_W2.py` drive the native
view only. The old side of each document is written as the new one, which the graft replaces with the ActiViz run of
the reference. W1 takes its views from the cameras of the reference. `Test_Embedded_W2.py` compared the embedded
native view with the ActiViz style; it now compares the Win32 messages to the embedded view with the same input
through `vtkn_view_handle_input` from the same start.

## Checks

All runs on the native view in the window against `%TEMP%\VtkPortH\reference_activiz`, with a private `TEMP`.

| Suite | Result |
|---|---|
| widgets_slice_b | 20 / 20 (after the recorder took the manual legend limits of the reference, whose ActiViz lookup table kept its default range 0 - 1) |
| widgets_w1 | 35 / 35 |
| widgets_w2 | 20 / 20, embedded test passed |
| widgets_w3 | 4 / 6: `pinch_zoom` and the annotation right click (rectangle x 277 / width 86, reference 278 / 85) |
| geometry_g7 | 36 / 36 |
| selection_s5_probe | passed |
| integration_window, `Run_Window_Events.sh` | passed |
| all other geometry suites, selection_s1, selection_s3, selection_slice_c | passed |
| groundwork | failed: its picture comes out 1027 x 475 instead of 800 x 600 |
| selection_s2 | four hover pictures, as before any stream |
| selection_s4 | section element wise picks differ from the reference |
| Python API scripts (`Test_Api`, `Test_Selection`, `Test_Workflow`, `Test_Model_Parts`, `Test_Result_Field`, `Test_Model_Loads`, `Test_Model_Constraints`, `Audit_`) | 16 / 16 (`Test_Api_View_Results` fails only with a private `TEMP`, as it expects PrePoMax's temporary folder) |

The failures of widgets_w3, groundwork and selection_s4 are the same on the build of `3e8c0b9ef` (its vtkControl
put into a copy of this build) with the same recorders, so they do not come from this stream. `pinch_zoom` zooms at
the real mouse pointer (`Cursor.Position` in `gestureRecognizer_GestureUpdated`), so its end camera changes from run
to run (1e-2, 2e-2, 4e-4 here); the reference was recorded with the pointer elsewhere. The annotation of the right
click measures one pixel wider here than in the reference on both builds.

## Open points

- `pinch_zoom` of W3 depends on where the mouse pointer is; the recorder should place the pointer, and the reference
  be recorded again with it placed.
- `Review_W2_Camera.py` names the harness folder of an old worktree (`port-widgets-401a67`).
- A widget made before the native view exists has no size; `ResetScaleBarPosition` waits for the twin, the arrows
  and the other read-backs assume the native view is there (it is from `SetNativeView(true)` at startup on).
- When the ActiViz style goes, `StyleWidget` goes with it; `vtkMaxWidgetHighlight` keeps only constants.
