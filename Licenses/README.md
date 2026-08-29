# Third-party licenses

PrePoMax is distributed under the GNU General Public License v3 (see [LICENSE](../LICENSE) in the
repository root). It ships several third-party mesher binaries that are not built from this
repository. This folder holds the license texts that must be distributed alongside them.

The `AfterBuild` post-build event in `PrePoMax/PrePoMax.csproj` copies these into the build output,
so a release built from this repository carries them automatically. Each mesher's license is copied
only when that mesher's binary is actually present in the base folder.

## Inventory

| Component | Shipped as | License | Verified from |
|---|---|---|---|
| PrePoMax | `PrePoMax.exe` | GPL-3.0 | `LICENSE` in this repository |
| Gmsh 4.13 | `lib\gmsh-4.13.dll` | GPL-2.0-or-later, with an exception permitting combination with Netgen, METIS, OpenCASCADE and ParaView | `share/doc/gmsh/LICENSE.txt` in the vendored SDK under `ThirdParty/` |
| Mmg | `Meshers\Mmg\mmgs.exe` (5.5.2), `Meshers\Mmg\mmg3d.exe` (5.7.0), vendored in `ThirdParty/mmg/` | LGPL-3.0-or-later | `LICENSE` at github.com/MmgTools/mmg |
| QuadWild-BiMDF 0.0.2 | not yet shipped | GPL-3.0 | `LICENSE` at github.com/cgg-bern/quadwild-bimdf |
| NetGenMesher | `Meshers\Netgen\NetGenMesher.exe` | GPL-3.0 (own build, same license as PrePoMax) | the author |
| Netgen 5.3.1 | `Meshers\Netgen\nglib.dll` | LGPL-2.1 | `LICENSE` at github.com/NGSolve/netgen |
| OpenCASCADE 8.0.0 | `Meshers\Netgen\TK*.dll` | LGPL-2.1 with the Open CASCADE exception | github.com/Open-Cascade-SAS/OCCT |
| pthreads-win32 2.9.1 | `Meshers\Netgen\pthreadVC2.dll` | LGPL-2.1 | `COPYING.LIB` in the prebuilt package used for the build |

Gmsh, Mmg and QuadWild are all GPL/LGPL v3-compatible with PrePoMax's own GPL-3.0, so bundling them
raises no license-compatibility question. Mmg's own LICENSE requires that both `COPYING.LESSER`
(LGPL-3) and `COPYING` (GPL-3) travel with the binaries, which is why all three files are here.

## Partly unresolved: the Netgen folder

`NetGenMesher.exe` is the author's own build and is GPL-3, and the libraries it links are covered
above. But `Meshers\Netgen` also ships ten image/video/compression dlls that come with the
OpenCASCADE binary distribution (FreeImage, FFmpeg, FreeType, jemalloc, OpenVR, TBB, zlib) whose
exact terms have not been established - several are dual-licensed or depend on build flags.
See [netgen/NOTICE.md](netgen/NOTICE.md); 48 of the 91 dlls in that folder are never loaded and
could simply be dropped.

## Corresponding source

GPL-3 section 6 and LGPL-3 require a route to the source for the binaries that are conveyed. For each
component above, record the exact upstream version shipped and keep the matching source archive, or
publish a written offer with the release. The versions currently shipped are listed in the table.
