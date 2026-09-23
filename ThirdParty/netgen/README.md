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
Deploy `NetGenMesher.exe` and `nglib.dll` as a matched pair. The current pair supports
`STL_SURFACE_MESH` for open STL shells and open-boundary `STL_EDGE_MESH`; optional refinement and
edge-node files may be omitted. Its `BREP_SPLIT` takes `Split target: Shell` with `Target solid`
and `Target face ids`, which divides only the named faces and leaves the body they belong to whole, and
`Tool type: TwoPointLine`, which divides them along the line between two points drawn in the parameter space
of the surface. `BREP_SPLIT_PREVIEW` runs the same split and writes only the edges it made, as
polylines, instead of the result - an edge the part had before, whole or cut shorter, is left out - which is
what the split dialogs preview. Its `BREP_VISUALIZATION` metadata reports circular-edge closure and
swept angle, and classifies solid cylindrical faces as convex or concave by their material side. It
recomputes the SameParameter and SameRange flags after the
periodic-face split driven by *Split periodic faces*, without which the written brep is invalid
and BRepMesh silently drops whole faces, and it warns on stdout about every face it could not
triangulate. Its `BREP_MESH` reads the optional `transfinite_faces` and `transfinite_angle` entries
of the mesh parameters file, which mesh 4-sided faces with matching segment counts on opposite sides
as structured triangle grids - none, size checked (only where the grid is not much finer than the mesh
size) or all; the edge meshing balances the segment counts of opposite sides - each
one edge or several - across neighbouring faces, so that most of them match.

The `PostBuildEvent` in `PrePoMax.csproj` copies this folder into `$(TargetDir)Meshers\Netgen`.
License texts are added separately from `Licenses/netgen/`.
