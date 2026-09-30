# Third-party components

| Component | Where | Licence | Use |
|---|---|---|---|
| TQMesh (Florian Setzwein) | `external/TQMesh`, commit in `external/TQMesh.commit` | MIT (`external/TQMesh/LICENSE.md`) | Comparison only. Locally patched (`external/TQMesh-keep-boundary.patch`) to keep prescribed boundary nodes when `TQMESH_KEEP_BOUNDARY` is set. |
| qmorph (Karl Erik Levik, 2002) | `external/qmorph`, commit in `external/qmorph.commit` | MIT (`external/qmorph/LICENSE`) | Comparison only. `driver/RunQMorph.java` is our headless driver. |
| Gmsh | not bundled; used through its Python API | GPL-2.0-or-later with exceptions | Triangulation and quad recombination in the prototypes. PrePoMax already integrates Gmsh. |
