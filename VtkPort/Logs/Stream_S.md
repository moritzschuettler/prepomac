# VTK native port - stream S (selection)

The selection stream of `Vtk_Native_Port_Plan.md`, run in the worktree `port-selection-010fc6` (branch
`claude/port-selection-010fc6`, cut from `port/vtk-native` at `0a7748af4`), in parallel with streams G and W.

## Summary

| Slice | Start | End | Commit | Usage (5 h / weekly, account wide) | Fix cycles (port/render + harness/check) | Old C# ported | Written |
|---|---|---|---|---|---|---|---|
| Setup | 18:12 | 18:20 | - | 29 % / 13 % at start | 1 build (no `packages`), 1 harness (no `Tests/`) | - | - |
| S1 id resolution | 18:20 | 18:33 | `cf3b2e10f` | -> 41 % / 15 % | 1 port (compile) + 2 check | 341 | C++ 367 + C API 79, C# 357, harness 209 |
| S2 highlights, window click | 18:33 | 18:44 | `8c316d5a2` | -> 45 % / 16 % | 1 render + 3 check | 492 | C++ 201, C API 6, C# 357, harness 301 |
| S3 geometry modes, full run, log | 18:44 | 18:52 | this commit | -> 47 % / 16 % | 0 | - (covered by S1 and S2) | harness 6 |
| **Total** | **18:12** | **18:52** | | **18 % of the 5 h window, 3 % of the week (upper bounds)** | **2 + 5** | **~833** | **C++/C API ~670, C# ~710, harness ~520** |

Old C# counted as non-blank lines of the Selection region at `0a7748af4`: the PickBy* hover paths 421, the highlight
branch of `PickByArea` 71, the id resolution helpers (`GetGlobalCellIdClosestTo3DPoint`, `GetCellFaceNodeIds`,
`GetNodeIdOnCellFaceClosestToPoint`, `GetCellFaceNodeCoor`, `GetCellNormal`, `GetClosestCellsOnActors`,
`GetGeometryPickProperties`, `GetSelectionPrecision`, `GetPointAndCellIdsInsideFrustum` and its helpers) 341. The
edge helpers (`GetClosestEdgeCell`, `GetVisualizationCellEdges`, `PointToLineSegmentDistance`, 146 lines) make no VTK
call and stay in C#; both paths use them.

The plan percentages are account wide and streams G and W ran in the same hours, so the differences above are upper
bounds for stream S. Agent time: 40 minutes against the plan's 1 - 1.5 h. The rate (about 20 lines of old C# per
minute) is below the pilot's 45 - 50 because most of the time went into the checks (1,960 + 1,372 picks per path and
74 picture pairs), as in slice C.

## S1 - the id resolution

The helpers the Controller resolves picks with run in the native view when the selection area is on:

| Old C# | Native |
|---|---|
| `GetGlobalCellIdClosestTo3DPoint` (all visible parts), `GetCellFaceNodeIds`, `GetCellFaceNodeCoor` | `View::GetClosestCell`, `vtkn_view_get_closest_cell`: the element id, the point moved onto the cell, the node ids and coordinates of the surface cell; the cell is reported also beyond the distance limit with element -1, as the old helper left `cell` set |
| `GetGeometryPickProperties` | the closest cell natively, then `Controller_GetCellFaceActorData` and `GetClosestEdgeCell` in C# |
| `GetNodeIdOnCellFaceClosestToPoint` | the nearest of the native cell's nodes |
| `GetClosestCellsOnActors`, `GetCellNormal` | `View::GetClosestCellsOnParts` |
| `GetSelectionPrecision` | the native one of slice C, exported |
| `GetPointAndCellIdsInsideFrustum` (bounds test, whole parts, `vtkExtractSelectedFrustum`) | part bounds and `View::GetIdsInsideFrustum`; the volume grids are sent by C# once per view, actor and animation frame (`SendNativeVolumeGrid`) |

