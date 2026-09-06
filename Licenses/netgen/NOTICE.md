# Meshers\Netgen - license notice

## NetGenMesher.exe

Built from `C:\Pc\Programiranje\NetgenMesher` (not part of this repository) and licensed under the
**GNU General Public License v3**, the same license as PrePoMax itself. The license text is the
`LICENSE` file in the base folder root.

## Libraries it links

| Component | Shipped as | License | Text |
|---|---|---|---|
| Netgen 5.3.1 | `nglib.dll` | LGPL-2.1 | `LGPL-2.1.txt` |
| OpenCASCADE Technology 8.0.1 | 29 x `TK*.dll` | LGPL-2.1 with the Open CASCADE exception | `LGPL-2.1-OCCT.txt`, `OCCT_LGPL_EXCEPTION.txt` |
| pthreads-win32 2.9.1 | `pthreadVC2.dll` | LGPL-2.1 | `LGPL-2.1-pthreads-win32.txt` |
| zlib 1.2.11 | `zlib.dll` | zlib License | `ZLIB-LICENSE.txt` |
| Microsoft Visual C++ runtime | `msvcp140.dll`, `msvcr100.dll`, `vcomp140.dll`, `vcruntime140.dll`, `vcruntime140_1.dll` | Microsoft redistributable terms | - |

The Netgen and OpenCASCADE texts were taken from their upstream repositories (NGSolve/netgen and
Open-Cascade-SAS/OCCT); Netgen 5.3.1's own source tarball carries no license file. The pthreads-win32
text is the `COPYING.LIB` from the prebuilt distribution actually used for the build.

GPL-3 permits linking LGPL-2.1 libraries, so NetGenMesher.exe being GPL-3 raises no conflict.

## Runtime closure

OpenCASCADE 8.0.1 was built locally with the optional image, video, visualization and
parallel-runtime dependencies disabled. The previous FreeImage, FFmpeg, FreeType, jemalloc,
OpenVR and TBB dlls are no longer imported and no longer ship in `Meshers\Netgen`. The remaining
38-file runtime set is documented in `Notes/NetgenMesher-minimal-dll-set.md`.
