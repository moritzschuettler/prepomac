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
with OpenCASCADE 8.0.1 and checks that the result is still a valid solid. It and `BREP_DEFEATURE_COMPOUND`
cut the splits the defeaturing joins back into the result - the kernel makes every two neighbouring faces on
one surface one face, anywhere in the solid - so the halves of a periodic face and the pieces of a shell split
survive the removal of another feature. Its `BREP_SPLIT` takes
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
triangulate. A face BRepMesh triangulated only in part, because the polylines of its edges cross in its
parameter space, is meshed again with its edges divided finer, so a thin face along long curved edges reaches
PrePoMax whole with all its edges. Its `BREP_MESH` reads the optional `transfinite_faces`, `transfinite_angle` and
`transfinite_filename` entries of the mesh parameters file. The file names the structured faces PrePoMax
finds by the one definition all its meshers follow - every face with its sides as chains of edges, the
numbers of elements set on edges and the names messages use - and Netgen then finds none of its own: it
balances opposite sides to the coarser count without changing a set number - an edge bordering a face not
given keeps at least 0.8 of its own count, the sides it is balanced with raised to it instead, unless the set
numbers leave no room for that - meshes every given face as a
structured grid - a 3-sided one as three blocks of quads around one inner node by the mapping all
PrePoMax meshers share - quads when quad dominated, else triangles - with no fallback, and refuses the meshing,
naming the faces and edges, when the set numbers cannot agree or a grid folds. Without the file the older
detection of 4-sided faces by `transfinite_faces` and `transfinite_angle` applies. Its `BREP_MERGE_SOLIDS`
makes one solid of the named solids of a compound, leaving out the faces they share and healing the edges
the removed faces leave on the outside, and writes every other solid as it was, which is what Fuse Bodies
runs. Its `BREP_REMOVE_FACES` deletes the named faces and leaves the gap open, so a solid that loses a
face comes back as the open shells left of it, and writes the pieces that no longer touch beside the largest
when the faces left fall into more pieces than before, which is what Delete Faces of a CAD part runs. `BREP_REMOVE_COMPOUND_FACES` does the same
for a compound and writes the solids that were kept, each opened solid as a shell of its own beside them and
the list of which solids were opened. Its `BREP_FUSE_FACES` makes the named faces one across the edges
they share where they lie on one surface and bound the same solids, and joins the edges at the vertices of
the edges it took out, which is what Fuse Faces runs; `BREP_REPAIR` sews, fixes, removes small faces and
edges, fuses faces and joins edges on the whole shape as its options say - closing the wire gaps the removed
edges leave only when the shape is invalid without it, since closing them on a valid shape reorients faces far
from any small edge - and refuses a result with other
solids or one that is no longer valid, which is what Repair runs. `BREP_REPAIR_PREVIEW` repairs the same way and
writes only what the repair would take away - the removed and sewn edges as polylines and the removed vertices
as points - which is what the Repair dialog previews. Its `BREP_OFFSET_FACES`
writes every sewn patch as a shell of its own, so that `BREP_ASSEMBLY_SPLIT_TO_PARTS` writes the patches
that share no edge to separate files. Its `VOL_VOLUME_MESH` meshes a given boundary - triangles and
quadrilaterals in a `.vol` file, one domain per cell - by tetrahedra, standing a pyramid on every
quadrilateral, and keeps the boundary points fixed and first in its output. The sweep meshes the cells
of a compound it cannot reach with it. The pyramids need two changes to Netgen 5.3.1 in `nglib.dll`:
its pyramid rules asked for the sides of a quadrilateral to be identified point pairs, which only a
boundary layer has, and the rules that give a pyramid an apex of its own carried a quality no
quadrilateral ever reached; a mesh without identified pairs now takes those rules. The volume
optimisation no longer collapses an edge of a pyramid, which merged a pyramid's apex into its base, and its edge
swapping leaves an edge whose ring of elements does not close, which read unset points and crashed. Its smoothing weighs a
pyramid as the four tetrahedra its points span, which keeps the apex from being moved through the base. A pyramid is scaled by its own
quadrilateral rather than by the coarser local mesh size, so that pyramids in an inside corner do not lean
over one another, and a pyramid whose quadrilateral closes an angle sharper than a right one with another
quadrilateral is lowered, so that the pyramid of the other face still fits. Its `BREP_COMPOUND` writes the
solid where parts overlap once - the general fuse lists it in the image of every part it lies in - and
writes `<out.brep>.origins` beside the result, one line per solid listing every input solid it lies in,
which is how a compound with a split part among its parts gathers its solids.

The `PostBuildEvent` in `PrePoMax.csproj` copies this folder into `$(TargetDir)Meshers\Netgen`.
License texts are added separately from `Licenses/netgen/`.
