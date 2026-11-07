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

## Finishing the stream (G6 - G10), after the integration

Run on `port/vtk-native` after the merge of the three streams, 2026-11-06 from about 15:05 to 16:10, in one user turn
("Finish G next"). Each slice was committed with its suite; the full `Run_Port_Checks.sh` closed it.

| Slice | Commit | Old C# ported | Suite |
|---|---|---|---|
| G6 vector and tensor plots | `62ea153f6` | ~560 (the glyph actors, animated and empty included) | `geometry_g6` 48/48 |
| G7 captions, labels, annotation updates | `a5e250616` | ~240 | `geometry_g7` 36/36 |
| G8 coordinate system symbols | `07e681e15` | ~470 (on W's triad geometry) | `geometry_g8` 12/12 |
| G9 element-wise section view | `8bfbd9b29` | ~580 | `geometry_g9` 36/36 |
| G10 animated section, follower view, directional copies, exploded steps | `2ec854b84`, `9df66ab58`, last commit | ~260 (camera and values reused from C#) | `geometry_g10` 24/24, `geometry_g10_follower`, `geometry_g10_directional` 24/24, `geometry_g10_exploded` 12/12 |

### G6 - vector and tensor plots

`AddVectorFieldActor`, `AddTensorFieldActor`, the animated plots (`AddAnimatedGlyphFieldActor`,
`CreateAnimatedGlyphNodes`) and `AddEmptyScalarFieldActor`. vtkControl already computes the line arrows (five points
each) and the node points; the mirror sends those arrays to `vtkn_view_add_glyph_actor`, which builds the float poly
data with lines and vertices and draws it unlit in the colors of the values (`vtkMaxActor(data, mapper)`). The frames
of an animated plot go through the G5 frame path. Scenes: DISP as a vector plot and PRINCIPAL_MAX as a tensor plot,
static and at a frame of a scale factor animation.

Fix cycles: (render) **Unlit result colors.** VTK 5.8 drew the colors of an unlit actor as they are; VTK 9 adds the
ambient and diffuse parts of its material even without lighting. An unlit actor with color contours now has ambient 0
and diffuse 1. (check, rendering default 6) **Thousands of dots.** VTK 5.8 smoothed the edges of the node points; the
sphere imposters of VTK 9 (default 1) have hard edges, which alone differs on up to 6 % of the pixels of a plot.
Narrower lines and smaller dots made it worse and were reverted; the four plot scenes have a picture limit of 7 %
(`DIFFERING_LIMITS` in `scenes.py`, mean difference 2.2 levels, cameras exact).

### G7 - captions, labels and the in-place annotation updates

`AddCaptionActor`, both `AddLabelActor` overloads, `SetAllCaptionPositions` (the pixel offset of a caption is
recomputed natively before every render), `ApplySelectionFormattingToCaptionActor`, `UpdateOverlayCaptionActorPosition`,
and the point updates of `UpdateMeasurementAnnotation` and `UpdateOverlayPointsActor` (`vtkn_view_set_actor_points`,
which moves a symbol's input points and normals). The removal of a measurement annotation is mirrored too. Scenes:
captions of the overlay and selection layers with an offset and a move, labels of both overloads, the item ids of
the Controller, a measurement drawn and moved.

Fix cycles: (render) **Captions a pixel high.** VTK 9 draws the text of `vtkCaptionActor2D` as a texture, a pixel
higher than VTK 5.8; captions are drawn by a text mapper anchored at their world point plus the padding of the
caption box, as the labels are, which match exactly. (check, rendering default 7) **Text.** FreeType of VTK 9 draws
some glyphs a row shorter (the descenders), so the outline of a scene with much text on the background covers a
little less: the text scenes have a coverage limit of 0.99 (`COVERAGE_LIMITS`; stream W uses 0.98). (check) **The
base layer.** ActiViz never draws a caption added to the base layer, with or without the native view (checked in
plain ActiViz); PrePoMax adds captions to the overlay and selection layers only. The native view draws them; the scene
leaves the base layer out.

### G8 - coordinate system symbols

`vtkMaxCoorSysSymbols` and `vtkMaxCoorSysSymbol` (`vtkNativeCoorSysSymbols.cpp`) on the triad geometry stream W
ported for the widget: the overlay set, the selected set in the highlight color, the material axes in the colors of
their axes; projected before every render, sorted by depth, the labels sized from the shaft. Passed at once, 12/12.

### G9 - element-wise section view

`GetSectionViewActorsElementWise` and its helpers (`GetPlaneSideOfBounds`, the outer faces, `LabelSectionFaces`,
`ExtractSectionFaces`, `GetSectionOutline`, `GetSurfaceOfKeptElements`, `GetModelEdgesOfSkin`) and
`AddSectionViewSkinActorsToSurfaceActors`. The volume grid carries the node and element ids for it. Scenes: the FE
model, a MISES result with lightened colors, and a plane that leaves one part whole. Fix cycle: (check) the exposed
faces come from the volume grid, which is single precision on the ActiViz side: the three scenes have the camera
tolerance 1e-7 of the result scenes.

### G10 - the animated section, the follower view, directional copies and the exploded view animation

- **Section view of an animated part.** Nothing to port: `vtkControl.SetAnimationFrame` removes the section view and
  creates it again, and the mirror then sends the volume grid at the frame. Checked plain and element-wise. Seen from
  the right the plane is nearly edge on, and VTK 9 draws a one pixel sliver of the section face that VTK 5.8 left out:
  the plain scene has a coverage limit of 0.998.
- **Follower view.** The camera moves of `UpdateFollowerViewPint` and `UpdateFollowerViewPlane` are computed once and
  applied to the native camera too (its points by the rotation and translation, its view up by the rotation, then
  fitted as `AdjustCameraDistanceAndClipping`). `Test_Follower_View.py` steps an animation with the view following one
  node and three nodes: the camera moved 3 % and 8 % of the model size, and the cameras agree within 1e-15.
- **Directional values of transformed copies.** A native copy is named after the actor it starts from, the
  transformation and its place in the row (`<name>:t<k>-<i>`), and vtkControl names its copies the same way for the
  mirror, which sends each copy of a directional result the values `ApplyResultFieldTransformation` computed. Checked
  with U1 under a symmetry (the copy's colors reverse) and S11 under a symmetry and a circular pattern.
- **Exploded view animation.** Every step of `PreviewExplodedView` sends the positions the parts reached. Only the end
  is compared in pictures; a failing step would fail the scene as a mirror error.
- **Repainting the native view in the window.** `RenderScene` invalidated `vtkControl` only, and Windows does not
  repaint a child window with it, so a change that no native event followed (an animation step) stayed undrawn in the
  window. `RenderScene` now invalidates the native host too.
- `TryGetStableInitialAnimationExtremeNodes` stays in C#: it chooses the anchors of the min/max annotations, which are
  widgets (stream W routes them).

### Rendering defaults found (continued)

6. Colors of an unlit actor: VTK 9 adds the ambient and diffuse parts without lighting; unlit result actors have ambient
   0 and diffuse 1.
7. Text of a caption: drawn by a text mapper rather than the texture of `vtkCaptionActor2D`, a pixel lower. FreeType
   still draws some descenders a row shorter than VTK 5.8.
8. An unlit (smoothed) point cloud and a section face seen edge on are drawn with harder edges by VTK 9; the check
   limits of the scenes say so where it matters.

### Review list (additions)

10. Results of the hinge as a vector plot (DISP) and a tensor plot (PRINCIPAL_MAX), also animated: the dots are crisper
    than in ActiViz.
11. Show reference point names and node and element ids; a distance measurement while the model animates.
12. Coordinate systems of the model, selected in the tree, and material axes.
13. The element-wise section view of the FE model and of a result, then animate it.
14. Animate with the follower view on a node and on three nodes; a symmetry of U1; the exploded view animation.

## Changed after the user's review (2026-11-06)

- **Edges through faces at a large zoom**: the line offset of the mappers was (-1, -8), which pulled every edge toward
  the camera; at a 1000 times zoom on a corner of the base, a hidden edge leaving the corner showed through the face
  for about 60 px. VTK 5.8 had no line offset; it is (0, 0) now and 2 - 3 px of the edge are left at the corner. The
  picture suites pass with the same mean differences, so the edges do not sink into the faces without it.
- **Model edges 1 px wide**: vtkControl asks for 0.8 px model edges and 0.5 px element edges. VTK 5.8 drew no line
  thinner than a pixel and smoothed it, VTK 9 draws the true width with multisampling, so the native model edges looked
  gray, thin and jagged. On the user's choice the model edges (and the hatch of a section view) are 1 px wide; the
  element edges stay 0.5 px.

## Changed after the user's second review (2026-11-07)

- **Element edges 1 px wide** as well, on the user's choice: VTK 5.8 drew the 0.5 px edges one pixel wide. The picture
  suites give a scene that shows the mesh `ELEMENT_EDGES_DIFFERING_LIMIT` (6 %, `scenes.py`), since along every mesh
  line the native picture is darker than ActiViz's smoothed half pixel line (up to 4.1 % measured).
- **Edges through faces, the rest of it**: a short tick of a hidden edge was still drawn at a corner of a face seen at a
  steep angle. The polygon offset of the view was (2, 1); a polygon is pushed back by its depth slope times the factor,
  which is largest exactly there. VTK 5.8 used (1, 1), which ActiViz never changed, and so does the native view now; a
  surface that is not a part keeps its place between the parts and the edges with a relative (-0.5, -1). Measured on
  384 low angle views of every corner of the hinge at a 1000 times zoom: 15 views with native only edge pixels at
  (2, 1) (the tick of the user's picture), 4 at (1, 1), and those are 1 - 2 px ends of edges at corners of the lever.
- **Surfaces of the loads, boundary conditions and constraints missing** in the native window: the Controller keeps
  the surfaces it draws in its selection buffer and adds them again by `AddActor`, and vtkControl rebuilds a buffered
  actor natively from the data it recorded when the actor was built. That data was recorded only while the native view
  was on, so the surfaces drawn before it was switched on never reached it. `MirrorActor` now records the data of every
  actor that is not a part (parts are built again on every redraw). Clearing the selection buffer on the switch was
  tried first and left: ActiViz then rebuilds the element-wise section of those surfaces differently.
- **Check**: the scene `hinge_surface_loads` (`geometry_g2`) draws six pressures before the native view is switched on
  and reports a mirror error for each load surface the native view does not hold; it fails without the fix. The
  pictures alone cannot tell, as the parts only mode hides on the ActiViz side what the native view lacks.
- **Element-wise section and surfaces on the parts**: with the surfaces now mirrored, the two element-wise scenes framed
  differently. ActiViz crops such a surface to the remaining elements but keeps all its points, so its zoom to fit
  still frames the whole surface; the native crop keeps the points it draws. The native view is given the ActiViz
  camera in these scenes (`SAME_CAMERA` in `scenes.py`, also in the S4 pick plan), and the pictures then agree.

## Changed after the user's third review (2026-11-07)

- **The undeformed wireframe of the results was jagged**: the Controller draws it with alpha 254, so VTK 9 drew it in
  the translucent pass without multisampling, as it did the edges. A wire actor is forced opaque now; it keeps its
  alpha. Scene `hinge_result_undeformed_wireframe` (`geometry_g3`).
- **Colors annotated by material, section and the like were darker than the legend** (204 against 242 on a top face,
  188 against 215 on a front face): VTK 5.8 put the colors of a table into the ambient material color only, so
  vtkMaxActor bakes the ambient component into them, while VTK 9 puts a scalar color into both the ambient and the
  diffuse color (`ambientIntensity * vertexColor`, `diffuseIntensity * vertexColor` in vtkOpenGLPolyDataMapper.cxx)
  and multiplied the diffuse light by the ambient a second time. The native table holds the colors themselves with the
  actor's ambient and diffuse; a cell is lit as a part of its color, and the faces match ActiViz exactly for the
  materials, the sections and the mesh types (measured on the hinge; the colors by part never used the table). Scene
  `hinge_annotate_materials` (`geometry_g2`).

## Changed after the user's fourth review (2026-11-07)

- **The transparency of a part did nothing in the native view**: `UpdateActor`, which the Controller calls for a new
  color or transparency of a part, had no native hook. `vtkn_view_set_actor_color` (geometry API 7) sets the color and
  opacity of an actor and of its transformed copies and rebuilds an active section view, which took the color when it
  was made. Not done: a renamed part keeps its old name in the native view.

## Changed after the user's fifth review (2026-11-07)

- **No order independent translucency**: VTK 9 drew the translucent geometry into a buffer of its own without
  multisampling, where lines came out jagged and where nothing lost a tie of depth to what was already drawn. VTK 5.8
  blended it into the multisampled frame as it came, and so do the renderers of the native view now (`UseOITOff`,
  `UseDepthPeelingOff`). The undeformed wireframe (drawn before the deformed parts, alpha 254) is no longer forced
  opaque: it is smooth and, as in ActiViz, it loses to the model edges of a part that did not move and stays hidden
  behind the deformed faces (it showed gray there, (43, 43, 43) on the edge of the base, ActiViz (15, 15, 80), now
  (17, 17, 86)).
- **White lines through translucent parts**: their edges were forced opaque and drawn before the faces, whose depth
  then lost to them along every line. Only the edges of an opaque part are forced opaque now.
- **A flash on a click with translucent parts, and of the highlight on a right click**: the native pick rendered the
  window with every part opaque and only the pickable parts shown, and that frame was shown. The pick renders without
  swapping the buffers, as vtkControl rendered only its renderer.
