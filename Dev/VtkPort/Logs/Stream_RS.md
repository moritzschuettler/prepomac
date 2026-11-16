# Removing ActiViz - stream RS (selection, interaction style, ActiViz window)

One of the three streams of the ActiViz removal (`Port_Log.md`, "Removing ActiViz"), run in its own worktree (branch
`worktree-agent-a26b0d0678c9c6fff`, cut from `port/vtk-native` at `3e8c0b9ef`), in parallel with RG (geometry) and RW
(widgets).

## What went

| What | Lines |
|---|---|
| `vtkInteractorStyleControl.cs` (the ActiViz style): deleted | 1,652 |
| `vtkControl.Designer.cs`: the ActiViz render window, its interactor, the mouse and key forwarding, `OnPaint` rendering ActiViz, `OnHandleCreated`/`OnHandleDestroyed`, `SyncRenderWindowSize`, `GetXDisplay` | ~450 |
| The Selection region: `PickByNode` .. `PickByGeometryEdgeAngle`, `PickByArea`, `PickAreaHardware`, `GetPickPoint`, `GetRotationPointPickPoint`, the section view pick helpers, `GetPropPickerPickPosition`, `GetGlobalCellIdClosestTo3DPoint`, the cell helpers, the ActiViz branches of `GetClosestCellsOnActors`, `GetGeometryPickProperties`, `GetSelectionPrecision`, `GetPointAndCellIdsInsideFrustum`, `AddAllGlobal*Ids`, `GetPlaneParameters` | ~1,200 |
| `Style_PointPickedOnMouseMoveEvt`, `style_PointPickedOnLeftUpEvt`, the style event handlers (enter, leave, mouse interaction, zoom changed, key press, the interactor's modified event) | ~260 |
| The ActiViz camera path of the Views region: the ActiViz branch of every view, `ResetCamera`, `ComputeVisiblePropBounds`, `GetViewRotationPivot`, `KeepZoomAndRotationPivot`, `GetClosestAxisViewUp`, `GetClosestIsoDirectionQuadrant`, `FinishViewChange`, `AnimateCamera` and the two unused `AnimateCamera_`/`AnimateCameraQuat`, `AdjustCameraDistanceAndClipping` | ~720 |
| `InitializeControl`: the render window smoothing, the three lights, the style and its wiring, the ActiViz background and camera, the polygon offset; `ApplyEdgesVisibilityAndBackfaceCulling` and its three helpers; `SwitchLights`; the pickers; the mouse selection actors | ~330 |
| The native pilot region: the off screen mirror, the ActiViz fallback (`SetNativeView(false)`), `NativeWindowIndependent`, `KeepActiVizWindowBehindNativeView`, `ShowNativeView`, `NativeInWindow`, `NativeWindow_CameraEvent`, `CopyCameraToActiViz`, `PushCameraToNativeWindow`, `ShowSameParallelView`, `SetStyleAnimating`; `NativeIdResolution`, `NativeExactPicking` and `UseNativeIdResolution` | ~200 |
| `SetSmoothing` rebuilding the ActiViz window | ~50 |
| Native: the off mode of the exact picking (`vtkn_view_set_exact_picking`, `View::ExactPicking`) | ~25 |

## What the control is now

- **One window.** `vtkControl` holds only `_nativeHost`, docked and always shown; the native view is created with
  the host's handle and switched on in `InitializeControl` (`SetNativeAreas(All)`), so a view call made before the
  application shows the viewport (`SetZoomFactor(1000)` at startup) reaches the native camera. `SetNativeView(bool)`
  stays for PrePoMax until the coordinator removes its setting: it only makes sure the native view is on.
- **Input.** The mouse, the keys (focus handed to the OpenGL window in `OnGotFocus`, the arrow keys forwarded by
  `ForwardKeyToNativeWindow`) and the trackpad (`MirrorTrackpadGesture`) reach the native style; its events come back
  through `NativeMirror_InteractionEvent` (pick, hover, right click, rotation center, commit, clear, box zoom) and,
  without the widgets area, `NativeWindow_SelectionEvent`. `SelectionBoxEnabled` of the style is the field
  `_selectionBoxEnabled`, which `MirrorInteractionSettings` sends.
- **Selection.** `Pick`, the right click and the hover go through `PickWithNativeMirror`, `NativeMirrorPickPoint` and
  `HoverWithNativeMirror`; the public helpers the Controller resolves picks with (`GetGeometryPickProperties`,
  `GetPointAndCellIdsInsideFrustum`, `GetClosestCellsOnActors`, `GetSelectionPrecision`) are the native ones. Kept in
  C#: `GetClosestEdgeCell`, `GetVisualizationCellEdges`, `PointToLineSegmentDistance`, `TryAddWholePart` (no VTK call;
  the `vtkCellType` they use is the control's own enum, not Kitware's).
