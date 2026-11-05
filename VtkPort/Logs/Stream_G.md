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
