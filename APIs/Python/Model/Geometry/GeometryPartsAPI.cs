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
                   "came from, and a solid where the parts overlap is a sub-part of its own named Overlap. " +
                   "At least two parts are needed, all of them CAD or all of them STL parts of one part type.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void add_compound(object part_names)
        {
            _service.CreateCompound(_service.ResolveNames(part_names));
        }

        [PythonAPI("Regenerates the named compound parts using command history. A part that is not a " +
                   "compound, or a compound a part it was made from is missing for, is refused.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void regenerate_compound(object part_names)
        {
            _service.RegenerateCompound(_service.ResolveNames(part_names));
        }

        [PythonAPI("Swaps the geometries of two parts using command history.",
                   Arguments = "part_name_1: str, part_name_2: str", Returns = "None")]
        public void swap_geometries(object part_name_1, object part_name_2)
        {
            string partName1 = PythonValueConverter.ToText(part_name_1, "part_name_1", false);
            string partName2 = PythonValueConverter.ToText(part_name_2, "part_name_2", false);
            _service.SwapGeometries(partName1, partName2);
        }

        [PythonAPI("Converts the named CAD shell parts to solid parts using command history. Any other part " +
                   "is refused.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void convert_shell_to_solid(object part_names)
        {
            _service.ConvertShellToSolid(_service.ResolveNames(part_names));
        }

        [PythonAPI("Converts the named CAD solid parts or compound parts to shell parts using command " +
                   "history. Any other part, a sub-part of a compound included, is refused.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void convert_solid_to_shell(object part_names)
        {
            _service.ConvertSolidToShell(_service.ResolveNames(part_names));
        }

        [PythonAPI("Recomputes the part edges from the angle between the neighbouring faces, in degrees from 0 to 90.",
                   Arguments = "part_names: list[str] or str, edge_angle_deg: float", Returns = "None")]
        public void find_edges_by_angle(object part_names, object edge_angle_deg)
        {
            _service.FindEdgesByAngle(_service.ResolveNames(part_names), edge_angle_deg);
        }

        [PythonAPI("Flips the orientation of the selected faces using command history. The selection uses " +
                   "the 'geometry' view and the 'geometry' or 'geometry_surfaces' target. Returns the number " +
                   "of faces flipped on every selected part, so a part that was skipped is visible: it reports " +
                   "zero. Only the faces of CAD " +
                   "shell parts can be flipped.",
                   Arguments = "selection: Selection", Returns = "dict[str, int]")]
        public Dictionary<string, int> flip_face_orientations(object selection)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            return _service.FlipFaceOrientations(selectionValue);
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
        public Dictionary<string, int> split_solid(object selection, object tool,
                                                   object fuzzy_tolerance = null)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            SplitToolAPI toolValue = PythonValueConverter.ToApiObject<SplitToolAPI>(tool, "tool", false);
            return _service.SplitGeometry(selectionValue, toolValue, fuzzy_tolerance, SplitTypeEnum.Solid);
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
        public Dictionary<string, int> split_face(object selection, object tool,
                                                   object fuzzy_tolerance = null)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            SplitToolAPI toolValue = PythonValueConverter.ToApiObject<SplitToolAPI>(tool, "tool", false);
            return _service.SplitGeometry(selectionValue, toolValue, fuzzy_tolerance, SplitTypeEnum.Shell);
        }

        [PythonAPI("Renames a sub-part of a compound part made by split_solid, the way renaming it in the " +
                   "tree does. The sub-part is the solid the cells were cut from, which the part lists in " +
                   "split_sub_part_names; its cells are renamed after it, and so is the mesh part it was " +
                   "meshed into. old_name is matched regardless of case.",
                   Arguments = "part_name: str, old_name: str, new_name: str", Returns = "None",
                   Example = "pmx.model.geometry.parts.rename_split_sub_part('Compound_part-1', 'Solid_part-1', 'Web')")]
        public void rename_split_sub_part(object part_name, object old_name, object new_name)
        {
            string partName = PythonValueConverter.ToText(part_name, "part_name", false);
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string newName = PythonValueConverter.ToText(new_name, "new_name", false);
            _service.RenameSplitSubPart(partName, oldName, newName);
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
        public string group_split_cells(object part_name, object cell_names, object sub_part_name = null)
        {
            string partName = PythonValueConverter.ToText(part_name, "part_name", false);
            string subPartName = PythonValueConverter.ToText(sub_part_name, "sub_part_name", true);
            return _service.GroupSplitCells(partName, cell_names, subPartName);
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
        public string fuse_bodies(object part_name, object body_names)
        {
            string partName = PythonValueConverter.ToText(part_name, "part_name", false);
            return _service.FuseBodies(partName, body_names);
        }

        [PythonAPI("Removes the selected geometry features using command history. The selection uses the " +
                   "'geometry' view and the 'geometry' or 'geometry_surfaces' target. Returns the number of " +
                   "faces each selected part actually " +
                   "lost, so a feature the modelling kernel could not close is visible: it reports zero. " +
                   "A blind hole needs its bottom face selected as well as its wall.",
                   Arguments = "selection: Selection", Returns = "dict[str, int]",
                   Example = "removed = pmx.model.geometry.parts.defeature(selection)")]
        public Dictionary<string, int> defeature(object selection)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            return _service.Defeature(selectionValue);
        }

        [PythonAPI("Offsets the selected surfaces into new shell parts using command history. The " +
                   "selection uses the 'geometry' view and the 'geometry' or 'geometry_surfaces' " +
                   "target. The surfaces picked on one " +
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
        public StringListAPI offset_face(object selection, object offset = null)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            return new StringListAPI(_service.OffsetFace(selectionValue, offset));
        }

        [PythonAPI("Deletes the selected CAD part faces using command history and leaves the gap open. " +
                   "The selection uses the 'geometry' view and the 'geometry' or 'geometry_surfaces' " +
                   "target. A solid that loses a face becomes " +
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
        public Dictionary<string, int> delete_cad_faces(object selection)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            return _service.DeleteCadFaces(selectionValue);
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
        public Dictionary<string, int> fuse_faces(object selection)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            return _service.FuseFaces(selectionValue);
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
        public Dictionary<string, int> repair(object part_names, object sew_faces = null, object fix_shape = null,
                                              object remove_small_faces = null, object remove_small_edges = null,
                                              object fuse_faces = null, object join_edges = null,
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
        public Dictionary<string, RepairPreviewAPI> repair_preview(object part_names, object sew_faces = null,
                                                                   object fix_shape = null,
                                                                   object remove_small_faces = null,
                                                                   object remove_small_edges = null,
                                                                   object fuse_faces = null, object join_edges = null,
                                                                   object tolerance = null)
        {
            GeometryRepair geometryRepair = GetGeometryRepair(sew_faces, fix_shape, remove_small_faces,
                                                              remove_small_edges, fuse_faces, join_edges);
            return _service.RepairPreview(_service.ResolveNames(part_names), geometryRepair, tolerance);
        }
        private static GeometryRepair GetGeometryRepair(object sewFaces, object fixShape, object removeSmallFaces,
                                                        object removeSmallEdges, object fuseFaces, object joinEdges)
        {
            GeometryRepair geometryRepair = new GeometryRepair();
            geometryRepair.SewFaces = PythonValueConverter.ToBoolOrDefault(sewFaces, true, "sew_faces");
            geometryRepair.FixShape = PythonValueConverter.ToBoolOrDefault(fixShape, true, "fix_shape");
            geometryRepair.RemoveSmallFaces =
                PythonValueConverter.ToBoolOrDefault(removeSmallFaces, true, "remove_small_faces");
            geometryRepair.RemoveSmallEdges =
                PythonValueConverter.ToBoolOrDefault(removeSmallEdges, true, "remove_small_edges");
            geometryRepair.FuseFaces = PythonValueConverter.ToBoolOrDefault(fuseFaces, true, "fuse_faces");
            geometryRepair.JoinEdges = PythonValueConverter.ToBoolOrDefault(joinEdges, true, "join_edges");
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
        public Dictionary<string, int> split_periodic_faces(object part_names, object number_of_faces = null)
        {
            int numberOfFaces = PythonValueConverter.ToIntOrDefault(number_of_faces, 2, "number_of_faces");
            return _service.SplitPeriodicFaces(_service.ResolveNames(part_names), numberOfFaces);
        }

        [PythonAPI("Flips the surface normals of the named STL parts using command history. A CAD part is " +
                   "refused.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void flip_stl_normals(object part_names)
        {
            _service.FlipStlNormals(_service.ResolveNames(part_names));
        }

        [PythonAPI("Deletes the selected STL part faces using command history. The selection uses the " +
                   "'geometry' view and the 'geometry' or 'geometry_surfaces' target. Returns the number of " +
                   "elements deleted from every selected " +
                   "part, so a part that was skipped is visible: it reports zero. Only the faces of STL " +
                   "parts can be deleted.",
                   Arguments = "selection: Selection", Returns = "dict[str, int]")]
        public Dictionary<string, int> delete_stl_faces(object selection)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            return _service.DeleteStlFaces(selectionValue);
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
