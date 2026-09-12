# Building the volume grid on demand

Working-level plan for stopping every part actor from carrying a `vtkUnstructuredGrid` of the whole
part mesh from the moment it is drawn. The grid stays; it is created the first time a feature needs
it and not before.

**The goal.** Drawing a part costs the surface and nothing else. Box selection, the section view,
interior result colouring and animated annotation anchors keep working exactly as they do now, on the
same grid content, produced later and only for the parts that need it. Nothing persisted changes:
`SelectionNodeMouse` keeps its six planes, command replay is untouched, no `.pmx` or `.pmh` file is
affected.

## What the grid is and who pays for it

`vtkMaxActor._frustumCellLocator` is a `vtkCellLocator` whose data set is a `vtkUnstructuredGrid` of
every node and every element of the part. The name is misleading twice over. It is not used for
picking - every mouse pick resolves on the surface `_cellLocator`, which wraps the mapper's own data
set and copies nothing ([vtkMaxActor.cs:404](vtkControl/vtkMax/Actor/vtkMaxActor.cs:404),
[vtkControl.cs:1923](vtkControl/vtkControl.cs:1923)). And it is barely a locator: `LazyEvaluationOn`
defers only the search tree, and the tree is built in exactly one place, the animated arrow anchor
([vtkMaxActor.cs:2154](vtkControl/vtkMax/Actor/vtkMaxActor.cs:2154)). Everywhere else the code calls
`GetDataSet()` and uses the grid as a grid.

The cost is paid at every draw of every part:

1. `FeMesh.GetSetNodesAndCells` copies the part into renumbered managed arrays, one `int[]` per
   element and one `double[]` per node ([FeMesh.cs:9379](CaeMesh/FeMesh.cs:9379)).
2. `AddNodeAndCellDataToGrid` pushes them into VTK one P/Invoke call per point, per cell and per id
   ([vtkMaxActor.cs:1081](vtkControl/vtkMax/Actor/vtkMaxActor.cs:1081)), from
   [vtkMaxActor.cs:973](vtkControl/vtkMax/Actor/vtkMaxActor.cs:973).
3. The managed arrays become garbage; the VTK grid lives as long as the actor.

For a solid part the grid holds all volume elements, so it dwarfs the surface poly data, and it is
paid again for every symmetry or pattern copy by `DeepCopy`
([vtkMaxActor.cs:482](vtkControl/vtkMax/Actor/vtkMaxActor.cs:482)) and once more by the transform
filter that rebuilds it ([vtkControl.cs:6773](vtkControl/vtkControl.cs:6773)).

### Producers

| Where | View | What it builds |
|---|---|---|
| [Controller.cs:16490](PrePoMax/Controller.cs:16490) `GetGeometryPartActorData` | geometry, when `pickable` | all nodes and cells of the geometry part |
| [Controller.cs:16628](PrePoMax/Controller.cs:16628) `GetModelPartActorData` | model | all nodes and cells of the part |
| [Controller.cs:22336](PrePoMax/Controller.cs:22336) `GetResultPartActorData` via [FeResults.cs:6417](CaeResults/FeResults.cs:6417) | results, static | nodes, cells, values and result field |
| [Controller.cs:23546](PrePoMax/Controller.cs:23546) field change | results | values and result field for all interior nodes, every part |
| [FeResults.cs:6515](CaeResults/FeResults.cs:6515) `GetScaleFactorAnimationData` | scale-factor animation | grid plus coordinates and values for **every frame** |
| [Controller.cs:23244](PrePoMax/Controller.cs:23244) `DrawHarmonicAnimation` | harmonic animation | grid plus coordinates and values for **every frame** |
| [Controller.cs:23113](PrePoMax/Controller.cs:23113) `DrawTimeIncrementAnimation` | time-increment animation | grid once; frame nodes on demand through `GetAnimationLocatorNodes` |

### Consumers

