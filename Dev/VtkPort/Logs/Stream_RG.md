# Removing ActiViz - stream RG (geometry)

The log of stream RG of the ActiViz removal ("Removing ActiViz" in `Port_Log.md`), run in the worktree
`agent-ae61434ac3d0d2bfc` (branch `worktree-agent-ae61434ac3d0d2bfc`). The worktree was created from `master`; it was
reset to `port/vtk-native` at `3e8c0b9ef` (the base the brief names, which contains that `master`) before any change.
Streams RW (widgets) and RS (selection, interaction style, ActiViz window) ran at the same time.

## What is free of ActiViz now

- `vtkMaxActor`, `vtkMaxCaptionActor`, `vtkMaxExtreemeNode`, `vtkMaxActorData`, `vtkMaxActorAnimationData`,
  `vtkMaxAnimationArrowAnchor` (new file, split from `vtkMaxActorAnimationData.cs`), `vtkMaxAnimationFrameData`,
  `vtkMaxLookupTable`, `SectionViewData`, `Globals`, `Tools/Extensions.cs`, `Native/Geometry/*`.
- Deleted: `Extensions.cs` (an unused `vtkProperty` extension), `Tools/SectionViewCap.cs`,
  `Tools/SectionViewHatch.cs`, `vtkMax/CoorSys/01_vtkMaxCoorSysSymbol.cs`, `vtkMax/CoorSys/vtkMaxCoorSysSymbols.cs`
  (the native view draws the caps, hatches and coordinate system symbols).
- In `vtkControl.cs` the regions Add geometry, Highlight geometry, Hide/Show geometry, Section view, Exploded view,
  Follower view, Transformations, Scalar fields, Animation and Clear, the code after the regions and the formatting
  helpers before the Views region. What is left there of ActiViz is only in other streams' code: the `_style` calls
  (RW), the min/max arrow widgets, `CreateScalarBarLookupTable`, `SetScalarBarWidgetVisibility` and
  `_statusBlockWidget` (RW), and `GetColorTransferFunction`, which only RW's ActiViz scalar bar
  (`InitializeScalarBar`, `CreateScalarBarLookupTable`) still calls: it goes with that scalar bar.
- `vtkControl.cs` went from 11,879 to about 6,400 lines and `vtkMaxActor.cs` from 2,625 to about 800.

## How the read-backs were rewritten

- `vtkMaxActor` is the state of an actor: identity and style as before, a plain `Pickable` and visibility, the source
  `vtkMaxActorData` (`Data`, which `AddActor(vtkMaxActor)` of the Controller's selection buffer sends again), whether
  it is nodal, its point count, the native name of a transformed copy, an accumulated 4 x 4 transformation instead of
  `_volumeGridTransform`, the exploded offset, the node id to point map, the values drawn and their extremes, the
  animation frames as float arrays (`vtkMaxActorAnimationData`), the model edges a compound sub part draws, and a
  memory estimate from the exchange data (`SelectionCache` evicts by it). `GetPoint` gives a point as drawn: of the
  frame shown, transformed.
- Transformations: `AddSymmetry`, `AddLinearPattern` and `AddCircularPattern` keep 4 x 4 matrices
  (`vtkMaxActor.GetScale/GetTranslation/GetRotation`, the matrices `vtkTransform` built); a copy appends its
  transformation, and its directional result is transformed with `ResultFieldExchangeData.GetTransformedValues`, the
  extreme node coordinates with the matrix (`MirrorCopyResultValues` read both from ActiViz).
- Exploded view: the offset is `vtkMaxActor.ExplodedOffset`; a part sent again gets it again.
- Follower view: the node id to point map and the point as drawn come from `vtkMaxActor`; the camera move is computed
  from the native camera (its view plane normal for the point follower) and the control follows through
  `OnNativeCameraChanged`.
- Section view: the native view cuts; `GeometryHiddenBySectionView` is read back from it
  (`vtkn_view_get_section_view_hidden_actors`), so the selection's rule (a part the element-wise cut replaces by its
  skin counts as drawn) keeps its answer. A highlighted part that is cut also highlights the outline of its cut faces,
  as `HighlightActor` did with `actor.SectionViewActor` (native `HighlightActor`).
- Color tables: `UpdateActorsColorTables` sends the tables and the mean colors to the native view (it made no native
  call, so the native view kept the old face colors) and rebuilds an active section view; `UpdateActorModelEdgesToDraw`
  sends the sub part again with its new model edges when they changed.
- Measurement annotations keep the point counts of their actors and arrows in C#.
- Animation: the frames, values and extremes are C# arrays; `TryGetAnimationNodeData` reads a surface node from the
  frame shown and an interior node from the results through the animation nodes provider; the anchor of an annotation
  is located by the native view in the part's volume grid at the frame shown (or in its surface) with the weights of
  the cell's points (`vtkn_locate_point_in_mesh`), and moves with those points.
- 3MF export: `GetVtkMaxActorData` takes the drawn actors from the native view (`vtkn_view_prepare_export`,
  `vtkn_view_get_export_actor`), as ActiViz gave them: triangles with their values when colored, element and model edge
  segments, a wire as model edges in its own color.
- STL export: `vtkMaxActorData.GetStlTriangles` triangulates the surface cells in C# as `AddCellDataToPoly` did.
- `CropPartWithCylinder` and `CropPartWithCube` are not without callers: the Geometry > Stl Part menu reaches them.
  They are native (`vtkn_view_crop_part_to_stl`), as `SmoothPart` was.
- `RenderToPNG`, `SaveAnimationAsImages`, `SaveAnimationAsAVI`, `GetBoundingBox`, `GetLookupTable` and `SmoothPart`
  keep only their native paths.
