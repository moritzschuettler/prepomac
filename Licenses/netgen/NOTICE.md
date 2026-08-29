# Meshers\Netgen - license notice

## NetGenMesher.exe

Built from `C:\Pc\Programiranje\NetgenMesher` (not part of this repository) and licensed under the
**GNU General Public License v3**, the same license as PrePoMax itself. The license text is the
`LICENSE` file in the base folder root.

## Libraries it links

| Component | Shipped as | License | Text |
|---|---|---|---|
| Netgen 5.3.1 | `nglib.dll` | LGPL-2.1 | `LGPL-2.1.txt` |
| OpenCASCADE Technology 8.0.0 | 74 x `TK*.dll` | LGPL-2.1 with the Open CASCADE exception | `LGPL-2.1-OCCT.txt`, `OCCT_LGPL_EXCEPTION.txt` |
| pthreads-win32 2.9.1 | `pthreadVC2.dll` | LGPL-2.1 | `LGPL-2.1-pthreads-win32.txt` |
| Microsoft Visual C++ runtime | `msvcp140.dll`, `msvcr100.dll`, `vcomp140.dll`, `vcruntime140.dll`, `vcruntime140_1.dll` | Microsoft redistributable terms | - |

The Netgen and OpenCASCADE texts were taken from their upstream repositories (NGSolve/netgen and
Open-Cascade-SAS/OCCT); Netgen 5.3.1's own source tarball carries no license file. The pthreads-win32
text is the `COPYING.LIB` from the prebuilt distribution actually used for the build.

GPL-3 permits linking LGPL-2.1 libraries, so NetGenMesher.exe being GPL-3 raises no conflict.

## Outstanding

These dlls also ship in `Meshers\Netgen`. They come with the OpenCASCADE Windows binary
distribution rather than being built here, and their license terms have **not** been established -
several are dual-licensed or depend on build flags, so no text is asserted for them:

`FreeImage.dll`, `avcodec-57.dll`, `avformat-57.dll`, `avutil-55.dll`, `swscale-4.dll`,
`freetype.dll`, `jemalloc.dll`, `openvr_api.dll`, `tbb12.dll`, `zlib.dll`

These are required: removing all ten makes every NetGenMesher command fail, so they cannot
simply be dropped and their terms do need to be established.

Separately, 48 of the 91 dlls in this folder (all `TK*.dll`, 19 MB) are never loaded and can be
removed - see `Notes/NetgenMesher-minimal-dll-set.md` in the repository. That does not affect the
licensing question above, since none of the ten are among them.
