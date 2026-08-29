# NetGenMesher (vendored binaries)

The meshing and CAD utility PrePoMax shells out to for Netgen meshing and all OpenCASCADE geometry
operations. Built from `C:\Pc\Programiranje\NetgenMesher` (not part of this repository), so a clean
checkout could not otherwise produce a working `Meshers\Netgen`.

| Component | Files | License |
|---|---|---|
| NetGenMesher | `NetGenMesher.exe` | GPL-3.0 - own build, same license as PrePoMax |
| Netgen 5.3.1 | `nglib.dll` | LGPL-2.1 |
| OpenCASCADE 8.0.0 | 26 x `TK*.dll` | LGPL-2.1 with the Open CASCADE exception |
| pthreads-win32 2.9.1 | `pthreadVC2.dll` | LGPL-2.1 |
| OCCT third-party | `FreeImage`, `avcodec-57`, `avformat-57`, `avutil-55`, `swscale-4`, `freetype`, `jemalloc`, `openvr_api`, `tbb12`, `zlib` | see `Licenses/netgen/NOTICE.md` |
| Visual C++ runtime | `msvcp140`, `msvcr100`, `vcomp140`, `vcruntime140`, `vcruntime140_1` | Microsoft redistributable terms |

This is the trimmed runtime set only - 1 exe + 43 dlls. The NetGenMesher build ships 74 of the 83
OpenCASCADE dlls; 48 of those are never loaded and are excluded. See
`Notes/NetgenMesher-minimal-dll-set.md` for how that set was established.

## Refreshing after a NetGenMesher rebuild

The NetGenMesher build already produces exactly this set in
`C:\Pc\Programiranje\NetgenMesher\NetgenMesher\Release`, so:

    copy that folder's *.exe and *.dll over this one, then commit

Only `NetGenMesher.exe` (~80 KB) and `nglib.dll` (~2 MB) normally change; the OpenCASCADE and
runtime dlls are static, so the repository does not grow much per rebuild.

The `PostBuildEvent` in `PrePoMax.csproj` copies this folder into `$(TargetDir)Meshers\Netgen`.
License texts are added separately from `Licenses/netgen/`.
