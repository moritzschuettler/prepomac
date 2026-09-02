# QuadWild-BiMDF runtime

This directory vendors the Windows binary distribution identified by its included
README_release.md as QuadWild-BiMDF version 0.0.2. It was supplied for the
PrePoMax QuadWild integration and contains:

- quadwild.exe
- quad_from_patches.exe
- the complete config/ directory expected at runtime

The executables are deployed together to Meshers/QuadWild/. QuadWild must be
started with that directory as its working directory so relative config references
continue to resolve.

Upstream source: https://github.com/cgg-bern/quadwild-bimdf

License: GPL-3.0. The distributable license and provenance notice are stored in
Licenses/quadwild/ and copied beside the executables during the PrePoMax build.

SHA-256 checksums of the supplied binaries:

    9F63C337593629957A3FCC50F6306CD3F7BC55D5AD4309ED8C91790B0746AD01  quadwild.exe
    5C43C16E4838BF0D4498EEF32628DC28AC769F0448805E3DA793B2311B7CBA34  quad_from_patches.exe