- **Views.** Every view, zoom, box zoom and `Rotate` is `RunNativeViewChange` on the native camera; `RenderScene`
  invalidates the native host. The camera path overlay stays (it draws through `AddPoints`).
- **Exact picking** is the only picking (the user's decision): the point is where the ray of the pixel meets the part,
  the depth of the pick only where the ray misses its cells, and the pick within the selection distance always runs.
  Selection area API version 4 -> 5.

## Left for the other streams (transitional)

The geometry and the widgets still build their ActiViz duplicates on this branch. So that they keep compiling and
running until RG and RW land, `CreateActiVizScene` creates the four ActiViz renderers, a render window and an
interactor: no parent, never rendered, sized with the control (`SyncActiVizSceneSize`, which the widgets lay out in).
The coordinator deletes both methods and the six fields with the last ActiViz use. The render window must not be set
to off screen: VTK 5.8 then crashed the message loop at startup (an access violation in `DispatchMessage`).

## Edits in regions of other streams (smallest edits that keep them compiling)

- RG, `AddActorGeometry`: the ActiViz picker registration (`_cellPicker`, `_propPicker`) and the call of
  `ApplyEdgeVisibilityAndBackfaceCullingToActor` removed; `AddActorEdges`: the edge visibility calls removed.
- RG, section view: `RemoveSectionViewActor` without the pickers; `ApplySectionViewVisibility` (ActiViz edge
  visibility only) and its three calls removed; `IsSectionViewActor` (used by `AddSectionViewSkinActorsToSurfaceActors`)
  kept as a small helper in the Selection region.
- RG, `HideShowActors`, `HideTransformedActors`, `ShowTransformedActors`, `UpdateActor`: the calls of
  `ApplyEdgesVisibilityAndBackfaceCulling` removed; every `_style.AdjustCameraDistanceAndClipping()` (section view,
  follower view, hide/show, animation) removed; `AdjustCameraAfterAdd` and the exploded view animation no longer call
  `AdjustCameraDistanceAndClipping`.
- RG, follower view with one point: the view plane normal is the native one (`GetViewPlaneNormal()`), the ActiViz
  camera no longer follows the native one.
- RG, `SetAllCaptionPositions`: the world size of a pixel is the native one (`GetSelectionPrecision(1)`); it moves
  only the ActiViz captions.
- RG, `Clear`/`ClearButKeepParts`/`ClearSelection`/`ClearCurrentMouseSelection`: the picker clearing, the style's
  rotation center reset and the ActiViz mouse selection actors removed; `ClearCurrentMouseSelection` clears the native
  mouse highlights and the probe.
- RG/RW, Settings: `SetSmoothing` keeps the values and sends them to the native view.
- RW, `vtkControl.NativeWidgets.cs`: `MirrorInteractionSettings` reads `_selectionBoxEnabled`; `MirrorOwnerBoxZoom`
  always lets the control animate the box zoom; the state constant is `VtkNativeView.InteractionStateRotate`;
  `NativeMirrorTriad_TargetClicked` always goes through the views of the control (its off screen branch was dead).
- RW, Widgets region: the `_style` widget highlight calls removed (the native ones stay).
- RW, `vtkMaxBorderWidget`: the three registrations with `vtkInteractorStyleControl` removed.

## Test entries that remain

`NativeHost`, `NativeMirror`, `NativeAreas`, `SetNativeAreas(VtkNativeArea)` (the in window flag is gone),
`EnableNativeMirror(bool)` (all areas), `GetNativeMirrorNames`, `SyncNativeMirror`, `PickWithNativeMirror`,
`HoverWithNativeMirror`, `SetNativeMirrorPartsOnly`/`ScalarBarOnly`/`WidgetsOnly` (RG/RW). Gone: `ShowNativeView`,
`NativeInWindow`, `NativeWindowIndependent`, `NativeIdResolution`, `NativeExactPicking`. `vtkport.enable_native`
still takes `in_window` and ignores it.

## Recorders

- `vtkport.py`: `enable_native` narrows the areas of the one view; it no longer copies an ActiViz camera.
- Slice C, S1, S3, S4 (`Record_SliceC.py`, `Record_Selection_Picks.py`): with a reference the old pick of every grid
  point and box, and the probes around a pick where one side misses, are taken from the reference document, so the
  records line up with the grafted ones; the closest cells comparison of S1 to S4 (ActiViz locators against the
  native ones) is the reference's value, as it needs ActiViz.
- S2 (`Record_Selection_Highlights.py`): the old side is not driven; the points and the border marks are the
  reference's.
- S5 (`Test_Probe_Label.py`): the probe is compared with the old probe state of the reference at its points.
- Groundwork: no ActiViz checks; the embedded view test uses the view of the control and gives it back its scene.
- Window events: the "ActiViz window cannot paint over" check is "the native view is the only window of the control".
- Deleted: `Toggle_Native_View.py`, `Toggle_Native_Window.py`, `Toggle_Native_Selection.py` (they switched between
  the two views).

### Why selection_s2 failed four hover pictures in the first window run

The four pictures (`hinge_femodel_node_point2`, `element_point1`, `edge_angle_point2`, `surface_angle_point1`) are
border picks in the reference: points where a click selects other ids on the ActiViz path and the native one, which
`Compare_Highlights.py` reports instead of failing. The recorder computed the border marks live by clicking through
both paths (`clicked_ids(False)` with `NativeIdResolution = False` and `VTK.Pick`); in the window `Pick` already
picked through the native view (only the ids were resolved by the ActiViz helpers), so the "old" click found the
point the native one found, these points were not marked, and the grafted ActiViz pictures were compared strictly. The recorder now takes the border marks (and the points) from the reference.

## Checks

All on the final build of this branch (vtkNative built with `vtk-9.7.1-build-x64-opt`, its runtime copied into
`PrePoMax/bin/x64/Debug/lib`), against the ActiViz reference (`--reference`), with a private `TEMP`:

| Suite | Result |
|---|---|
| groundwork | passed (the picture after the test scene is reported beside the phase 0 one, not judged) |
| selection_slice_c | 1960 of 1960 picks pass (18 border picks reported) |
| selection_s1 | 1960 of 1960 picks pass (18 border), closest cells 4.4e-07 (the reference's value) |
| selection_s2 | 74 of 74 highlight pictures pass |
| selection_s3 | 1372 of 1372 picks pass (6 border), closest cells 7.5e-07 (the reference's value) |
| selection_s4 | passed: 2936 of 2940 picks, the four differences are the known one (`hinge_symmetry (225, 425)`) |
| selection_s5_probe | passed |
| integration_window (`Run_Window_Events.sh` on a copy of the build) | passed |
| geometry_scenes, geometry_g4, geometry_g10_follower (stream RG's, for the edits made in its regions) | 36/36, 72/72 pictures, passed |

Python API scripts (`Run_Tests.py --filter Test_Api Test_Selection Test_Workflow Test_Model_Parts Test_Result_Field
Test_Model_Loads Test_Model_Constraints Audit_` on a copy of the build): 16 of 16 pass. `Test_Api_View_Results.py`
failed in the run with the private `TEMP` (it expects a relative picture name in the user's PrePoMax temporary
folder) and passes run alone with the normal `TEMP`.

First runs: S4 failed `hinge_section_element_wise` at ten node picks, since the native camera was framed by the native
view where the reference gives it the ActiViz camera (fixed in the recorder, as `Record_Scene.py` does); the
groundwork compared .NET objects with `is`.

## Open points

- The recorders of the other streams that still drive ActiViz members removed here: `Record_W1.py`, `Record_W2.py`,
  `Record_W3.py`, `Test_Embedded_W2.py` (`_style`, `_renderWindowInteractor`, `NativeWindowIndependent`),
  `Review_W2_Camera.py` (`NativeInWindow`), `Record_SliceB.py`, `Test_Embedded_ScalarBar.py`,
  `Toggle_Native_ScalarBar.py` (`_style`, `ShowNativeView`); the phase 0 `Record_Reference.py` is not in a suite.
- `CreateActiVizScene` and its six fields stay until RG and RW have removed every use of the ActiViz scene.
- Without a reference the selection recorders compare the native view with itself: the suites need `--reference`.