| Where | Needs |
|---|---|
| Box selection [vtkControl.cs:2082](vtkControl/vtkControl.cs:2082) and the rubber-band highlight [vtkControl.cs:1534](vtkControl/vtkControl.cs:1534) | grid, for `vtkExtractSelectedFrustum`; bounds, for the pre-test |
| Section view: `HasVolumeCells` [vtkControl.cs:5527](vtkControl/vtkControl.cs:5527), planar cut [vtkControl.cs:5602](vtkControl/vtkControl.cs:5602), closed-surface merge [vtkControl.cs:5750](vtkControl/vtkControl.cs:5750), element-wise cut [vtkControl.cs:5837](vtkControl/vtkControl.cs:5837) | grid with point scalars |
| Skin-actor filter [vtkControl.cs:6222](vtkControl/vtkControl.cs:6222) | grid *presence* as a flag only |
| Result scalars [vtkControl.cs:8078](vtkControl/vtkControl.cs:8078) and removal [vtkControl.cs:8096](vtkControl/vtkControl.cs:8096) | grid, dereferenced unconditionally |
| Transformed copies [vtkMaxActor.cs:482](vtkControl/vtkMax/Actor/vtkMaxActor.cs:482), [vtkControl.cs:6773](vtkControl/vtkControl.cs:6773) | grid, copied and transformed |
| Animation frame [vtkMaxActor.cs:2079](vtkControl/vtkMax/Actor/vtkMaxActor.cs:2079), interior node annotations [vtkMaxActor.cs:2137](vtkControl/vtkMax/Actor/vtkMaxActor.cs:2137), arrow anchors [vtkMaxActor.cs:2151](vtkControl/vtkMax/Actor/vtkMaxActor.cs:2151) | grid points of the displayed frame; the tree, for anchors only |
| Node-to-actor map [vtkControl.cs:7785](vtkControl/vtkControl.cs:7785) and node-to-point map [vtkMaxActor.cs:1977](vtkControl/vtkMax/Actor/vtkMaxActor.cs:1977) | the part's node **ids** only, no grid |
| Frame extremes `ScaleValuesAndGetExtreme` [FeResults.cs:6572](CaeResults/FeResults.cs:6572) | node ids, coordinates and values on the C# side, no grid |

The last two rows matter: they are the reasons the exchange data cannot simply be dropped, and they
are satisfied by `BasePart.NodeLabels` without any VTK object.

## Decisions

- **D1 - build once, keep for the actor's lifetime.** The grid is created on first need and released
  with the actor. It is *not* released when the section view is switched off, because dragging the
  section plane calls `UpdateSectionView` on every move and a rebuild would be visible. The target
  saving is the draw of every part in every session; a session that opens a section view pays what it
  pays today, once. Releasing on `RemoveSectionView` is an optional later step (S7).
- **D2 - the Controller produces the content, through a callback.** `vtkControl` gets
  `Func<string, PartExchangeData> Controller_GetVolumeGridData`, wired in `FrmMain` beside
  `Controller_GetAnimationLocatorNodes` ([FrmMain.cs:437](PrePoMax/Forms/FrmMain.cs:437)). That
  callback is the existing precedent for pulling part data lazily, and it keeps `vtkControl` ignorant
  of the mesh. The callback returns the part as currently displayed: geometry, model or result mesh,
  current deformation, current field.
- **D3 - the surface bounds replace the grid bounds in the pre-test.** For a solid, shell or geometry
  part the bounding box of the rendered surface equals the bounding box of the volume. An actor
  outside the frustum therefore never builds its grid. In phase 1 both *intersecting* and *inside*
  actors build it, so box selection behaves exactly as today; a mesh-side test that avoids the grid
  altogether is S7.
- **D4 - grids are invalidated, not patched, on a field change.** `UpdateActorScalarField` stops
  writing interior values; it marks the actor's grid stale, and the next consumer re-requests the
  content through the callback, which now carries the new values. The Controller already removes and
  re-creates the section view around a field change ([Controller.cs:23539](PrePoMax/Controller.cs:23539)),
  so the rebuild lands where the cut is rebuilt anyway. `GetSetNodesCellsAndValues` for every part on
  every field change goes away.
- **D5 - one frame-nodes callback for all three animations.** The time-increment path already keeps
  only the displayed frame's locator nodes and fetches them per frame
  ([Controller.cs:23187](PrePoMax/Controller.cs:23187)). Scale-factor and harmonic animation are
  brought onto the same path. `FeResults.GetDeformedFlatCoor` is non-mutating and takes a node id
  subset, so a frame's grid points can be computed from a frame descriptor (field data and scale, or
  angle) without touching the mesh state.
- **D6 - a copy remembers its transform.** A transformed copy is created without a grid. It stores
  its source actor's name and the accumulated `vtkTransform`; when a grid is first needed the source's
  content is fetched and transformed at creation.
