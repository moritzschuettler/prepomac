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

    C20009356C669AE4B6BE5E04C1337A875532F704CB4D6C2B9F26E92ED7A62A16  quadwild.exe
    C680E6F18E1848FDA3C462AD27235C88B914CCFFEF1E821AF384F1F582EAF3E0  quad_from_patches.exe
