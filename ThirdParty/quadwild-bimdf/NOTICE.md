# QuadWild-BiMDF notice (modified build)

PrePoMax distributes Windows binaries of a modified QuadWild-BiMDF as separate
processes:

- quadwild.exe
- quad_from_patches.exe

They are built from a local fork of QuadWild-BiMDF 0.0.2 with these changes: prescribed
nodal positions on edges (optional <input>.cnodes constraint file), pillowing, size
grading, quality optimisation of the final mesh, projection of the final nodes onto the
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

SHA-256 checksums of the binaries (modified build, 2026-09-23):

    2BB3547DCF24F9F9B48DFC5D329A4A1F44E0C49F44C515826D01A4EC3499F89F  quadwild.exe
    C406F9B18D3AFCC249F3E7D7E6E82F8429C259499C83821BD016CEE8D46654A5  quad_from_patches.exe
