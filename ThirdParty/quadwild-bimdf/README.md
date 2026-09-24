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

SHA-256 checksums of the binaries (modified build, 2026-09-23):

    2BB3547DCF24F9F9B48DFC5D329A4A1F44E0C49F44C515826D01A4EC3499F89F  quadwild.exe
    F29779BD800A859CDB6E6AADC658793DF5CBBD67F67760023361DE61DCA88225  quad_from_patches.exe