- The mirror bookkeeping is gone: `_nativeActors` holds the actors the native view draws, `RunNative` records a failed
  native call for `GetNativeMirrorErrors`, the parts only mode of the checks is a flag (`SetNativeMirrorPartsOnly`)
  and `MirrorPart` (no callers) is deleted.

## Native API (geometry area version 9 -> 10)

- `vtkn_view_get_section_view_hidden_actors`, `vtkn_view_crop_part_to_stl`, `vtkn_view_prepare_export`,
  `vtkn_view_get_export_actor` with `vtkn_export_actor`, `vtkn_locate_point_in_mesh` (new file
  `src/Geometry/vtkNativeViewExport.cpp`), and the outline of the cut faces in `vtkn_view_highlight_actor`.

## Edits in code other streams own

- RS, the Selection region: the ActiViz pick functions that read the ActiViz members of `vtkMaxActor` are stubs
  (`PickByNode` .. `PickByGeometryEdgeAngle`, `PickByArea`, `GetPickPoint`, `GetSectionViewParentActor`,
  `GetSelectableActorName` (both), `ResolveSectionViewPick`, `GetClosestPointOnActorSurface`, both
  `GetGlobalCellIdClosestTo3DPoint`), and `GetClosestCellsOnActors` and `GetPointAndCellIdsInsideFrustum` lost their
  ActiViz fallback after the native branch. All are dead in the window; RS deletes them.
- RS, the mouse handlers before the Selection region: three `GetActorName(pickedActor)` became `null` (ActiViz picks).
- Top of the control: the fields of the coordinate system symbols, the caption placeholder actor and
  `_nativeMirrorHiddenProps` are gone, `_measurementArrowInputs` became `_measurementArrowPointCounts`, `_transforms`
  is a list of matrices; `Globals.Initialize` (the ActiViz mouse selection property) is gone from the constructor and
  `SetMouseHighlightColor`.
- `EdgesVisibility`, `vtkControl_Resize`, `vtkControl_EnabledChanged`, `style_ZoomChangedEvent`, the Views region (two
  calls) and `OnNativeCameraChanged`: the calls of `SetAllCaptionPositions` and `ApplyEdgesVisibilityAndBackfaceCulling`
  (ActiViz captions and edges) are gone.
- `InitializeControl`: the creation of the ActiViz coordinate system symbols is gone; `Timer_Tick` no longer calls the
  deleted `AddDiskAnimation` test.
- Settings: `SetHighlightColor` no longer formats the ActiViz selection actors; `MirrorGeometry` is `RunNative`.
- Native pilot region: `SetNativeMirrorPartsOnly` rewritten for the native view only, `MirrorPart` deleted.
- PrePoMax: none. `vtkMaxActor.GetNumberOfElements` (the Controller's selection buffer) is kept, counted from the data.

## Harness

- `scenes.py`: the surface load check reads `_nativeActors` instead of `_nativeMirroredActors`.
- `Test_Follower_View.py`: the node ids come from `vtkMaxActor.Data`; with a reference run the native camera must
  move as far as the ActiViz camera moved (the reference's `moved`), since the camera of the control and the native
  camera are now the same camera and agree trivially. `Record_Scene.py`, `Record_Png.py`, `Run_Follower_View.sh` and
  `VtkPilot/Phase2/*` drive no ActiViz any more (their old side goes through the native view and is grafted).

## Checks

All against `%TEMP%\VtkPortH\reference_activiz`, with a private `TEMP`, on the final build:

| Suite | Result |
|---|---|
| geometry_scenes | 36 / 36 |
| geometry_slice_a | 36 / 36 |
| geometry_g1 | 72 / 72 |
| geometry_g2 | 108 / 108 |
| geometry_g3 | 36 / 36 |
| geometry_g4 | 72 / 72 |
| geometry_g5 | 12 / 12 |
| geometry_g5_png | 4 / 4 |
| geometry_g6 | 48 / 48 |
| geometry_g7 | 36 / 36 |
| geometry_g8 | 12 / 12 |
| geometry_g9 | 36 / 36 |
| geometry_g10 | 24 / 24 |
| geometry_g10_follower | passed; moved 0.0328 / 0.0847 as the reference |
| geometry_g10_directional | 48 / 48 |
| geometry_g10_exploded | 12 / 12 |

No native errors in any scene. `Run_Window_Events.sh`: TEST PASSED. `Run_Tests.py --filter Test_Api Test_Selection
Test_Workflow Test_Model_Parts Test_Result_Field Test_Model_Loads Test_Model_Constraints Audit_`: 16 of 16 passed.
A smoke script (not committed) exercised what no suite covers: the 3MF export (2 actors, 9,064 triangles, with
values), the STL export of the hinge parts, the bounding box, a highlight of a cut part, an animation with a node
annotation (one anchor, `TryGetAnimationNodeData`), no native errors.

## Open points

- The ActiViz pick functions of the Selection region are stubs here; RS deletes them.
- `GetColorTransferFunction` and the `_lookupTable` field remain for RW's ActiViz scalar bar.
- The 3MF export now ignores nothing it used to take, but the actors come in name order of the native view instead of
  the insertion order of `_actors`; the file has the same objects.
- `RemoveSectionView(releaseVolumeGrids)` no longer releases anything in C# (there are no C# grids); the native view
  keeps the grids it was given.
- Not checked by any suite: the crop menu items (Geometry > Stl Part > Crop With Cylinder/Cube), the anchor of an
  annotation inside a part's volume during an animation, and `UpdateActorModelEdgesToDraw` of compound parts.
