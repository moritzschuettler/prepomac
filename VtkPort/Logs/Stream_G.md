# VTK native port - stream G (geometry and data)

The log of stream G of `Vtk_Native_Port_Plan.md`, run in the worktree `port-geometry-aec7b3` (branch
`claude/port-geometry-aec7b3`, cut from `port/vtk-native` at step 0). The integration merges it into `Port_Log.md`.

Usage is read from the session usage card (`get_usage`); the 5 hour and weekly percentages are account wide and the
streams W and S ran at the same time, so the differences overstate this stream.

## Summary table

| Slice | Start | End | Usage (5 h / weekly) | Fix cycles (render + check/harness) | Old C# ported | Written |
|---|---|---|---|---|---|---|
| Setup | 18:12 | 18:20 | 29 % / 13 % at start | 1 harness (missing NuGet packages and Tests in a new worktree) | - | - |
| G1 | 18:20 | 19:11 | -> 52 % / 17 % | 2 render + 4 check | ~1,100 | C++ ~800, C API +108, C# ~290, harness 56 |
| G2 | 19:11 | 19:14 (sources written 18:38 - 18:50 while G1's checks ran) | window reset at 19:20 | 0 | ~1,050 | C++ 470, C API +45, C# 130 |
| G3 | 19:14 | 19:38 | 6 % of the new window at 20:00 / 18 % | 2 render (1 found) + 1 check | ~600 | C++ ~600, C API +110, C# ~200 |
| G4 | 19:38 | 19:47 | | 0 render + 2 check (+1 G2 regression) | ~1,050 | C++ ~800, C API +60, C# ~250 |
| G5 | 19:47 | 19:59 | 6 % / 18 % at 20:00 | 1 render | ~350 | C++ ~330, C API +30, C# ~110, harness 70 |
| **Stream** | **18:12** | **19:59 (1 h 47 min)** | **5 h: 29 % -> 52 % in the first window, 6 % in the next; weekly 13 % -> 18 %** | **6 render + 8 check** | **~4,150 of ~11,800** | **C++ 3,273, C# 933 (+66 in vtkControl.cs), harness 211** |

The plan's rule for G1 (more than 50 % over its share of 4 - 5 h means splitting the rest): G1 took 59 minutes, about
its share (80 minutes for 3,500 of 11,800 lines), so the remaining slices were not split. In lines G1 ported less than
the plan's 3,500: much of what the plan put into G1 belongs to code G4 and G5 own (section view, copies, animation),
and slice A of the pilot had already ported the part path.

## Final checks

`Run_Port_Checks.sh` (all suites, 4 min 38 s, at ac91d5a3b): groundwork passes; geometry_scenes 36 of 36; slice A 36 of
36; slice B 20 of 20; slice C 1,960 of 1,960 picks; geometry_g1 60 of 60; geometry_g2 60 of 60; geometry_g3 24 of 24;
geometry_g4 72 of 72; geometry_g5 12 of 12; geometry_g5_png 4 of 4. ALL PASSED.

## Setup

A new worktree lacks the ignored folders a build and the checks need: `packages` (NuGet, MSB4019 otherwise) and
`Tests/Joints` (the hinge model of the pilot runners). Both were copied from the main checkout; nothing ignored is
committed. The runner keeps its output and build copy under `%TEMP%`, which three streams share, so this stream ran
it with `TEMP` pointing at a folder of its own. The baseline `Run_Port_Checks.sh` passed in 3 minutes.

## G1 - actors of every layer, nodes, highlight, hide/show, clear

### What was ported

| Old C# (ActiViz 5.8) | Native (VTK 9.7) |
|---|---|
| `vtkMaxActor`: the default constructor's style, `vtkMaxActor(data, mapper)`, `CreateNodalActor`, `UpdateColor` with the color contours flag and backface properties, the model edges kept for highlight | `PartActor` (`vtkNativePartActor.cpp`), three constructors: mesh, poly data, pipeline output |
| `AddPoints`, `AddCells`, `AddActor`, `AddActorGeometry`, `AddActorEdges` for the base, overlay and selection layers | `View::AddActor` (`vtkNativeViewGeometry.cpp`) |
| `ApplySelectionFormattingToActor`, `ApplyEdgesVisibilityAndBackfaceCulling` and its three per-actor methods for every layer | `PartActor::ApplySelectionFormatting`, `PartActor::ApplyEdgesVisibility(layer)` |
| Highlight geometry region: `HighlightActor`, `GetHighlightActorFromActorName`, `GetCopyOfModelEdgesActor` (silhouette when a part has no model edges) | `View::HighlightActor` |
| Hide/Show geometry region: `HideActors`, `ShowActors`, `HideShowActor`, `UpdateResultRepresentationVisibility` | `View::HideShowActors`, `View::UpdateResultRepresentationVisibility` |
| Clear region: `Clear`, `ClearButKeepParts`, `ClearSelection`, `ClearOverlay`, `RemoveOverlayActors` | `View::Clear`, `View::RemoveOverlayActors` |
| Settings: `SetBackground`, `SetHighlightColor`, the edges mode setter | hooks to the existing view calls, `View::SetHighlightColors` |
| `vtkInteractorStyleControl.AdjustCameraDistance` / `ResetClippingRange` over the selection layer too | `vtkNativeViewCamera.cpp` (two hunks) |

Not in G1, though it sits in the same code: the section view parts of these methods (G4), the transformed copies
(G4), animation names (G5), the color table of `UpdateColor` (G3), the measurement annotation updates of points in
place (`UpdateMeasurementAnnotation`, `UpdateOverlayPointsActor`), captions and labels (G2).

### How it is checked (`Run_Port_Checks.sh --filter geometry_g1`, 40 s)

Scenes (in `scenes.py`): `shell_femodel` (Joint_Plates_Hinge, shells with backface colors and BC nodes),
`springs_femodel`, `hinge_highlight` (element and node highlights through the Controller, the selection layer and
nodal actors), `hinge_hidden` (a part hidden after the mirror is on), `solid_geometry_highlight` (a geometry part
highlighted by its model edges). With the geometry area on, the parts only mode now keeps every mirrored actor of
every layer drawn on the ActiViz side (`SetNativeMirrorAllGeometry`). A mirror call that throws is recorded and fails
the scene.

Result: 60 of 60 pictures pass, cameras identical; the full runner passes (groundwork, geometry scenes, slices A, B,
C, geometry_g1).

### Fix cycles

1. (render) **Round points.** VTK 9 has no point smoothing and draws a vertex as a square; ActiViz drew smoothed
   round dots. Nodal actors are sphere imposters (`RenderPointsAsSpheres`) without lighting: flat round dots.
2. (render) **Coincident faces.** Constraint faces lie exactly on part faces. VTK 5.8 drew the later of two equal
   depths; VTK 9 mixed them into a speckle. A surface actor that is not a part gets a relative polygon offset
   (-1, -1) on top of the view's (2, 1): in front of the parts, behind the edge lines.
3. (check) The geometry highlight passed while the ActiViz side showed no highlight at all: its highlight actor is
   built outside `AddCells` and the parts only filter hid it. The check allows 2 % differing pixels, which a thin
   highlight stays under. Every new actor kind is therefore also reviewed in the side by side pictures.
4. (check) The beam scene drew nothing: the .inp reader of PrePoMax imports solids and shells only (beams come from
   meshing wires). Dropped; beam parts are line cells, which the highlights and model edges exercise.
5. (check) Slice C failed 6 of 1,960 picks after G1. Its picks highlight the picked items in the selection layer,
   which the mirror now sends to the native view, where they moved the camera distance differently from ActiViz.
   Two causes, both fixed: the native camera ignored the selection layer (now ported as in
   `vtkInteractorStyleControl`), and in the pilot's parts only mode the native view must hold the base layer only,
   so the mirror skips the other layers there.
6. (check) The parts only filter of the pilot suites must stay parts only: non-part base actors added to the
   mirrored names made their props visible on the ActiViz side.

### Rendering defaults found

- Points: no point smoothing in VTK 9; round dots need sphere imposters.
- Coincident polygons of two actors: VTK 9 does not resolve equal depths in drawing order; a relative offset per
  mapper is the depth ladder.
- The selection layer takes part in the camera distance and clipping range.

## G2 - symbols

Ported: `AddSphereActor`, `AddOrientedDisplacementConstraintActor`, `AddOrientedConeActors`,
`AddOrientedRotationalConstraintActor`, `AddOrientedJointActor` with its five joints and the part builders
(`AddCylinderAlongX`, `AddSocketAlongX`, `AddCube`, `AddSquareSleeveAlongX`, `AddWeldFillet`),
`AddOrientedArrowsActor`, `AddOrientedDoubleArrowsActor`, `AddOrientedSpringActor` with `CreateSpringSource` and
`SmoothLinePoints`, `AddOrientedThermoActor`, `AddOrientedFluxActor`, `AddSymbolEdges`, `ApplySymbolFormattingToActor`,
the measurement arrows (`vtkNativeSymbols.cpp`, `View::AddSymbolActor`). The native view builds each symbol source,
glyphs it with `vtkDistanceToCamera` on its base renderer and adds the silhouette actor when the control draws symbol
edges.

Checked by `geometry_g2` (hinge, weld, slider, ball and plate joints with their BCs and loads): 60 of 60 pictures, and
the symbols compared in the side by side pictures. No new rendering default: glyphs, flat joint shading and the
silhouettes match VTK 5.8.

Not ported from G2: the coordinate system symbols (`vtkMaxCoorSysSymbols`, `vtkMaxCoorSysSymbol`,
`vtkMaxCoorSysGeometry`, about 820 lines: a 2D projection with text labels, whose geometry class the triad widget of
stream W shares, so the two should be ported once), captions (`AddCaptionActor`, `SetAllCaptionPositions`) and labels
(`AddLabelActor`, both overloads), which are text and need the text defaults of slice B, and the in-place updates of
measurement annotations (`UpdateMeasurementAnnotation`, `UpdateOverlayPointsActor`,
`UpdateOverlayCaptionActorPosition`).

## G3 - results

Ported: `UpdateScalarFormatting` without its widgets (`View::UpdateScalarFormatting`), `GetColorTransferFunction` and
`PrepareActorLookupTable` (`vtkNativeColorSpectrum.cpp`), the result values and extreme nodes of an actor (from its data
and `UpdateActorScalarField`), `UpdateActorsColorContoursVisibility`, the cell values and the color table of
`vtkMaxActor.UpdateColor`, `AddScalarFieldOnCells`.

Checked by `geometry_g3` (DISP and MISES on the solved hinge): 24 of 24 pictures, mean difference 0.73 levels.

Fix cycles:
1. (render) **Result surfaces 12 % darker.** Measured on pixels: plain parts have the same shading on both paths,
   result surfaces were darker natively by a constant factor on a face. VTK 5.8 lit the colors of scalars with
   OpenGL's default global ambient light (0.2) on top of the actor's ambient; VTK 9 has no global ambient light. The
   ambient of an actor with color contours is raised by that factor (0.6 -> 0.72). Two wrong leads first: the scalar
   material mode (VTK 9 already replaces only the diffuse color when ambient equals diffuse) and the lookup table size
   (the user's ActiViz 9.7 notes: `SetNumberOfColors` does not resize a built table in VTK 9; the native table uses
   `SetNumberOfTableValues`, which was right but not the cause).
2. (check) Cameras differed by 2.5e-8 against the 1e-9 tolerance: result coordinates are floats, the framing centers
   round one float step apart. Scenes with single precision data now carry a camera tolerance of 1e-7
   (`CAMERA_TOLERANCES` in `scenes.py`), the default stays 1e-9.

Not ported from G3: the vector and tensor field glyph actors (`AddVectorFieldActor`, `AddTensorFieldActor`,
`AddAnimatedVectorFieldActor`, `AddAnimatedTensorFieldActor`, `CreateAnimatedGlyphNodes`, the line arrows, about 450
lines), `TryGetStableInitialAnimationExtremeNodes`, the directional result transformation of transformed copies
(`ApplyResultFieldTransformation`). The min/max annotations are widgets (stream W); their anchors come from the
extreme nodes, which the native view has.

## G4 - section view, transformations, exploded view

Ported: `CreateSectionView`, `UpdateSectionView`, `RemoveSectionView`, `ClearSectionViewActors`,
`AddSectionViewActors`, `GetSectionViewActorFromSolid`, `GetSectionViewActorFromClosedSurface`, `IsClosedSurface`,
`GetSectionCap`, `SectionViewCap` and `SectionViewHatch` (`vtkNativeSectionView.cpp`), the clipping planes of
`vtkMaxActor`, `AddSymmetry`, `AddLinearPattern`, `AddCircularPattern`, `ApplyTransforms`, `TransformAllMaxActors`,
`TransformActor`, `RemoveTransformedActors`, `Hide/ShowTransformedActors`, `PreviewExplodedView` (its end state),
`RemovePreviewedExplodedView` (`vtkNativeTransformations.cpp`). vtkControl hands the native view the volume grid of
each part, the result values on it, the section color (Lighten stays a .NET extension) and the hatch index.

Checked by `geometry_g4`: section of the hinge with hatches, section of the MISES result with lightened colors, the
capped closed surface of a geometry part, an exploded hinge, a symmetric result, a circular pattern: 72 of 72.

Fix cycles: (check) camera tolerance of the single precision scenes as in G3; (regression of G2, found by the full
run) slices A and C failed: the symbols reached the native overlay during the redraw before the pilot suites switch
parts only on, so the native camera framed them while ActiViz hid them. Parts only without the all-geometry opt-in now
clears the native overlay and selection layers.

Not ported from G4: the element-wise section (`GetSectionViewActorsElementWise` and its helpers, about 500 lines; it
falls back to the plain cut), the section of an animated part at the frame (the locator nodes), the follower view
(about 190 lines of camera code), the exploded view animation steps (only the end positions are mirrored).

## G5 - animation and pictures

Ported: `vtkMaxActor.InitializeAnimation` and `SetAnimationFrame` (the frames are copied before vtkMaxActor releases
them), `vtkControl.SetAnimationFrame` (the actor part; the section view and transformation hooks replay around it),
`RenderToPNG` with magnification and transparency (`vtkNativeViewPicture.cpp`). `SaveAnimationAsAVI` and
`SaveAnimationAsImages` are loops over these and stay to the integration.

Checked by `geometry_g5` (frame 3 of 8 of a scale factor animation of the DISP result, 12 of 12) and
`geometry_g5_png` (RenderToPNG and RenderToPng of the hinge, opaque and transparent, at 1x and 2x, alpha compared:
4 of 4).

Fix cycle: (render) **Stale bounds of animated points.** The native framing stayed at the first frame shown. VTK 9's
`vtkPolyData` computes its bounds again only when its points are newer than the last computation; a frame's points
were made earlier, so swapping them in left the bounds. They are marked modified when shown. Found with a new
`vtkn_view_get_bounds`, which the checks can use.

## Rendering defaults found by the stream

1. Points: VTK 9 has no point smoothing; nodes are sphere imposters without lighting (flat round dots).
2. Coincident faces of two actors are not resolved by drawing order in VTK 9; non-part surfaces get a relative
   polygon offset (-1, -1) in front of the parts.
3. The selection layer takes part in the camera distance and clipping range (as `vtkInteractorStyleControl`).
4. Colors of scalars: VTK 5.8 added OpenGL's global ambient light (0.2); color contour actors get 1.2 times their
   ambient.
5. Bounds of a data set follow the modification time of its points, not of the data set: points swapped in are
   marked modified.

Not a default but a harness lesson: the picture check allows 2 % differing pixels, which a missing highlight or a
thin symbol stays under (G1 passed once with no highlight on the ActiViz side). Every new actor kind was also checked
in the side by side pictures.

## Harness additions (by adding lines)

- `scenes.py`: the G scenes, `AFTER_ENABLE` steps that run after the areas are switched on (switching on redraws, so
  highlights, hidden parts, section views, transformations and animations are set after it), `CAMERA_TOLERANCES`.
- `Record_Scene.py`: runs the after-enable step, asks `SetNativeMirrorAllGeometry` when the geometry area is on (parts
  only then keeps every mirrored actor of every layer drawn), records the mirror errors and the camera tolerance.
- `checks.py`: a mirror error fails its scene, the camera tolerance of a scene, the alpha of transparent pictures.
- `Run_Port_Checks.sh`: suites `geometry_g1` to `geometry_g5` and `geometry_g5_png` (`Record_Png.py`,
  `Run_Png.sh`).
- A new worktree needs `packages` and `Tests/Joints` copied from the main checkout, and a run of its own needs its own
  `TEMP` (the runner keeps its build copy and output there).

## Review list for the user

Run `VtkPort/Harness/Toggle_Native_Window.py` from the Python panel of a PrePoMax built from this branch, with
`AREAS = ["geometry"]` (its `HARNESS` path points at the main checkout's harness, which is the same code). The native
view covers the viewport with the ported geometry; run the script again after each change of the scene, since the
native view receives the scene when it is switched on, then follows the changes.

1. FE model of `Tests/Joints/Joint_Hinge.pmx`: parts, element edges, BC cones and cylinders, the hinge joint, load
   arrows, constraint faces in the bores (coincident faces, default 2).
2. Highlights: select parts, node and element sets in the tree; nodes must be round dots (default 1); geometry parts
   are highlighted by their model edges.
3. Hide and show parts; switch the edges mode (element edges, model edges, wireframe, none).
4. Shell model `Joint_Plates_Hinge.pmx`: back face colors.
5. Results of the hinge: DISP and MISES, color spectrum types, discrete and smooth, a manual range with min and max
   colors; compare the shade of the result surfaces with ActiViz (default 4).
6. Section view: a plane through both parts with hatches, then lightened colors, then a result with a section; look
   at the hatches face on (the checks saw them nearly edge on).
7. Transformations: a symmetry and a circular pattern of a result.
8. Exploded view, then an animation of a result (frames, the framing following the frames).
9. Save a picture with a transparent background and magnification 2 (RenderToPng is reached through the checks; the
   menu still uses the ActiViz path until the integration).

Known differences to look at: the specular glints of flat faces in element edges mode (two triangles of a bore show
a glint in ActiViz and not natively); the element-wise section shows the plain cut.