- **D7 - explicit flags replace grid presence.** `vtkMaxActorData` gains `HasVolumeCells` (the part's
  first element is three-dimensional) and `PartNodeIds` (`BasePart.NodeLabels`). `HasVolumeCells(actor)`
  and the filter at [vtkControl.cs:6222](vtkControl/vtkControl.cs:6222) read the flag; the two node
  maps read the ids.
- **D8 - names.** `FrustumCellLocator` is retired. The actor exposes `TryGetVolumeGrid(out
  vtkUnstructuredGrid grid)` (creates on demand, may return false for actors that have no source) and
  the anchor code builds its own `vtkCellLocator` over that grid when it needs a tree. `vtkMaxLocator`
  in `vtkControl/Locator` has no callers and is deleted.

## Cross-cutting rules

- `vtkControl` never reaches the mesh; everything comes through the callback (D2).
- The callback must describe what is on screen. Whoever changes displayed deformation, field or view
  either redraws (which discards the actors and their grids) or invalidates the grids (D4).
- Nothing that today reads `FrustumCellLocator != null` may keep doing so; every site moves to a flag
  or to `TryGetVolumeGrid` (D7).
- `PrePoMax` and `APIs` are C# 7.3; `vtkControl`, `CaeResults`, `CaeGlobals` are not pinned. No
  `.Designer.cs` changes are involved.

## Implementation status

Steps S1 to S5 are implemented and `PrePoMax.sln` builds. S0 was not run, because it needs the
application on screen; the S8 checklist is still to be run by hand for the same reason. S6 and S7 are
not started.

What landed, against the plan:

- `vtkMaxActorData` carries `VolumeGridAvailable`, `HasVolumeCells` and `PartNodeIds`; `CellLocator`
  is gone. `vtkMaxActor` exposes `TryGetVolumeGrid`, `InvalidateVolumeGrid` and
  `AppendVolumeGridTransform`; `FrustumCellLocator`, `FrustumPointData` and `vtkMaxLocator` are gone.
- The provider is `Controller.GetVolumeGridData(partName, frameNumber)`, wired through
  `vtkControl.Controller_GetVolumeGridData`; `vtkControl` passes the displayed frame for an animated
  part and -1 otherwise, so a grid built during an animation carries that frame's deformation.
- One refinement of D4: an invalidated grid is refreshed **in place** (points and scalars), not
  replaced, so an arrow anchor holding the grid stays valid across a field change.
- One refinement of D5: the frame descriptor holds field data, the base scale, per-frame ratios for
  the scale-factor animation and per-frame angles for the harmonic animation. Scale-factor frames go
  through the new `FeResults.GetScaledDeformedNodes`, which scales values the way the frame data
  did; harmonic frames set the angle, read, and restore it.
- `FeResults.GetScaleFactorAnimationData` and `GetTimeIncrementAnimationData` no longer return
  locator data; the scale-factor frame extremes are still searched over `part.NodeLabels`.
- The per-frame locator update now runs for every animated part that has a grid, and only for those,
  in place of the section-view-or-anchor condition.

## Steps

Each step builds and runs on its own. Build `vtkControl`, `CaeResults` and `CaeGlobals` singly with
`SolutionDir` set, then `PrePoMax.sln` for the whole.

### S0 - Baseline

Measure before changing anything, on a fine solid mesh (a few hundred thousand tets) and on a small
one: wall time of `DrawModel` and of `Add3DCells`, and the grid's `GetActualMemorySize` summed over
actors. `RecomputeMemorySize` ([vtkMaxActor.cs:2251](vtkControl/vtkMax/Actor/vtkMaxActor.cs:2251))
does not count the grid today; add it temporarily under `Debugger.IsAttached` and remove it with S3.
Keep the numbers in this file.

### S1 - The accessor, the flags and the callback

- `vtkMaxActorData`: add `bool HasVolumeCells` and `int[] PartNodeIds`. Keep `CellLocator` for this
  step so that both paths compile.
- `vtkMaxActor`: add `_volumeGrid`, `_volumeGridStale`, `_hasVolumeCells`, `_partNodeIds`,
  `_volumeGridSourceName`, `_volumeGridTransform`. Add `TryGetVolumeGrid(out vtkUnstructuredGrid)`,
  which returns the cached grid, or asks `vtkControl` for content when the grid is missing or stale and
  builds it through the existing `AddNodeAndCellDataToGrid`, setting global ids and scalars exactly as
  [vtkMaxActor.cs:977](vtkControl/vtkMax/Actor/vtkMaxActor.cs:977) does now. Add `InvalidateVolumeGrid()`.
  The provider is a `Func<string, PartExchangeData>` handed to the actor by `vtkControl`, or the actor
  asks `vtkControl` back through an internal delegate; either way `vtkControl` owns the callback.