VTK 9.7 has no `vtkExtractSelectedFrustum`; its point and cell tests live in `vtkFrustumSelector` behind protected
methods, which a small subclass exposes. `NativeIdResolution` (on by default) lets a check resolve the ActiViz picks
the old way.

Check `selection_s1` (`VtkPort/Selection/Run_S1.sh`): the slice C pick plan on the meshed solid and the hinge, the
ActiViz picks resolved by the ActiViz helpers and the native picks by the native helpers only. 1,960 of 1,960 picks
pass with the same 18 border and tolerance picks as slice C; `GetClosestCellsOnActors` at every hit point agrees to
4e-7 (ActiViz keeps float points).

Fix cycles: (build) `FrustumBoxResultEnum` lives in `CaeMesh`; (harness) the worktree had no `Tests/` (gitignored), the
hinge run failed silently and the slice C compare counted a stale hinge result from the shared `%TEMP%\VtkPortChecks`
of another session, so the checks now clear their output folder and run with a private `TEMP`; (check) the closest
cell distances are compared relative to the view size, not absolutely.

## S2 - the mouse selection highlights, selecting in the native window

`HoverWithNativeMirror` is `Style_PointPickedOnMouseMoveEvt` through the native view: the native point pick, the
native id resolution, the same Controller data, and highlights in a native selection layer (a renderer at layer 2,
created on first use, the widgets moved to layer 3). Every point mode is ported (node, element, edge, surface, edge
angle, surface angle, part, geometry, geometry vertex, edge, surface, edge angle, surface angle, part) and the
highlight branch of `PickByArea` (node, element, part, geometry). A highlight is built from the mesh the Controller
returns (vertices, lines, surface cells, the outer faces of volume cells by `vtkUnstructuredGridGeometryFilter`, the
element edges in the element edges mode), a part highlight copies the part's model edges or draws its silhouette.

With the selection area on and the native view in the window, a click (press and release within 2 px) selects through
`PickWithNativeMirror`, the mouse moving without a button highlights, a drag rotates the view and selects nothing.
Checked by feeding the handler a move, a click and a drag in a hidden session (element 1500 selected, the drag kept
it).

Check `selection_s2` (`VtkPort/Selection/Run_S2.sh`): per mode three points on the model and, where the mode
highlights an area, a box; the highlight is what the picture changed against the picture of the same mode without it,
on each path. 74 of 74 pass: shapes cover each other within two pixels (coverage 1.0000 on the solid), mean colors
differ by 24 levels at most and 6.6 on average (the limit is 40); 4 hinge points are reported as border picks (a click there selects other ids
on the two paths, the slice C finding), not failed.

Rendering default found: **VTK 9 draws an unlit actor in (ambient + diffuse) x color and lights lines and points as if
they faced the light**, so the orange mouse highlight (255, 165, 0) of a line or a point came out yellow
(250, 245, 22); VTK 5.8 drew it in about its own color. Highlights of only lines and points are drawn unlit with the
color as ambient color only. Stream G meets the same with its edges and symbols.

