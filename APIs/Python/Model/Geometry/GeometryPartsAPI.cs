// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class GeometryPartsAPI : PartsAPI<GeometryPartAPI>
    {
        // Variables
        private readonly GeometryPartService _service;


        // Constructors
        internal GeometryPartsAPI(GeometryPartService service)
            : base("geometry part", service)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Creates a compound part from the named parts using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void create_compound(object part_names)
        {
            _service.CreateCompound(_service.ResolveNames(part_names));
        }

        [PythonAPI("Regenerates the named compound parts using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void regenerate_compound(object part_names)
        {
            _service.RegenerateCompound(_service.ResolveNames(part_names));
        }

        [PythonAPI("Swaps the geometries of two parts using command history.",
                   Arguments = "part_name_1: str, part_name_2: str", Returns = "None")]
        public void swap_geometries(string part_name_1, string part_name_2)
        {
            _service.SwapGeometries(part_name_1, part_name_2);
        }

        [PythonAPI("Converts the named shell parts to solid parts using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void convert_shell_to_solid(object part_names)
        {
            _service.ConvertShellToSolid(_service.ResolveNames(part_names));
        }

        [PythonAPI("Converts the named solid parts to shell parts using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void convert_solid_to_shell(object part_names)
        {
            _service.ConvertSolidToShell(_service.ResolveNames(part_names));
        }

        [PythonAPI("Recomputes the part edges from the angle between the neighbouring faces.",
                   Arguments = "part_names: list[str] or str, edge_angle: float", Returns = "None")]
        public void find_edges_by_angle(object part_names, object edge_angle)
        {
            _service.FindEdgesByAngle(_service.ResolveNames(part_names), edge_angle);
        }

        [PythonAPI("Flips the orientation of the selected faces using command history. The selection uses " +
                   "the 'geometry' view and target. Returns the number of faces flipped on every selected " +
                   "part, so a part that was skipped is visible: it reports zero. Only the faces of CAD " +
                   "shell parts can be flipped.",
                   Arguments = "selection: Selection", Returns = "dict[str, int]")]
        public Dictionary<string, int> flip_face_orientations(SelectionAPI selection)
        {
            return _service.FlipFaceOrientations(selection);
        }

        [PythonAPI("Removes the selected geometry features using command history. The selection uses the " +
                   "'geometry' view and target. Returns the number of faces each selected part actually " +
                   "lost, so a feature the modelling kernel could not close is visible: it reports zero. " +
                   "A blind hole needs its bottom face selected as well as its wall.",
                   Arguments = "selection: Selection", Returns = "dict[str, int]",
                   Example = "removed = pmx.model.geometry.parts.defeature(selection)")]
        public Dictionary<string, int> defeature(SelectionAPI selection)
        {
            return _service.Defeature(selection);
        }

        [PythonAPI("Extracts the selected surfaces into new shell parts using command history. The " +
                   "selection uses the 'geometry' view and target. The surfaces picked on one " +
                   "geometry become one new shell part, sewn together where they touch, and the " +
                   "parts they were taken from are left as they were. A selection spanning three " +
                   "parts gives three, while the surfaces of several sub-parts of one compound give " +
                   "one part named after the compound, because a sub-part carries no geometry of " +
                   "its own and the compound is what all of them are written out of. A non zero " +
                   "offset moves the extracted surfaces along their own normals, outwards when it " +
                   "is positive, so half the wall thickness entered as a negative value on an outer " +
                   "surface gives a mid-surface. Returns the names of the parts that were created.",
                   Arguments = "selection: Selection, offset: float = 0", Returns = "list[str]",
                   Example = "names = pmx.model.geometry.parts.extract_surface(selection, -1.5)")]
        public string[] extract_surface(SelectionAPI selection, object offset = null)
        {
            return _service.ExtractSurface(selection, offset);
        }

        [PythonAPI("Splits a face using two vertices. Both selections use the 'geometry' view and target. " +
                   "Returns the number of faces the part gained, which is zero when the modelling kernel " +
                   "could not make the cut. Only a face of a CAD part can be split.",
                   Arguments = "surface_selection: Selection, vertices_selection: Selection",
                   Returns = "dict[str, int]")]
        public Dictionary<string, int> split_face(SelectionAPI surface_selection, SelectionAPI vertices_selection)
        {
            return _service.SplitFace(surface_selection, vertices_selection);
        }

        [PythonAPI("Flips the surface normals of the named STL parts using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void flip_stl_normals(object part_names)
        {
            _service.FlipStlNormals(_service.ResolveNames(part_names));
        }

        [PythonAPI("Deletes the selected STL part faces using command history. The selection uses the " +
                   "'geometry' view and target. Returns the number of elements deleted from every selected " +
                   "part, so a part that was skipped is visible: it reports zero. Only the faces of STL " +
                   "parts can be deleted.",
                   Arguments = "selection: Selection", Returns = "dict[str, int]")]
        public Dictionary<string, int> delete_stl_faces(SelectionAPI selection)
        {
            return _service.DeleteStlFaces(selection);
        }
        //
        protected override GeometryPartAPI CreateItemAPI(string name)
        {
            return new GeometryPartAPI(name, _service);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "GeometryParts(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