- `vtkControl`: add `Controller_GetVolumeGridData`; `FrmMain` wires it.
- `Controller.GetVolumeGridData(string actorName)`: geometry view returns
  `GetSetNodesAndCells` on the geometry part; model view on `DisplayedMesh`; results view returns
  `CurrentResult.GetSetNodesCellsAndValues(part, _currentFieldData)`; unknown names return null.

### S2 - Move the consumers

- Box selection and the highlight: replace `locator.GetDataSet().GetBounds()` with
  `actor.Geometry.GetBounds()` for the pre-test, then `TryGetVolumeGrid` for intersecting and inside
  actors. The `_selectableActorsFilter` handling stays.
- Section view: `HasVolumeCells` reads the flag; `GetSectionViewActorFromSolid`,
  `GetSectionViewActorFromClosedSurface` and `GetSectionViewActorsElementWise` call `TryGetVolumeGrid`
  and return null when it fails. The filter at [vtkControl.cs:6222](vtkControl/vtkControl.cs:6222) tests
  the flag.
- `UpdateActorScalarField`: write surface scalars, call `InvalidateVolumeGrid` on the actor and its
  copies, drop the `frustumCellLocatorValues` and `frustumResultField` parameters through `FrmMain`.
  `RemoveActorScalarField` likewise.
- `TransformMaxActor`: transform the grid if it exists, otherwise store the transform on the copy.
  The copy constructor stops deep-copying the grid and records the source name.
- Anchors: `TryCreateAnimationArrowAnchor` calls `TryGetVolumeGrid`, builds a local `vtkCellLocator`
  over it, and falls back to the surface locator as now. `TryGetAnimationNodeData` and
  `TryGetAnimationNodeCoordinates` use `TryGetVolumeGrid`.
- Delete `vtkMaxLocator.cs` and remove it from `vtkControl.csproj`.

### S3 - Stop producing the grid data

- [Controller.cs:16490](PrePoMax/Controller.cs:16490), [Controller.cs:16628](PrePoMax/Controller.cs:16628),
  [Controller.cs:22336](PrePoMax/Controller.cs:22336): replace `data.CellLocator = ...` with
  `data.HasVolumeCells` and `data.PartNodeIds`. `GetVtkData` loses its locator argument.
- [Controller.cs:23546](PrePoMax/Controller.cs:23546): drop the `GetSetNodesCellsAndValues` call and
  the two locator arguments.
- `vtkMaxActor` constructor: the block at [vtkMaxActor.cs:973](vtkControl/vtkMax/Actor/vtkMaxActor.cs:973)
  goes; `InitializeResultFields` takes the frustum field from nothing and `_frustumResultField` is
  set when the grid is built from the callback's `Nodes.ResultField`.
- Remove `CellLocator` from `vtkMaxActorData` once nothing reads it.

### S4 - Animation

- `FeResults.GetScaleFactorAnimationData`: keep `locatorResultData.Nodes.Ids` and the per-frame
  `ScaleValuesAndGetExtreme`, which needs the interior coordinates and values on the C# side, but stop
  returning `locatorResultData` and stop filling its `NodesAnimation`. The per-frame `FlatCoor` for the
  extremes is computed and dropped.
- `DrawHarmonicAnimation`: remove the `CellLocator.NodesAnimation` lines at
  [Controller.cs:23244](PrePoMax/Controller.cs:23244) and [Controller.cs:23271](PrePoMax/Controller.cs:23271).
- `Controller.GetAnimationLocatorNodes(actorName, frameNumber)` becomes the frame-nodes callback for
  all three kinds. The Controller stores a frame descriptor array set by each `Draw*Animation`: time
  increment keeps `_animationLocatorFieldData[i]` and `_animationLocatorScale`; scale factor stores
  `_currentFieldData` and `scale * ratios[i]`, computed through `GetDeformedNodes` (non-mutating);
  harmonic stores the angle and computes by setting `SetComplexResultTypeAndAngle`, reading, and
  restoring the previous angle.
