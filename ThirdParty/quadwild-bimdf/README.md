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

    C20009356C669AE4B6BE5E04C1337A875532F704CB4D6C2B9F26E92ED7A62A16  quadwild.exe
    6C5B626F879F9F50E2B9C30BE2E307A5AECE02315645C2982C79D70205290834  quad_from_patches.exe
