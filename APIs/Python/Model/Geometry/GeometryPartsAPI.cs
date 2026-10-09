// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
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
        [PythonAPI("Adds a compound part made from the named parts using command history. When one of them is a " +
                   "compound part made by split_solid, the solids are gathered into sub-parts after the parts they " +
                   "came from, and a solid where the parts overlap is a sub-part of its own named Overlap.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void add_compound(object part_names)
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
                   Arguments = "part_names: list[str] or str, edge_angle_deg: float", Returns = "None")]
        public void find_edges_by_angle(object part_names, object edge_angle_deg)
        {
            _service.FindEdgesByAngle(_service.ResolveNames(part_names), edge_angle_deg);
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

        [PythonAPI("Splits the selected CAD parts with a splitting tool made by pmx.create.split_tool_*, the way " +
                   "the Split Solid dialog does. The selection uses the 'geometry' view and the 'parts' target; " +
                   "one sub-part of a compound names that solid only, and the compound names all of them. A " +
                   "part the tool cuts in two is replaced by a compound part of the pieces, so its name " +
                   "changes. Returns the number of faces every selected part gained, which is zero for a part " +
                   "the tool missed. The fuzzy tolerance is how far apart the modelling kernel still takes " +
                   "geometry as coincident; leave it at zero unless an imported geometry is too inaccurate to " +
                   "be split.",
                   Arguments = "selection: Selection, tool: SplitTool, fuzzy_tolerance: float = 0",
                   Returns = "dict[str, int]",
                   Example = "tool = pmx.create.split_tool_plane_by_point_and_normal([30, 20, 10], [0, 0, 1])\n" +
                             "added = pmx.model.geometry.parts.split_solid(selection, tool)")]
        public Dictionary<string, int> split_solid(SelectionAPI selection, SplitToolAPI tool,
                                                   object fuzzy_tolerance = null)
        {
            return _service.SplitGeometry(selection, tool, fuzzy_tolerance, SplitTypeEnum.Solid);
        }

        [PythonAPI("Splits the selected surfaces of CAD parts with a splitting tool made by " +
                   "pmx.create.split_tool_*, the way the Split Face dialog does. The selection uses the " +
                   "'geometry' view and the 'geometry_surfaces' target. Only the selected surfaces are " +
                   "divided; the part keeps its name and stays the kind of body it is, and a part whose " +
                   "divided surfaces would not give back the same body is left as it was. Returns the number " +
                   "of faces every part of the selection gained, which is zero for a part that was not split. " +
                   "The fuzzy tolerance is as for split_solid.",
                   Arguments = "selection: Selection, tool: SplitTool, fuzzy_tolerance: float = 0",
                   Returns = "dict[str, int]",
                   Example = "tool = pmx.create.split_tool_line_by_two_points(splitter=vertices)\n" +
                             "added = pmx.model.geometry.parts.split_face(surfaces, tool)")]
        public Dictionary<string, int> split_face(SelectionAPI selection, SplitToolAPI tool,
                                                   object fuzzy_tolerance = null)
        {
            return _service.SplitGeometry(selection, tool, fuzzy_tolerance, SplitTypeEnum.Shell);
        }

        [PythonAPI("Renames a sub-part of a compound part made by split_solid, the way renaming it in the " +
                   "tree does. The sub-part is the solid the cells were cut from, which the part lists in " +
                   "split_sub_part_names; its cells are renamed after it, and so is the mesh part it was " +
                   "meshed into. old_name is matched regardless of case.",
                   Arguments = "part_name: str, old_name: str, new_name: str", Returns = "None",
                   Example = "pmx.model.geometry.parts.rename_split_sub_part('Compound_part-1', 'Solid_part-1', 'Web')")]
        public void rename_split_sub_part(string part_name, string old_name, string new_name)
        {
            _service.RenameSplitSubPart(part_name, old_name, new_name);
        }

        [PythonAPI("Puts the named cells of a compound part made by split_solid under one sub-part, the way " +
                   "Group Cells in the tree does, so that they are meshed into one mesh part when the cells are " +
                   "merged, which is the default. The geometry is not changed. The sub-part is the one named, " +
                   "joined if the compound has it and created otherwise, and a new one is listed last; " +
                   "without a name it is the largest sub-part whose cells are all among the named ones, or " +
                   "else a new sub-part named after the one the first cell belongs to. A sub-part left " +
                   "without cells is gone. Grouping cells that " +
                   "already belong to that sub-part, or all the cells of one sub-part under a new name, is " +
                   "refused - rename_split_sub_part renames, and so is a cell named twice in any spelling. " +
                   "Returns the name of the sub-part.",
                   Arguments = "part_name: str, cell_names: list[str] or str, sub_part_name: str = None",
                   Returns = "str",
                   Example = "cells = compound.get_split_cell_names('Solid_part-1')\n" +
                             "pmx.model.geometry.parts.group_split_cells(compound.name, cells[:2], 'Steel')")]
        public string group_split_cells(string part_name, object cell_names, string sub_part_name = null)
        {
            return _service.GroupSplitCells(part_name, cell_names, sub_part_name);
        }

        [PythonAPI("Makes one solid of the named bodies of a compound part, the way Fuse Bodies in the tree " +
                   "does. A body is a sub-part of the compound; of a compound made by split_solid it is a cell, or " +
                   "a sub-part, which stands for all of its cells. The faces the solids share are taken out, and " +
                   "the edges left on their outside are healed where the faces on both sides lie on one surface, " +
                   "so cells come back as the solid the splits cut them from. The solids must touch over faces, " +
                   "and there must be at least two of them. The compound keeps its name and id, and the sub-parts " +
                   "that were not fused keep theirs; the fused solid takes the name and id of the first sub-part " +
                   "fused. Cells of several sub-parts of a split compound are first grouped under one - the " +
                   "largest of them whose cells are all fused, or else the one of the first cell - and a sub-part " +
                   "left without cells is gone. Fusing the whole compound gives back a plain part with the name " +
                   "and id of the sub-part it ends in. Returns the name of the fused solid, or of that part.",
                   Arguments = "part_name: str, body_names: list[str] or str", Returns = "str",
                   Example = "pmx.model.geometry.parts.fuse_bodies(compound.name, ['Solid_part-1', 'Solid_part-2'])")]
        public string fuse_bodies(string part_name, object body_names)
        {
            return _service.FuseBodies(part_name, body_names);
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

        [PythonAPI("Offsets the selected surfaces into new shell parts using command history. The " +
                   "selection uses the 'geometry' view and target. The surfaces picked on one " +
                   "geometry become one new shell part, sewn together where they touch, and the " +
                   "parts they were taken from are left as they were. A selection spanning three " +
                   "parts gives three, while the surfaces of several sub-parts of one compound give " +
                   "one part named after the compound, because a sub-part carries no geometry of " +
                   "its own and the compound is what all of them are written out of. A non zero " +
                   "offset moves the new surfaces along their own normals, outwards when it " +
                   "is positive, so half the wall thickness entered as a negative value on an outer " +
                   "surface gives a mid-surface. Returns the names of the parts that were created.",
                   Arguments = "selection: Selection, offset: float = 0", Returns = "StringList",
                   Example = "names = pmx.model.geometry.parts.offset_face(selection, -1.5)")]
        public StringListAPI offset_face(SelectionAPI selection, object offset = null)
        {
            return new StringListAPI(_service.OffsetFace(selection, offset));
        }

        [PythonAPI("Deletes the selected CAD part faces using command history and leaves the gap open. " +
                   "The selection uses the 'geometry' view and target. A solid that loses a face becomes " +
                   "a shell part; the part keeps its name and part id either way. When the faces it keeps " +
                   "fall apart into pieces that no longer touch, the largest piece stays the part and every " +
                   "other one becomes a shell part of its own, and a part or a sub-part whose last face is " +
                   "deleted is deleted with it. A sub-part or split cell of a compound that loses a face " +
                   "leaves the compound as a shell part with its own name and part id, and a face two of them " +
                   "share is deleted from both; the solids that lost nothing stay the compound, and one solid " +
                   "left becomes a " +
                   "plain part. Returns the number of faces deleted from every selected or opened part, so a " +
                   "part that was left alone reports zero.",
                   Arguments = "selection: Selection", Returns = "dict[str, int]",
                   Example = "deleted = pmx.model.geometry.parts.delete_cad_faces(selection)")]
        public Dictionary<string, int> delete_cad_faces(SelectionAPI selection)
        {
            return _service.DeleteCadFaces(selection);
        }

        [PythonAPI("Fuses the selected CAD part faces into one using command history, the way Fuse Faces in the " +
                   "CAD Part menu does. The selection uses the 'geometry' view and the 'geometry' or " +
                   "'geometry_surfaces' target. Faces are fused across an edge they share where they lie on one " +
                   "surface, which gives back a face a split divided, and " +
                   "the edges left on the outside are joined where the cut left a point on them. The shape does " +
                   "not change, so a part keeps its name, its id and its bodies. Faces of the sub-parts of a " +
                   "compound are fused on the compound as a whole, and only faces that bound the same solids are " +
                   "fused, so the solids still meet. A fused face that closes on itself is divided again as every " +
                   "closed face is. Returns the number of faces every part lost, so a part whose faces could not " +
                   "be fused reports zero.",
                   Arguments = "selection: Selection", Returns = "dict[str, int]",
                   Example = "fused = pmx.model.geometry.parts.fuse_faces(selection)")]
        public Dictionary<string, int> fuse_faces(SelectionAPI selection)
        {
            return _service.FuseFaces(selection);
        }

        [PythonAPI("Repairs the whole of the named CAD parts using command history, the way Repair in the CAD " +
                   "Part menu does. The repairs run in this order: sew_faces sews loose faces and shells of parts " +
                   "without solids, fix_shape fixes edge tolerances, open wires, missing curves on surfaces and " +
                   "orientations, remove_small_faces removes faces narrower than the tolerance, remove_small_edges " +
                   "removes edges shorter than it, fuse_faces fuses neighbouring faces that lie on one surface and " +
                   "bound the same solids - which also fuses faces a split divided - and join_edges joins edges " +
                   "that lie on one curve. A sub-part is repaired with its compound. A repair that would change the " +
                   "number of solids or break a valid shape leaves the part as it was. The tolerance defaults to " +
                   "one ten-thousandth of the diagonal of each part. Returns the number of faces every part lost.",
                   Arguments = "part_names: list[str] or str, sew_faces: bool = True, fix_shape: bool = True, " +
                               "remove_small_faces: bool = True, remove_small_edges: bool = True, " +
                               "fuse_faces: bool = True, join_edges: bool = True, tolerance: float = None",
                   Returns = "dict[str, int]",
                   Example = "removed = pmx.model.geometry.parts.repair('Solid_part-1', join_edges=False)")]
        public Dictionary<string, int> repair(object part_names, bool sew_faces = true, bool fix_shape = true,
                                              bool remove_small_faces = true, bool remove_small_edges = true,
                                              bool fuse_faces = true, bool join_edges = true,
                                              object tolerance = null)
        {
            GeometryRepair geometryRepair = GetGeometryRepair(sew_faces, fix_shape, remove_small_faces,
                                                              remove_small_edges, fuse_faces, join_edges);
            return _service.Repair(_service.ResolveNames(part_names), geometryRepair, tolerance);
        }

        [PythonAPI("Runs repair on a copy of the named CAD parts and returns what it would take away, without " +
                   "changing anything or recording a command, the way Preview in the Repair dialog does. It takes " +
                   "the arguments repair takes. A sub-part is previewed with its compound, so the result is keyed " +
                   "by the names of the parts the repair would run on. Every RepairPreview lists the edges the " +
                   "repair would remove or sew, each as the points sampled on it, and the vertices it would remove; " +
                   "refused is True when the repair would leave the part as it was.",
                   Arguments = "part_names: list[str] or str, sew_faces: bool = True, fix_shape: bool = True, " +
                               "remove_small_faces: bool = True, remove_small_edges: bool = True, " +
                               "fuse_faces: bool = True, join_edges: bool = True, tolerance: float = None",
                   Returns = "dict[str, RepairPreview]",
                   Example = "preview = pmx.model.geometry.parts.repair_preview('Solid_part-1', tolerance=0.01)\n" +
                             "print(preview['Solid_part-1'].edge_count)")]
        public Dictionary<string, RepairPreviewAPI> repair_preview(object part_names, bool sew_faces = true,
                                                                   bool fix_shape = true,
                                                                   bool remove_small_faces = true,
                                                                   bool remove_small_edges = true,
                                                                   bool fuse_faces = true, bool join_edges = true,
                                                                   object tolerance = null)
        {
            GeometryRepair geometryRepair = GetGeometryRepair(sew_faces, fix_shape, remove_small_faces,
                                                              remove_small_edges, fuse_faces, join_edges);
            return _service.RepairPreview(_service.ResolveNames(part_names), geometryRepair, tolerance);
        }
        private static GeometryRepair GetGeometryRepair(bool sewFaces, bool fixShape, bool removeSmallFaces,
                                                        bool removeSmallEdges, bool fuseFaces, bool joinEdges)
        {
            GeometryRepair geometryRepair = new GeometryRepair();
            geometryRepair.SewFaces = sewFaces;
            geometryRepair.FixShape = fixShape;
            geometryRepair.RemoveSmallFaces = removeSmallFaces;
            geometryRepair.RemoveSmallEdges = removeSmallEdges;
            geometryRepair.FuseFaces = fuseFaces;
            geometryRepair.JoinEdges = joinEdges;
            return geometryRepair;
        }

        [PythonAPI("Splits every periodic face of the named CAD parts - a face that closes on itself, such as the " +
                   "side of a cylinder with a single seam - into number_of_faces faces using command history, the " +
                   "way Split Periodic Faces in the CAD Part menu does. A sweep meshes such a face around its seam, " +
                   "so the split is only needed where the pieces are wanted as faces of their own. A sub-part is " +
                   "split with its compound, so the faces its solids share stay shared. Returns the number of faces " +
                   "every part gained.",
                   Arguments = "part_names: list[str] or str, number_of_faces: int = 2",
                   Returns = "dict[str, int]",
                   Example = "added = pmx.model.geometry.parts.split_periodic_faces('Solid_part-1')")]
        public Dictionary<string, int> split_periodic_faces(object part_names, int number_of_faces = 2)
        {
            return _service.SplitPeriodicFaces(_service.ResolveNames(part_names), number_of_faces);
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
