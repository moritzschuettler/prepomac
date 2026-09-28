# QuadWild-BiMDF notice (modified build)

PrePoMax distributes Windows binaries of a modified QuadWild-BiMDF as separate
processes:

- quadwild.exe
- quad_from_patches.exe

They are built from a local fork of QuadWild-BiMDF 0.0.2 with these changes: prescribed
nodal positions on edges (optional <input>.cnodes constraint file), pillowing that does not cross a prescribed curve, size
grading, field-aligned smoothing, quality optimisation and optional quality-driven edge rotations of the final mesh, an optional geometric target for the irregular node inside non-quadrilateral patches, an optional feedback search over the element counts of the patch sides, a boundary layer of quads around holes (pillowing), two elements across thin patches whose pattern degenerates, equal counts on the long sides of strips one element wide, patch corners where a patch side bends at a corner of the geometry, the better half of a mirror symmetric part split on its symmetry plane on both sides, patches the subdivision cannot solve merged with a neighbour, at least one element per 45 degrees of a curved line, holes with elements of one size, nodes on feature lines kept on their own line in the smoothing, inner nodes kept off holes and lines in the smoothing, holes kept with elements of one size where only a nearby hole spoils the mesh (their even spacing undone only on the holes where it folds a quad), an optional finer triangulation of the input with the edges between prescribed nodes kept whole (refine_input, off), the mesh made again without the thin patch and strip retries where they leave a bad quad, an optional pillowing of prescribed nodes on feature lines that are in one quad at a flat angle (pillow_feature_lines, off), two columns of quads with rows across corners where lines meet at a small angle, ending in one quad (a kite) at the tip, feature lines kept where they end on a hole with a boundary layer, projection of the final nodes onto the
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

SHA-256 checksums of the binaries (modified build, 2026-09-25):

    C7D7D412DE7CDC58D3B07FB645C8DC141ECE1AAF76A15C1153E7C19FE2659B9B  quadwild.exe
    C5954E4220DCE40E799FB77954EC9B47C98C44E62466B78730DB6A74E4F0DDAE  quad_from_patches.exe
