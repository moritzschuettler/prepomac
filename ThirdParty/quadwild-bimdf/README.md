# QuadWild-BiMDF runtime

This directory vendors Windows binaries of a modified QuadWild-BiMDF, built from a
local fork of version 0.0.2 (see NOTICE.md for the changes). README_release.md is the
upstream 0.0.2 release note. The directory contains:

- quadwild.exe
- quad_from_patches.exe
- the complete config/ directory expected at runtime

The executables are deployed together to Meshers/QuadWild/. QuadWild must be
started with that directory as its working directory so relative config references
continue to resolve.

Upstream source: https://github.com/cgg-bern/quadwild-bimdf

License: GPL-3.0. The distributable license and provenance notice are stored in
Licenses/quadwild/ and copied beside the executables during the PrePoMax build.

SHA-256 checksums of the binaries (modified build, 2026-09-25):

    C7D7D412DE7CDC58D3B07FB645C8DC141ECE1AAF76A15C1153E7C19FE2659B9B  quadwild.exe
    E01B58228A6D18E7D94044D928F5B7421C095AF5F1110E1948BA0A13939B2701  quad_from_patches.exe