- `vtkMaxActor.InitializeAnimation`: `_animationLocatorNodeIdToPointId` is built when the grid is
  built, from the callback's `Nodes.Ids`. `InitializeAndAddAnimatedActor` registers
  `data.PartNodeIds` at [vtkControl.cs:7785](vtkControl/vtkControl.cs:7785).
- `UpdateAnimationLocatorNodes` ([vtkControl.cs:8360](vtkControl/vtkControl.cs:8360)) iterates only
  actors that have a grid, and only when the section view is active or an anchor or request names
  that actor.
- A grid first requested during an animation is built for the *current* frame: the actor asks the
  grid callback for topology and ids, then the frame callback for the frame's points and values.

### S5 - Copies

`TransformAllMaxActors` ([vtkControl.cs:6745](vtkControl/vtkControl.cs:6745)) chains copies, each
transformed relative to the previous, so a copy stores the accumulated transform (its base's transform
concatenated with its own). `TryGetVolumeGrid` on a copy fetches the source's content by
`_volumeGridSourceName`, builds, then applies `_volumeGridTransform` with `vtkTransformFilter`, exactly
as [vtkControl.cs:6773](vtkControl/vtkControl.cs:6773) does today.

### S6 - Optional: per-anchor updates and a narrower pick

- Anchors read a handful of points ([vtkMaxActorAnimationData.cs:174](vtkControl/vtkMax/Animation/vtkMaxActorAnimationData.cs:174)).
  When no section view is active the frame callback can be asked for the anchors' node ids only, and
  the actor writes them with `vtkPoints.SetPoint` into the existing grid points. `GetDeformedNodes`
  already accepts the subset.
- `PickByCell` ([vtkControl.cs:1001](vtkControl/vtkControl.cs:1001)) computes the hardware-picked
  actor's name and discards it before searching every visible actor's surface locator. Restrict the
  interactive resolution to that actor; keep the global search for regeneration, where there is no
  hardware pick.

### S7 - Optional: release and avoid

- Release grids in `RemoveSectionView` when no anchor and no request exists and no animation is
  running.
- Box selection without a grid: return the names of fully-inside actors and let `FrmMain` expand
  them to `part.NodeLabels` and `part.Labels`; test intersecting actors' nodes against the six planes
  on the mesh side, as `BoundingBox.TestBoundsAgainstFrustum` already does for the box. After this the
  grid exists only while a section view is active or an interior anchor is in use.

### S8 - Verification

Manual, in the running application, against the S0 numbers:

- Model view: box select nodes, elements, parts and geometry, with and without "completely inside";
  the same on a hidden part and on a pattern copy. Edit - Regenerate on a history with box selections.
- Section view: on, drag the plane, element-wise on and off, off; in geometry, model and results
  views; with a symmetry and a circular pattern; after a field and a component change.
- All three animations with a section view open and with an arrow annotation anchored inside a part;
  a node annotation on an interior node during animation.
- Undeformed copy, transformed copies with results.
- Memory and draw time on the S0 meshes.

## Risks

- **Mesh state during the callback.** `GetScaleFactorAnimationData` deforms the mesh per frame with
  `SetPartDeformation` and leaves it at the last frame. A static-path callback during an animation
  would read those coordinates. Animated actors must therefore use the frame descriptor path (S4),
  never `GetSetNodesCellsAndValues`.
- **Harmonic frames mutate result state.** `SetComplexResultTypeAndAngle` is global on the result.
  The frame callback must restore it, or the displayed static field changes under the user.
- **Regenerate with many box selections.** Every redraw discards the grids and each replayed frustum
  selection rebuilds those of the intersecting parts. The worst case equals today's cost; the common
  case is far below it.
- **Copies of copies.** A wrong accumulation order in S5 shows as a section view cut in the wrong
  place on the second pattern instance; the S8 pattern test covers it.
- **Silent nulls.** Every retired `FrustumCellLocator` dereference must become a `TryGetVolumeGrid`
  with a handled false, or a feature disappears without an error. Grep for `FrustumCellLocator` must
  return nothing at the end of S3.

## Exit criteria

- `grep FrustumCellLocator` and `grep CellLocator = new PartExchangeData` return nothing.
- Drawing a solid model allocates no `vtkUnstructuredGrid` per part; the S0 measurement shows it.
- Every item of S8 behaves as before the change.
- `Edit - Regenerate` on a history containing box selections reproduces the same sets.
