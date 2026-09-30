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
edge-node files may be omitted. `BREP_DEFEATURE` removes selected 1-based face IDs from a CAD solid
with OpenCASCADE 8.0.1 and checks that the result is still a valid solid. Its `BREP_SPLIT` takes
`Split target: Shell` with `Target solid`
and `Target face ids`, which divides only the named faces and leaves the body they belong to whole, and
`Tool type: TwoPointLine`, which divides them along the line between two points drawn in the parameter space
of the surface. `BREP_SPLIT_PREVIEW` runs the same split and writes only the edges it made, as
polylines, instead of the result - an edge the part had before, whole or cut shorter, is left out - which is
what the split dialogs preview. Its `BREP_VISUALIZATION` metadata reports circular-edge closure and
swept angle, and classifies solid cylindrical faces as convex or concave by their material side. It
recomputes the SameParameter and SameRange flags after the
periodic-face split driven by *Split periodic faces*, without which the written brep is invalid
and BRepMesh silently drops whole faces. It divides a copy in which every face has a surface of its
own, because faces a split cut from one face share its surface and the pcurves of the edges between
them, and dividing one of them left the other without pcurves on those edges. It warns on stdout about
every face it could not
triangulate. Its `BREP_MESH` reads the optional `transfinite_faces` and `transfinite_angle` entries
of the mesh parameters file, which mesh 4-sided faces with matching segment counts on opposite sides
as structured triangle grids - none, size checked (only where the grid is not much finer than the mesh
size) or all; the edge meshing balances the segment counts of opposite sides - each
one edge or several - across neighbouring faces, so that most of them match. Its `BREP_MERGE_SOLIDS`
makes one solid of the named solids of a compound, leaving out the faces they share and healing the edges
the removed faces leave on the outside, and writes every other solid as it was, which is what Fuse Bodies
runs. Its `BREP_REMOVE_FACES` deletes the named faces and leaves the gap open, so a solid that loses a
face comes back as the open shells left of it, and refuses a result whose faces fall into more separate
pieces than before, which is what Delete Faces of a CAD part runs. Its `BREP_OFFSET_FACES`
writes every sewn patch as a shell of its own, so that `BREP_ASSEMBLY_SPLIT_TO_PARTS` writes the patches
that share no edge to separate files. Its `VOL_VOLUME_MESH` meshes a given boundary - triangles and
quadrilaterals in a `.vol` file, one domain per cell - by tetrahedra, standing a pyramid on every
quadrilateral, and keeps the boundary points fixed and first in its output. The sweep meshes the cells
of a compound it cannot reach with it. The pyramids need two changes to Netgen 5.3.1 in `nglib.dll`:
its pyramid rules asked for the sides of a quadrilateral to be identified point pairs, which only a
boundary layer has, and the rules that give a pyramid an apex of its own carried a quality no
quadrilateral ever reached; a mesh without identified pairs now takes those rules. The volume
optimisation no longer collapses an edge of a pyramid, which merged a pyramid's apex into its base. Its smoothing weighs a
pyramid as the four tetrahedra its points span, which keeps the apex from being moved through the base. A pyramid is scaled by its own
quadrilateral rather than by the coarser local mesh size, so that pyramids in an inside corner do not lean
over one another.

The `PostBuildEvent` in `PrePoMax.csproj` copies this folder into `$(TargetDir)Meshers\Netgen`.
License texts are added separately from `Licenses/netgen/`.
