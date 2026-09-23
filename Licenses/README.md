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
| Gmsh 4.13, patched | `lib\gmsh-4.13.dll`, vendored in `ThirdParty/gmsh-4.13.0-Windows64-sdk/` | GPL-2.0-or-later, with an exception permitting combination with Netgen, METIS, OpenCASCADE and ParaView | `share/doc/gmsh/LICENSE.txt` in the vendored SDK under `ThirdParty/` |
| OpenCASCADE 7.7.0 | statically linked into `lib\gmsh-4.13.dll` | LGPL-2.1 with the Open CASCADE exception | github.com/Open-Cascade-SAS/OCCT |
| Mmg | `Meshers\Mmg\mmgs.exe` (5.5.2), `Meshers\Mmg\mmg3d.exe` (5.7.0), vendored in `ThirdParty/mmg/` | LGPL-3.0-or-later | `LICENSE` at github.com/MmgTools/mmg |
| QuadWild-BiMDF 0.0.2, modified | `Meshers\QuadWild\quadwild.exe`, `Meshers\QuadWild\quad_from_patches.exe`, vendored in `ThirdParty/quadwild-bimdf/` | GPL-3.0 | `LICENSE` at github.com/cgg-bern/quadwild-bimdf |
| NetGenMesher | `Meshers\Netgen\NetGenMesher.exe`, vendored in `ThirdParty/netgen/` | GPL-3.0 (own build, same license as PrePoMax) | the author |
| Netgen 5.3.1 | `Meshers\Netgen\nglib.dll` | LGPL-2.1 | `LICENSE` at github.com/NGSolve/netgen |
| OpenCASCADE 8.0.1 | `Meshers\Netgen\TK*.dll` | LGPL-2.1 with the Open CASCADE exception | github.com/Open-Cascade-SAS/OCCT |
| pthreads-win32 2.9.1 | `Meshers\Netgen\pthreadVC2.dll` | LGPL-2.1 | `COPYING.LIB` in the prebuilt package used for the build |
| zlib 1.2.11 | `Meshers\Netgen\zlib.dll` | zlib License | license notice in the vendored `zlib.h` |

Gmsh, Mmg and QuadWild are all GPL/LGPL v3-compatible with PrePoMax's own GPL-3.0, so bundling them
raises no license-compatibility question. Mmg's own LICENSE requires that both `COPYING.LESSER`
(LGPL-3) and `COPYING` (GPL-3) travel with the binaries, which is why all three files are here.

## Gmsh build

`lib\gmsh-4.13.dll` is no longer the binary from the upstream Gmsh SDK. It is built from patched
Gmsh sources, published at `gitlab.com/MatejB/gmsh` on the branch `prepomax-quadqs-transfinite`, so
that the quasi-structured quad algorithm leaves alone the element counts a swept mesh pins on its
curves. Everything else in `ThirdParty/gmsh-4.13.0-Windows64-sdk/` is still the upstream SDK, the
import library and the headers among it.

The build is configured without the graphical interface and without the OpenCASCADE application
framework, neither of which PrePoMax calls, and links OpenCASCADE statically, so the dll carries no
`TK*.dll` beside it exactly as the upstream one did. It is built with Microsoft's compiler, which
cannot compile Gmsh's OpenMP directives, so the parallel tetrahedral mesher runs on one thread
where the upstream binary used several.

## Netgen runtime set

`NetGenMesher.exe` is the author's own build and is GPL-3, and the libraries it links are covered
above. OpenCASCADE 8.0.1 was built locally with its optional image, video, visualization and
parallel-runtime dependencies disabled, so the former FreeImage, FFmpeg, FreeType, jemalloc,
OpenVR and TBB dlls no longer ship. See [netgen/NOTICE.md](netgen/NOTICE.md).

## Corresponding source

GPL-3 section 6 and LGPL-3 require a route to the source for the binaries that are conveyed. For each
component above, record the exact upstream version shipped and keep the matching source archive, or
publish a written offer with the release. The versions currently shipped are listed in the table.

Gmsh is the one component shipped as a modified binary, so its corresponding source is the branch
named under "Gmsh build" above rather than an upstream release, and that branch has to stay
published for as long as the binary is conveyed.