Fix cycles: (render) the yellow lines and points above; (check) on the hinge, ActiViz draws selection layer items of
the model (the joint surfaces, stream G's area) after the first mouse move of a mode, so each mode's picture without a
highlight is taken after a first move; (check, twice) border points are marked by the mode's click ids and the element
pick (several modes show the element face under the point).

## S3 - the geometry selection modes

The geometry modes need no code of their own beyond S1 and S2: their ids go through `GetGeometryPickProperties`
(S1) and their hover paths (`PickByGeometry`, `PickByGeometryVertex`, `PickByGeometryEdgeAngle`, the geometry cases
of `PickByEdge`, `PickBySurface`, `PickByActor`) are in S2. Check `selection_s3` (`VtkPort/Selection/Run_S3.sh`): the
pick plan in the seven geometry modes (geometry, vertex, edge, surface, edge angle, surface angle, part) on the
geometry view of the solid: 1,372 of 1,372 picks pass, 6 tolerance picks reported; the closest cells agree to 7.5e-7.
The S2 pictures cover the geometry hover on the same view (23 picture pairs).

## The full check run

`TEMP=<private> bash VtkPort/Harness/Run_Port_Checks.sh`, 6 minutes: ALL PASSED. groundwork; geometry_scenes 36 of
36; slice A 36 of 36; slice B 20 of 20 and the embedded check; slice C 1,960 of 1,960; selection_s1 1,960 of 1,960;
selection_s2 74 of 74; selection_s3 1,372 of 1,372. Context of the session at the end: 458k tokens.

## Decision for the user

**`vtkControl.NativeExactPicking`** (default on; `vtkn_view_set_exact_picking`) holds the picking behavior the native
view brought in slice C:

- on: a point pick takes the exact point where the ray of the pixel meets the picked part, and where the pixel misses
  the model, a cell pick catches geometry within the selection distance (the intent of `GetPickPoint`, which VTK 5.8
  never fulfilled). Picks on an element border can resolve to the neighbor of the element ActiViz picked
  (0.6 - 0.8 px apart), and a click up to 7 px beside the model now selects;
- off: the point is rebuilt from the depth of the hardware pick, as `GetPropPickerPickPosition` did, and nothing is
  caught beyond the pixel. VTK 9's depth misses the surface by pixels (slice C), so this does not restore the ActiViz
  ids either; it only removes the tolerance picks.

Selections recorded with ActiViz on an element border may resolve to the neighboring element on regeneration under
either setting. The off setting is built and wired but no check runs it.

## Not ported (with the owner)

- The probe label of a query (`_probeWidget` in the PickBy* paths): a widget, stream W.
- Section views in picking (`ResolveSectionViewPick`, `GetClosestPointOnActorSurface`, the section view parent of a
  pick): stream G4 brings the section view; picking on it follows.
- Transformed copies (symmetry, patterns): their volume grid is not sent transformed (G4).
- The box selection (rubber band) in the native window: the native window has the trackball style until W2; the
  native area pick and its highlight are ported and checked through the mirror.
- The persistent selection highlights (`AddCells`/`AddPoints` on the selection layer after a click): stream G mirrors
  them. Until G is merged, a click in the native window selects (the ids reach the Controller) but the selection shows
  only in the ActiViz view.
- `GetRotationPointPickPoint` (the rotation center, stream W2), `PickAreaHardware` (unused).

## Notes for the integration

- Two renderers for layer 2: stream G's `SelectionRenderer` and stream S's `HighlightRenderer`, both moving the
  widgets to layer 3. Keep G's and let `View::AddHighlightActor` draw into it.
- Shared lines added: `vtkNativeView.h` (public id and highlight methods, private members), `PartActor::GetModelEdges`,
  `CMakeLists.txt` (FiltersExtraction, FiltersGeometry, FiltersHybrid and two sources), hooks in `vtkControl.cs`:
  the four id helpers, `ClearCurrentMouseSelection`, `SetNativeAreas` (wire and unwire), `NativeMirrorPickPoint`.
- Selection API version 2.

## Review list for the user (selection and hover in the native window)

Run `VtkPort/Selection/Toggle_Native_Selection.py` from the Python panel (after the merge, from the main checkout;
`NATIVE_EXACT_PICKING` at its top). Then, on a meshed model (Joint_Hinge) and a geometry model (L_bracket_r.step):

1. Each select mode of a selection dialog (node, element, edge, surface, edge angle, surface angle, part; the
   geometry modes on the geometry view): move the mouse over the model and compare the highlight with the ActiViz
   view (toggle back): the same item, the orange color, the line and point sizes, the element edges in the element
   mode.
2. Click to select in each mode; the ids in the dialog must be the ones ActiViz selects (border picks aside). Until
   stream G is merged, the selection shows only after switching back to the ActiViz view.
3. Click a few pixels beside the model edge: with exact picking on, the edge is selected; decide whether to keep it.
4. Drag: the view rotates and nothing is selected.
5. The hover over the parts of an assembly, near the contact of two parts: the highlighted item belongs to the part
   under the pointer.
