# QuadWild-BiMDF notice (modified build)

PrePoMax distributes Windows binaries of a modified QuadWild-BiMDF as separate
processes:

- quadwild.exe
- quad_from_patches.exe

They are built from a local fork of QuadWild-BiMDF 0.0.2 with these changes: prescribed
nodal positions on edges (optional <input>.cnodes constraint file), pillowing, size
grading, quality optimisation of the final mesh, projection of the final nodes onto the
original input surface, stage-1 guards, and a
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

    42CE53BA9CC15D4F64577B199A8E7A4B4DBFE72461CB95DF79A62F8218A822FF  quadwild.exe
    F41D4DA565A2E7BCDC1193B326F3F5E4EB916F9021D85BF2884546A43C4F4566  quad_from_patches.exe
