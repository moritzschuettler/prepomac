# NetGenMesher (vendored binaries)

The meshing and CAD utility PrePoMax shells out to for Netgen meshing and all OpenCASCADE geometry
operations. Built from `C:\Pc\Programiranje\NetgenMesher` (not part of this repository), so a clean
checkout could not otherwise produce a working `Meshers\Netgen`.

| Component | Files | License |
|---|---|---|
| NetGenMesher | `NetGenMesher.exe` | GPL-3.0 - own build, same license as PrePoMax |
| Netgen 5.3.1 | `nglib.dll` | LGPL-2.1 |
| OpenCASCADE 8.0.1 | 29 x `TK*.dll` | LGPL-2.1 with the Open CASCADE exception |
| pthreads-win32 2.9.1 | `pthreadVC2.dll` | LGPL-2.1 |
| zlib 1.2.11 | `zlib.dll` | zlib License |
| Visual C++ runtime | `msvcp140`, `msvcr100`, `vcomp140`, `vcruntime140`, `vcruntime140_1` | Microsoft redistributable terms |

This is the trimmed runtime set only - 1 exe + 37 dlls. OpenCASCADE 8.0.1 was built locally with
the optional image, video, visualization and parallel-runtime dependencies disabled. The bundle
therefore needs no FreeImage, FFmpeg, FreeType, jemalloc, OpenVR or TBB dlls. See
`Notes/NetgenMesher-minimal-dll-set.md` for the exact runtime closure.

## Refreshing after a NetGenMesher rebuild

The NetGenMesher build already produces exactly this set in
`C:\Pc\Programiranje\NetgenMesher\NetgenMesher\Release`, so:

    copy that folder's *.exe and *.dll over this one, then commit

Only `NetGenMesher.exe` (~80 KB) and `nglib.dll` (~2 MB) normally change between Netgen rebuilds;
the OpenCASCADE and runtime dlls change when their corresponding dependencies are upgraded.

The `PostBuildEvent` in `PrePoMax.csproj` copies this folder into `$(TargetDir)Meshers\Netgen`.
License texts are added separately from `Licenses/netgen/`.
