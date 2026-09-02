# QuadWild sharp-edge smoke fixture

`SharpCube.obj` is a closed, consistently oriented triangular cube. `SharpCube.sharp` marks all 12 physical
cube edges as convex QuadWild features. Face and local-edge indices are zero based, and each physical edge is
listed once.

The smoke test runs preparation with remeshing disabled and supplies the external `.sharp` file. Successful
preparation must create `SharpCube_rem_p0.obj`, `.patch`, `.corners`, `.feature`, and `.c_feature` files. The
patch stage is run once with `flow.txt` and once with `flow_noalign.txt`; each result must import as a finite,
manifold shell mesh containing quadrilateral elements.

The quad generation stage writes both `_1_quadrangulation.obj` and `_1_quadrangulation_smooth.obj`. The two
share the same topology and differ only in node positions, and the `Smooth` setting selects which one is
imported. Both must be freshly written for the stage to count as complete, whichever one is used.

Smoothing improves the shape of most quads but can pull all corners of a face onto one sharp feature line. Such
a collapsed face has no area and is reported as a warning rather than rejecting the mesh. Degeneracy is measured
on the area of the face itself, relative to its longest edge - never on the area of one of its fan triangles: a
quad whose corner sits on the line joining its neighbours has a flat corner and a zero-area fan triangle, but a
perfectly usable area of its own.

The packaged `quad_from_patches.exe` can exit with Windows breakpoint code `0x80000003` after it has written a
complete result. The integration accepts a failed process status only when all expected files were freshly written;
the resulting OBJ must still pass the complete mesh validator.

The external `.sharp` file is deliberate: supplying it disables QuadWild's automatic feature detection. Closed
fixtures therefore need only the 12 model edges; open fixtures must additionally list every boundary edge.

## Model-edge recovery

The writer saves each source model edge as an ordered polyline (`QuadWildModelEdge`) carrying its own node ids,
open/closed state and median segment length. Recovery traces every saved edge independently: it collects the
output mesh edges lying inside that edge's tolerance tube, anchors on the resolved output nodes for the edge's
end points, and takes the cheapest connected path between them, weighting each mesh edge by its length, its
deviation from the polyline and whether it is a feature edge (patch-material boundary, free boundary or
angle-detected crease). Open edges are traced as a path, closed edges as the cheapest cycle through the seed
node. A traced path is accepted only if every sampled point of the source polyline lies within the coverage
tolerance of it.

Two independent scales set the tolerances, and both matter. Lateral drift of an output node away from the
feature line scales with the **source triangulation**; how far the closest output node can sit from a source
point measured **along** the feature scales with the **output quad size**, which the `Scale factor` setting
makes a free multiple of the traced triangle edge length. The tube half width therefore takes the larger of a
source-segment term and an output-quad term, and the anchor radius and coverage allowance are output-quad
based. A tolerance built only from the source segment length collapses as soon as `Scale factor` exceeds 1.

Anchors are resolved once per source model vertex, before any tracing, and shared by every model edge meeting
there. A vertex that is not a geometric corner - the split points of a hole divided into two edges, for example
- is not preserved by QuadWild, so the closest output node lies up to half an output quad edge away. Resolving
that node per model edge instead lets the two halves snap to different output nodes and leaves the mesh edges
between them owned by neither. Because the shared anchor can fall just past the vertex, onto the neighbouring
edge's side, the tube also admits a mesh edge incident to its own anchor whose far node is inside the tube.

All edges are traced on the strict tolerance first; only what is left is retried on the relaxed tolerance, so a
relaxed trace can never consume the mesh edges an exact match would have used. A committed path's mesh edges are
reserved, and a trace either commits its whole path or nothing.

Recovery is all or nothing. `SetPartVisualizationModelEdges` rebuilds the model edges as a contiguous zero-based
sequence, so a partial result would shift the ids that geometry selections refer to. On failure the imported
mesh keeps its angle-detected topology and the report names each unmatched edge, its mid-point coordinates and
the tolerance it missed.

`CoplanarSplit.obj` is the model-edge recovery regression fixture. Its three traced patches are coplanar, so
angle-based edge detection cannot find either interior boundary. Recovery must match `x=1` to the saved input-edge
polyline while discarding the internal QuadWild patch boundary at `x=0.5`. It must rebuild seven model edges in the
supplied zero-based order: six split outer-boundary edges and the shared interior model edge. The same recovery is
checked after first- and second-order OBJ import.

`CoplanarSplitNoMaterials.obj` has the same geometry without `usemtl` records. Without patch materials the
interior boundary is not a feature edge, so its strict trace finds an empty tube and the relaxed pass, which
considers every mesh edge, must recover the seven source edges. The six outer-boundary edges are still free
edges and must therefore be matched by the strict pass, before the relaxed pass runs.

`CoplanarSplitPerturbed.obj` moves the saved `x=1` feature to `x=1.08` in the output mesh. This lies outside the
strict tolerance and verifies recovery by the relaxed, source-edge-size-based tolerance.

`NoModelEdges.obj` verifies the valid zero-edge case. Recovery must clear the tetrahedron's angle-detected creases
and rebuild one connected visualization face with zero model edges and zero model vertices.

An additional harness case supplies an impossible extra source edge. Recovery must report failure without changing
the imported mesh's automatically detected visualization topology; mesh import then continues with a warning.
