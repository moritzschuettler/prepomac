# QuadWild-BiMDF notice (modified build)

PrePoMax distributes Windows binaries of a modified QuadWild-BiMDF as separate
processes:

- quadwild.exe
- quad_from_patches.exe

They are built from a local fork of QuadWild-BiMDF 0.0.2 with these changes: prescribed
nodal positions on edges (optional <input>.cnodes constraint file), pillowing that does not cross a prescribed curve or a feature line, size
grading, field-aligned smoothing, quality optimisation and optional quality-driven edge rotations of the final mesh, an optional geometric target for the irregular node inside non-quadrilateral patches, an optional feedback search over the element counts of the patch sides, a boundary layer of quads around holes (pillowing), two elements across thin patches whose pattern degenerates, equal counts on the long sides of strips one element wide, patch corners where a patch side bends at a corner of the geometry, the better half of a mirror symmetric part split on its symmetry plane on both sides, patches the subdivision cannot solve merged with a neighbour (never across a feature line), at least one element per 45 degrees of a curved line, holes with elements of one size, nodes on feature lines kept on their own line (split at the corners of the geometry) in the smoothing, inner nodes kept off holes and lines in the smoothing, holes kept with elements of one size where only a nearby hole spoils the mesh (their even spacing undone only on the holes where it folds a quad), an optional finer triangulation of the input with the edges between prescribed nodes kept whole (refine_input, off), the mesh made again without the thin patch and strip retries where they leave a bad quad, the previous subdivision kept where a retry cannot be solved, transfinite faces like those of gmsh (the faces with 3 and 4 sides taken out of the patch layout as single patches, meshed as three structured blocks or a structured grid with equal counts on opposite sides and transfinite node positions), structured faces given by PrePoMax (optional <input>.tfaces file) kept as patches the subdivision meshes as structured grids, with equal counts on opposite sides solved together with the rest of the mesh and the meshing refused where they cannot be given or their grid folds, the finish of a quad mesh reused where a fallback quantises to the same mesh again, the mesh made again without the corners of very short patch sides where the fallbacks leave a bad quad, a given structured face meshed as an ordinary patch where the counts fixed on its sides cannot be made equal, the patch of a given face placed by the one that carries its corners, the mesh made again with padded patterns where a pattern leaves a flat quad at a prescribed node on a feature line, the nodes of closed feature lines (the edge of a chamfer around a hole) spaced evenly like those of holes, the nodes of surfaces touching at a point joined into one, a corner added to a 3-sided patch whose fixed sides allow no pattern, the counts of a folded strip one element wide put in step along its two sides where the mesh has a bad quad, a given face whose grid folds meshed as an ordinary patch, an optional pillowing of prescribed nodes on feature lines that are in one quad at a flat angle (pillow_feature_lines, off), two columns of quads with rows across corners where lines meet at a small angle, ending in one quad (a kite) at the tip, feature lines kept where they end on a hole with a boundary layer, the longer of the long sides of a strip given the coarser count where the two cannot both get their own, with the sides of the structured faces beside such strips spaced evenly, a subdivision with more than three times the quads of the target size refused before the smoothing, the feature edges looked up in a grid in the smoothing, projection of the final nodes onto the
original input surface, stage-1 guards, patch corners on holes and curved lines (thin strips between holes merged), the target
size and minimum element counts on lines taken from the input mesh, and a
curvature-free fallback for the cross field on flat surfaces. The changes are described in
QUADWILD_PRESCRIBED_NODES_INVESTIGATION.md and collected in prescribed_nodes.patch in
the fork. The runtime configuration tree is unmodified; README_release.md is the
upstream 0.0.2 release note.

Upstream project:
https://github.com/cgg-bern/quadwild-bimdf

GPL-3.0 requires that the corresponding source of these modified binaries, including
the changes above, is made available to anyone who receives them.

License: GNU General Public License version 3. The copy in Licenses/quadwild is
deployed as LICENSE beside the executables.

SHA-256 checksums of the binaries (modified build, 2026-10-02):

    C7D7D412DE7CDC58D3B07FB645C8DC141ECE1AAF76A15C1153E7C19FE2659B9B  quadwild.exe
    AA40B17EA804303C3264D551011D12FF639CF4952CCAEF46ACF24750077054F4  quad_from_patches.exe
