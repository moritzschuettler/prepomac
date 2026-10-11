// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class MeshSetupItemsAPI : ModelItemsAPI<MeshSetupItem, MeshSetupItemAPI>
    {
        // Variables
        // The Gmsh settings the property grid offers, split the way the mesher types share them
        private const string MeshingParameterArguments =
            "max_size: float = None, min_size: float = None, grading: float = None, elements_per_edge: float = None, " +
            "elements_per_curve: float = None, second_order: bool = None, midside_nodes_on_geometry: bool = None, " +
            "relative_size: bool = None, factor_max: float = None, factor_min: float = None, " +
            "split_compound_mesh: bool = None, merge_compound_parts: bool = None, merge_cells: bool = None";
        private const string NetgenMeshArguments =
            "number_of_optimize_steps_2d: int = None, number_of_optimize_steps_3d: int = None, " +
            "quad_dominated: bool = None, transfinite_three_sided: str = None, transfinite_four_sided: bool = None, " +
            "transfinite_angle_deg: float = None";
        // The Gmsh item and its dialog take any number for these two, so a script may give the same, and the
        // description says where a value stops making a difference
        private const string GmshValueRanges =
            "recombine_min_quality and transfinite_angle_deg take any finite number, as the dialog does. A " +
            "face keeps its transfinite constraint when every corner angle lies between 180 - " +
            "transfinite_angle_deg and transfinite_angle_deg, so 180 or more keeps it on every face and a " +
            "smaller angle on fewer faces.";
        // The dialog shows the minimum quality only while the item recombines and the angle only while it
        // structures a face, and the mesher uses them only then
        private const string GmshUnusedValues =
            "recombine_min_quality is used only while algorithm_recombine is not 'None', and " +
            "transfinite_angle_deg only while transfinite_three_sided is not 'No' or transfinite_four_sided is " +
            "True; judged on the values the item ends up with, an unused one is refused naming the argument " +
            "that would use it and is left out of get_settings.";
        private const string ShellMmgArguments =
            "relative_size: bool = None, relative_hausdorff: float = None, hausdorff: float = None, " +
            "keep_model_edges: bool = None";
        private const string GmshRecombineArguments =
            "algorithm_recombine: str = None, transfinite_three_sided: str = None, " +
            "transfinite_four_sided: bool = None";
        private readonly MeshSetupItemService _service;


        // Constructors
        internal MeshSetupItemsAPI(MeshSetupItemService service)
            : base("mesh setup item")
        {
            _service = service;
        }


        // Static methods
        // The arguments of the replace_* method of the item type, each with the stored value it sets, which is how
        // the apply methods below write them. Nothing else is read, so the settings go back into the replace_*
        // method of the item
        internal static Dictionary<string, string[]> GetArgumentValueNames(MeshSetupItem item)
        {
            Dictionary<string, string[]> names = new Dictionary<string, string[]>();
            if (item is MeshingParameters)
            {
                // Gmsh, Mmg and the local mesh size limits use the sizes in either mode, and only Netgen sizes the
                // mesh by the factors when the relative switch is on, so the factors alone depend on it
                bool relativeSizeUsed = IsRelativeSizeUsed((MeshingParameters)item);
                names.Add("max_size", new string[] { "max_h" });
                names.Add("min_size", new string[] { "min_h" });
                ModelItemValueReader.AddArguments(names, "grading", "elements_per_edge", "elements_per_curve",
                                                  "second_order");
                if (((MeshingParameters)item).SecondOrder)
                    ModelItemValueReader.AddArguments(names, "midside_nodes_on_geometry");
                ModelItemValueReader.AddArguments(names, "relative_size");
                if (relativeSizeUsed) ModelItemValueReader.AddArguments(names, "factor_max", "factor_min");
                ModelItemValueReader.AddArguments(names, "split_compound_mesh", "merge_compound_parts",
                                                  "merge_cells");
            }
            else if (item is LocalMeshSize)
            {
                // The argument that is given decides the type, and only one of the two is accepted at a time
                if (((LocalMeshSize)item).Type == LocalMeshSizeTypeEnum.ElementSize)
                    names.Add("element_size", new string[] { "mesh_size" });
                else ModelItemValueReader.AddArguments(names, "number_of_elements");
            }
            else if (item is ShellMmg)
            {
                // Only the distance the item uses is read, as the dialog shows only that one
                ModelItemValueReader.AddArguments(names, "relative_size",
                                                  ((ShellMmg)item).RelativeSize ? "relative_hausdorff" : "hausdorff",
                                                  "keep_model_edges");
            }
            else if (item is ThickenShellMesh)
                ModelItemValueReader.AddArguments(names, "thickness", "number_of_layers", "offset", "keep_model_edges");
            else if (item is NetgenMesh)
            {
                names.Add("number_of_optimize_steps_2d", new string[] { "optimize_steps2_d" });
                names.Add("number_of_optimize_steps_3d", new string[] { "optimize_steps3_d" });
                ModelItemValueReader.AddArguments(names, "quad_dominated", "transfinite_three_sided",
                                                  "transfinite_four_sided");
                NetgenMesh netgenMesh = (NetgenMesh)item;
                if (netgenMesh.IsTransfiniteThreeSided || netgenMesh.TransfiniteFourSided)
                    ModelItemValueReader.AddArguments(names, "transfinite_angle_deg");
            }
            else if (item is SweepMesh)
            {
                ModelItemValueReader.AddArguments(names, "number_of_global_smooth_steps");
                // The 2D algorithm of a sweep may also be QuadWild, so it is stored apart from the gmsh one
                names.Add("algorithm_2d", new string[] { "sweep_algorithm_mesh2_d" });
                // gmsh meshes the walls of a sweep whose sources QuadWild meshes, so its recombination and
                // optimisation apply with either source mesher
                AddGmshArguments(names, (SweepMesh)item, "optimize_first_order_shell");
                names.Add("elements_per_layer", new string[] { "number_of_elements_per_layer" });
                if (((SweepMesh)item).SourceMesher == SweepSourceMesherEnum.QuadWild)
                    ModelItemValueReader.AddArguments(names, "quad_wild_flow_profile", "quad_wild_alpha",
                                                      "quad_wild_smooth");
                ModelItemValueReader.AddArguments(names, "symmetry");
            }
            else if (item is TransfiniteMesh)
            {
                // add_transfinite_mesh takes recombination as a switch, which GetSettings reads
                ModelItemValueReader.AddArguments(names, "allow_pyramid_elements");
                names.Add("algorithm_2d", new string[] { "algorithm_mesh2_d" });
                names.Add("algorithm_3d", new string[] { "algorithm_mesh3_d" });
                ModelItemValueReader.AddArguments(names, "transfinite_angle_deg", "transfinite_three_sided");
            }
            else if (item is TetrahedralGmsh)
            {
                names.Add("algorithm_2d", new string[] { "algorithm_mesh2_d" });
                names.Add("algorithm_3d", new string[] { "algorithm_mesh3_d" });
                AddGmshArguments(names, (GmshSetupItem)item, "optimize_first_order_solid", "optimize_high_order");
            }
            else if (item is ShellGmsh)
            {
                // A shell has no volume, and its dialog no 3D algorithm
                names.Add("algorithm_2d", new string[] { "algorithm_mesh2_d" });
                AddGmshArguments(names, (GmshSetupItem)item, "optimize_first_order_shell");
            }
            else if (item is ShellQuadWild)
            {
                // A preview shows the triangulation without running QuadWild, so the dialog hides the settings of
                // the quadrangulation then
                ShellQuadWild shellQuadWild = (ShellQuadWild)item;
                bool quadrangulated = !shellQuadWild.Preview;
                ModelItemValueReader.AddArguments(names, "preview");
                if (quadrangulated) ModelItemValueReader.AddArguments(names, "smooth", "alpha");
                ModelItemValueReader.AddArguments(names, "triangulation_mesher");
                if (quadrangulated) ModelItemValueReader.AddArguments(names, "flow_profile");
                ModelItemValueReader.AddArguments(names, "keep_files_on_failure", "debug_visualization",
                                                  "old_algorithm", "pin_edge_nodes", "edge_rotations",
                                                  "singularity_position");
                names.Add("number_of_search_rounds", new string[] { "count_search" });
                if (quadrangulated)
                {
                    ModelItemValueReader.AddArguments(names, "boundary_layer", "transfinite_three_sided",
                                                      "transfinite_four_sided");
                    if (shellQuadWild.IsTransfiniteThreeSided || shellQuadWild.TransfiniteFourSided)
                        ModelItemValueReader.AddArguments(names, "transfinite_angle_deg");
                }
            }
            //
            return names;
        }
        // The recombination and transfinite settings every gmsh item but the transfinite one takes, followed by
        // the optimization settings of the item type. The minimum quality is read only while the item recombines
        // and the angle only while it structures a face, as the dialog shows them only then
        private static void AddGmshArguments(Dictionary<string, string[]> names, GmshSetupItem item,
                                             params string[] optimizeArguments)
        {
            if (item.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None)
                ModelItemValueReader.AddArguments(names, "recombine_min_quality");
            ModelItemValueReader.AddArguments(names, "algorithm_recombine", "transfinite_three_sided",
                                              "transfinite_four_sided");
            if (item.IsTransfiniteThreeSided || item.TransfiniteFourSided)
                ModelItemValueReader.AddArguments(names, "transfinite_angle_deg");
            ModelItemValueReader.AddArguments(names, optimizeArguments);
        }
        // A value an argument sets in another form is reported in the form that argument takes, so the settings
        // go back into the replace_* method of the item
        internal static Dictionary<string, object> GetSettings(MeshSetupItem item)
        {
            Dictionary<string, object> values = ModelItemValueReader.GetValues(item, GetArgumentValueNames(item));
            // add_transfinite_mesh takes recombination as a switch, which picks the algorithm itself
            TransfiniteMesh transfiniteMesh = item as TransfiniteMesh;
            if (transfiniteMesh != null)
                values["recombine"] = transfiniteMesh.AlgorithmRecombine != GmshAlgorithmRecombineEnum.None;
            // The basic view of the dialog meshes by the absolute sizes whatever relative switch it keeps hidden
            MeshingParameters meshingParameters = item as MeshingParameters;
            if (meshingParameters != null) values["relative_size"] = IsRelativeSizeUsed(meshingParameters);
            //
            return values;
        }
        // The mesh is sized by the factors only when the relative switch is on in the advanced view of the dialog
        internal static bool IsRelativeSizeUsed(MeshingParameters item)
        {
            return item.RelativeSize && item.AdvancedView;
        }


        // Methods
        [PythonAPI("Adds meshing parameters for the selected geometry. Every parameter that is not given " +
                   "keeps the application default. max_size and min_size are at least 0, factor_max, factor_min " +
                   "and grading larger than 0 and at most 1, elements_per_edge and elements_per_curve " +
                   "larger than 0; a minimum larger than the maximum the item ends up with is refused naming both, " +
                   "and split_compound_mesh and merge_compound_parts cannot both be True. relative_size=True sizes " +
                   "the Netgen mesh by factor_max and factor_min times the bounding box diagonal of each part, and " +
                   "turns on the advanced view of the dialog, which offers the switch; relative_size=False sizes " +
                   "it by max_size and min_size. The factors are refused with relative_size=False and left out " +
                   "of get_settings then. Gmsh, Mmg and the limits of the local mesh sizes always use max_size " +
                   "and min_size, so those two are taken and read back in either mode. midside_nodes_on_geometry " +
                   "is used only with second_order=True, as the dialog shows it, so it is refused and left out " +
                   "of get_settings otherwise.",
                   Arguments = "name: str, selection: Selection, " + MeshingParameterArguments,
                   Returns = "MeshSetupItem",
                   Example = "pmx.model.mesh.setup_items.add_meshing_parameters('MP-1', selection, max_size=5.0)")]
        public MeshSetupItemAPI add_meshing_parameters(object name, object selection, object max_size = null,
                                                       object min_size = null, object grading = null,
                                                       object elements_per_edge = null,
                                                       object elements_per_curve = null, object second_order = null,
                                                       object midside_nodes_on_geometry = null,
                                                       object relative_size = null, object factor_max = null,
                                                       object factor_min = null,
                                                       object split_compound_mesh = null,
                                                       object merge_compound_parts = null,
                                                       object merge_cells = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            _service.ValidateNewName(nameText);
            MeshingParameters item = _service.CreateDefaultMeshingParameters(nameText);
            _service.PrepareItem(item, nameText, selectionValue);
            _service.ApplyPartDefaults(item);
            ApplyMeshingParameters(item, max_size, min_size, grading, elements_per_edge,
                                   elements_per_curve, second_order, midside_nodes_on_geometry,
                                   relative_size, factor_max, factor_min, split_compound_mesh,
                                   merge_compound_parts, merge_cells);
            _service.AddPreparedItem(item);
            return this[nameText];
        }

        [PythonAPI("Replaces meshing parameters, keeping every value that is not given. A new selection given " +
                   "without any of the values rebuilds the sizes from the part defaults of the new selection, as " +
                   "the dialog does, when they still equal the part defaults of the region the item had; sizes " +
                   "that differ from those were set earlier and are kept.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               MeshingParameterArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_meshing_parameters(object old_name, object name = null,
                                                           object selection = null, object max_size = null,
                                                           object min_size = null, object grading = null,
                                                           object elements_per_edge = null,
                                                           object elements_per_curve = null,
                                                           object second_order = null,
                                                           object midside_nodes_on_geometry = null,
                                                           object relative_size = null, object factor_max = null,
                                                           object factor_min = null,
                                                           object split_compound_mesh = null,
                                                           object merge_compound_parts = null,
                                                           object merge_cells = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            MeshingParameters item = _service.CloneItem<MeshingParameters>(oldName);
            bool valuesGiven = IsAnyGiven(max_size, min_size, grading, elements_per_edge,
                                          elements_per_curve, second_order, midside_nodes_on_geometry,
                                          relative_size, factor_max, factor_min, split_compound_mesh,
                                          merge_compound_parts, merge_cells);
            ApplyMeshingParameters(item, max_size, min_size, grading, elements_per_edge,
                                   elements_per_curve, second_order, midside_nodes_on_geometry,
                                   relative_size, factor_max, factor_min, split_compound_mesh,
                                   merge_compound_parts, merge_cells);
            return Replace(oldName, nameText, item, selectionValue, GetPartDefaultsRebuild(valuesGiven));
        }

        [PythonAPI("Adds a local mesh size for the selected geometry. Use either element_size, larger than 0, " +
                   "or number_of_elements, at least 1.",
                   Arguments = "name: str, selection: Selection, element_size: float = None, " +
                               "number_of_elements: int = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_local_mesh_size(object name, object selection, object element_size = null,
                                                    object number_of_elements = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            if (element_size == null && number_of_elements == null)
                throw new PythonAPIException("Either 'element_size' or 'number_of_elements' must be given.");
            //
            _service.ValidateNewName(nameText);
            LocalMeshSize item = new LocalMeshSize(nameText);
            ApplyLocalMeshSize(item, element_size, number_of_elements);
            _service.Add(item, nameText, selectionValue);
            return this[nameText];
        }

        [PythonAPI("Replaces a local mesh size, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               "element_size: float = None, number_of_elements: int = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_local_mesh_size(object old_name, object name = null,
                                                        object selection = null, object element_size = null,
                                                        object number_of_elements = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            LocalMeshSize item = _service.CloneItem<LocalMeshSize>(oldName);
            ApplyLocalMeshSize(item, element_size, number_of_elements);
            return Replace(oldName, nameText, item, selectionValue);
        }

        [PythonAPI("Adds a shell MMG mesh setup item, which selects MMG for the selected STL shell parts. " +
                   "hausdorff and relative_hausdorff are larger than 0. relative_size=True uses " +
                   "relative_hausdorff, relative to the bounding box diagonal, and relative_size=False the " +
                   "absolute hausdorff; the distance the item does not use is refused and left out of " +
                   "get_settings.",
                   Arguments = "name: str, selection: Selection, " + ShellMmgArguments,
                   Returns = "MeshSetupItem",
                   Example = "pmx.model.mesh.setup_items.add_shell_mmg('Shell_Mmg-1', selection)")]
        public MeshSetupItemAPI add_shell_mmg(object name, object selection, object relative_size = null,
                                              object relative_hausdorff = null, object hausdorff = null,
                                              object keep_model_edges = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            _service.ValidateNewName(nameText);
            ShellMmg item = _service.CreateDefaultShellMmg(nameText);
            _service.PrepareItem(item, nameText, selectionValue);
            _service.ApplyPartDefaults(item);
            ApplyShellMmg(item, relative_size, relative_hausdorff, hausdorff, keep_model_edges);
            _service.AddPreparedItem(item);
            return this[nameText];
        }

        [PythonAPI("Replaces a shell MMG mesh setup item, keeping every value that is not given. A new selection " +
                   "given without any of the values rebuilds the sizes from the part defaults of the new " +
                   "selection, as the dialog does, when they still equal the part defaults of the region the item " +
                   "had; sizes that differ from those were set earlier and are kept.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               ShellMmgArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_shell_mmg(object old_name, object name = null,
                                                  object selection = null, object relative_size = null,
                                                  object relative_hausdorff = null, object hausdorff = null,
                                                  object keep_model_edges = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            ShellMmg item = _service.CloneItem<ShellMmg>(oldName);
            bool valuesGiven = IsAnyGiven(relative_size, relative_hausdorff, hausdorff, keep_model_edges);
            ApplyShellMmg(item, relative_size, relative_hausdorff, hausdorff, keep_model_edges);
            return Replace(oldName, nameText, item, selectionValue, GetPartDefaultsRebuild(valuesGiven));
        }

        [PythonAPI("Adds a thicken shell mesh item for the selected geometry. thickness is larger than 0 and " +
                   "number_of_layers at least 1.",
                   Arguments = "name: str, selection: Selection, thickness: float, number_of_layers: int = 1, " +
                               "offset: float = 0, keep_model_edges: bool = False", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_thicken_shell_mesh(object name, object selection, object thickness,
                                                       object number_of_layers = null, object offset = null,
                                                       object keep_model_edges = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            _service.ValidateNewName(nameText);
            ThickenShellMesh item = new ThickenShellMesh(nameText);
            item.Thickness = ToThickness(thickness);
            item.NumberOfLayers = ToNumberOfLayers(number_of_layers ?? 1);
            if (offset != null) item.Offset = PythonValueConverter.ToFiniteDouble(offset, "offset");
            item.KeepModelEdges = PythonValueConverter.ToBoolOrDefault(keep_model_edges, false, "keep_model_edges");
            _service.Add(item, nameText, selectionValue);
            return this[nameText];
        }

        [PythonAPI("Replaces a thicken shell mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               "thickness: float = None, number_of_layers: int = None, offset: float = None, " +
                               "keep_model_edges: bool = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_thicken_shell_mesh(object old_name, object name = null,
                                                           object selection = null, object thickness = null,
                                                           object number_of_layers = null, object offset = null,
                                                           object keep_model_edges = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            ThickenShellMesh item = _service.CloneItem<ThickenShellMesh>(oldName);
            if (thickness != null) item.Thickness = ToThickness(thickness);
            if (number_of_layers != null) item.NumberOfLayers = ToNumberOfLayers(number_of_layers);
            if (offset != null) item.Offset = PythonValueConverter.ToFiniteDouble(offset, "offset");
            if (keep_model_edges != null)
                item.KeepModelEdges = PythonValueConverter.ToBool(keep_model_edges, "keep_model_edges");
            return Replace(oldName, nameText, item, selectionValue);
        }

        [PythonAPI("Adds a Netgen mesh item for the selected solid or shell parts. The mesh size comes " +
                   "from the meshing parameters; the item carries the settings that apply to Netgen alone. " +
                   "The 3-sided and 4-sided faces are structured unless told otherwise: transfinite_three_sided " +
                   "is 'Mapped' and transfinite_four_sided True by default. " +
                   "number_of_optimize_steps_2d and number_of_optimize_steps_3d are at least 0, and " +
                   "transfinite_angle_deg lies between 0 and 180, both excluded. transfinite_angle_deg is used " +
                   "only while transfinite_three_sided is not 'No' or transfinite_four_sided is True; judged on " +
                   "the values the item ends up with, an unused angle is refused and left out of get_settings.",
                   Arguments = "name: str, selection: Selection, " + NetgenMeshArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_netgen_mesh(object name, object selection,
                                                object number_of_optimize_steps_2d = null,
                                                object number_of_optimize_steps_3d = null, object quad_dominated = null,
                                                object transfinite_three_sided = null,
                                                object transfinite_four_sided = null,
                                                object transfinite_angle_deg = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            _service.ValidateNewName(nameText);
            NetgenMesh item = new NetgenMesh(nameText);
            ApplyNetgenMesh(item, number_of_optimize_steps_2d, number_of_optimize_steps_3d, quad_dominated,
                            transfiniteThreeSided, transfinite_four_sided, transfinite_angle_deg);
            _service.Add(item, nameText, selectionValue);
            return this[nameText];
        }

        [PythonAPI("Replaces a Netgen mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               NetgenMeshArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_netgen_mesh(object old_name, object name = null, object selection = null,
                                                    object number_of_optimize_steps_2d = null,
                                                    object number_of_optimize_steps_3d = null,
                                                    object quad_dominated = null,
                                                    object transfinite_three_sided = null,
                                                    object transfinite_four_sided = null,
                                                    object transfinite_angle_deg = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            NetgenMesh item = _service.CloneItem<NetgenMesh>(oldName);
            ApplyNetgenMesh(item, number_of_optimize_steps_2d, number_of_optimize_steps_3d, quad_dominated,
                            transfiniteThreeSided, transfinite_four_sided, transfinite_angle_deg);
            return Replace(oldName, nameText, item, selectionValue);
        }

        [PythonAPI("Adds a Gmsh tetrahedral mesh item for the selected geometry. " + GmshValueRanges + " " +
                   GmshUnusedValues,
                   Arguments = "name: str, selection: Selection, algorithm_2d: str = None, " +
                               "algorithm_3d: str = None, recombine_min_quality: float = None, " +
                               GmshRecombineArguments + ", transfinite_angle_deg: float = None, " +
                               "optimize_first_order_solid: str = None, optimize_high_order: str = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_tetrahedral_gmsh(object name, object selection, object algorithm_2d = null,
                                                     object algorithm_3d = null,
                                                     object recombine_min_quality = null,
                                                     object algorithm_recombine = null,
                                                     object transfinite_three_sided = null,
                                                     object transfinite_four_sided = null,
                                                     object transfinite_angle_deg = null,
                                                     object optimize_first_order_solid = null,
                                                     object optimize_high_order = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            string algorithm2d = PythonValueConverter.ToText(algorithm_2d, "algorithm_2d", true);
            string algorithm3d = PythonValueConverter.ToText(algorithm_3d, "algorithm_3d", true);
            string algorithmRecombine = PythonValueConverter.ToText(algorithm_recombine, "algorithm_recombine", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            string optimizeFirstOrderSolid =
                PythonValueConverter.ToText(optimize_first_order_solid, "optimize_first_order_solid", true);
            string optimizeHighOrder = PythonValueConverter.ToText(optimize_high_order, "optimize_high_order", true);
            _service.ValidateNewName(nameText);
            TetrahedralGmsh item = new TetrahedralGmsh(nameText);
            ApplyTetrahedralGmsh(item, algorithm2d, algorithm3d, recombine_min_quality, algorithmRecombine,
                                 transfiniteThreeSided, transfinite_four_sided, transfinite_angle_deg,
                                 optimizeFirstOrderSolid, optimizeHighOrder);
            _service.Add(item, nameText, selectionValue);
            return this[nameText];
        }

        [PythonAPI("Replaces a Gmsh tetrahedral mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_tetrahedral_gmsh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_tetrahedral_gmsh(object old_name, object name = null,
                                                         object selection = null, object algorithm_2d = null,
                                                         object algorithm_3d = null,
                                                         object recombine_min_quality = null,
                                                         object algorithm_recombine = null,
                                                         object transfinite_three_sided = null,
                                                         object transfinite_four_sided = null,
                                                         object transfinite_angle_deg = null,
                                                         object optimize_first_order_solid = null,
                                                         object optimize_high_order = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            string algorithm2d = PythonValueConverter.ToText(algorithm_2d, "algorithm_2d", true);
            string algorithm3d = PythonValueConverter.ToText(algorithm_3d, "algorithm_3d", true);
            string algorithmRecombine = PythonValueConverter.ToText(algorithm_recombine, "algorithm_recombine", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            string optimizeFirstOrderSolid =
                PythonValueConverter.ToText(optimize_first_order_solid, "optimize_first_order_solid", true);
            string optimizeHighOrder = PythonValueConverter.ToText(optimize_high_order, "optimize_high_order", true);
            TetrahedralGmsh item = _service.CloneItem<TetrahedralGmsh>(oldName);
            ApplyTetrahedralGmsh(item, algorithm2d, algorithm3d, recombine_min_quality, algorithmRecombine,
                                 transfiniteThreeSided, transfinite_four_sided, transfinite_angle_deg,
                                 optimizeFirstOrderSolid, optimizeHighOrder);
            return Replace(oldName, nameText, item, selectionValue);
        }

        [PythonAPI("Adds a Gmsh shell mesh item for the selected geometry. A shell has no volume, so the item " +
                   "takes no 3D algorithm. " + GmshValueRanges + " " + GmshUnusedValues,
                   Arguments = "name: str, selection: Selection, algorithm_2d: str = None, " +
                               "recombine_min_quality: float = None, " +
                               GmshRecombineArguments + ", transfinite_angle_deg: float = None, " +
                               "optimize_first_order_shell: str = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_shell_gmsh(object name, object selection, object algorithm_2d = null,
                                               object recombine_min_quality = null,
                                               object algorithm_recombine = null,
                                               object transfinite_three_sided = null,
                                               object transfinite_four_sided = null,
                                               object transfinite_angle_deg = null,
                                               object optimize_first_order_shell = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            string algorithm2d = PythonValueConverter.ToText(algorithm_2d, "algorithm_2d", true);
            string algorithmRecombine = PythonValueConverter.ToText(algorithm_recombine, "algorithm_recombine", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            string optimizeFirstOrderShell =
                PythonValueConverter.ToText(optimize_first_order_shell, "optimize_first_order_shell", true);
            _service.ValidateNewName(nameText);
            ShellGmsh item = new ShellGmsh(nameText);
            ApplyShellGmsh(item, algorithm2d, recombine_min_quality, algorithmRecombine,
                           transfiniteThreeSided, transfinite_four_sided, transfinite_angle_deg,
                           optimizeFirstOrderShell);
            _service.Add(item, nameText, selectionValue);
            return this[nameText];
        }

        [PythonAPI("Replaces a Gmsh shell mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_shell_gmsh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_shell_gmsh(object old_name, object name = null,
                                                   object selection = null, object algorithm_2d = null,
                                                   object recombine_min_quality = null,
                                                   object algorithm_recombine = null,
                                                   object transfinite_three_sided = null,
                                                   object transfinite_four_sided = null,
                                                   object transfinite_angle_deg = null,
                                                   object optimize_first_order_shell = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            string algorithm2d = PythonValueConverter.ToText(algorithm_2d, "algorithm_2d", true);
            string algorithmRecombine = PythonValueConverter.ToText(algorithm_recombine, "algorithm_recombine", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            string optimizeFirstOrderShell =
                PythonValueConverter.ToText(optimize_first_order_shell, "optimize_first_order_shell", true);
            ShellGmsh item = _service.CloneItem<ShellGmsh>(oldName);
            ApplyShellGmsh(item, algorithm2d, recombine_min_quality, algorithmRecombine,
                           transfiniteThreeSided, transfinite_four_sided, transfinite_angle_deg,
                           optimizeFirstOrderShell);
            return Replace(oldName, nameText, item, selectionValue);
        }

        [PythonAPI("Adds a Gmsh transfinite mesh item for the selected geometry. algorithm_2d and algorithm_3d, " +
                   "which the dialog keeps hidden, set the mesh size the 2D algorithm needs and mesh the faces " +
                   "and the volumes that are not structured. " + GmshValueRanges,
                   Arguments = "name: str, selection: Selection, allow_pyramid_elements: bool = False, " +
                               "algorithm_2d: str = None, algorithm_3d: str = None, recombine: bool = None, " +
                               "transfinite_angle_deg: float = None, transfinite_three_sided: str = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_transfinite_mesh(object name, object selection,
                                                     object allow_pyramid_elements = null, object algorithm_2d = null,
                                                     object algorithm_3d = null, object recombine = null,
                                                     object transfinite_angle_deg = null,
                                                     object transfinite_three_sided = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            string algorithm2d = PythonValueConverter.ToText(algorithm_2d, "algorithm_2d", true);
            string algorithm3d = PythonValueConverter.ToText(algorithm_3d, "algorithm_3d", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            _service.ValidateNewName(nameText);
            TransfiniteMesh item = new TransfiniteMesh(nameText);
            item.AllowPyramidElements = PythonValueConverter.ToBoolOrDefault(allow_pyramid_elements, false,
                                                                             "allow_pyramid_elements");
            ApplyTransfiniteMesh(item, algorithm2d, algorithm3d, recombine, transfinite_angle_deg,
                                 transfiniteThreeSided);
            _service.Add(item, nameText, selectionValue);
            return this[nameText];
        }

        [PythonAPI("Replaces a Gmsh transfinite mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_transfinite_mesh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_transfinite_mesh(object old_name, object name = null,
                                                         object selection = null,
                                                         object allow_pyramid_elements = null,
                                                         object algorithm_2d = null, object algorithm_3d = null,
                                                         object recombine = null, object transfinite_angle_deg = null,
                                                         object transfinite_three_sided = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            string algorithm2d = PythonValueConverter.ToText(algorithm_2d, "algorithm_2d", true);
            string algorithm3d = PythonValueConverter.ToText(algorithm_3d, "algorithm_3d", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            TransfiniteMesh item = _service.CloneItem<TransfiniteMesh>(oldName);
            if (allow_pyramid_elements != null)
                item.AllowPyramidElements = PythonValueConverter.ToBool(allow_pyramid_elements,
                                                                        "allow_pyramid_elements");
            ApplyTransfiniteMesh(item, algorithm2d, algorithm3d, recombine, transfinite_angle_deg,
                                 transfiniteThreeSided);
            return Replace(oldName, nameText, item, selectionValue);
        }

        [PythonAPI("Adds a Gmsh sweep mesh item for the selected source surfaces. The sweep direction, " +
                   "the side surfaces and the layer edges are derived by the model from the selection. A part " +
                   "selection - a solid or a compound of solids - lets the model find the source surfaces " +
                   "itself. Either selection may span several parts that stand apart from each other, and " +
                   "each of them is then swept on its own. A cell of a compound the sweep cannot reach is " +
                   "meshed by tetrahedra, with a pyramid on every quadrilateral a swept cell leaves on the face " +
                   "they share. The sweep builds its volume mesh itself, so the item takes no 3D algorithm. The " +
                   "2D algorithm QuadWild meshes the source surfaces with QuadWild, set up by the quad_wild " +
                   "arguments, and gmsh the side surfaces, with the recombination and optimisation of the item. " +
                   "With the symmetry Automatic, the default, a compound whose cells are mirror images across the " +
                   "planes it is split on is swept one symmetric piece at a time and the mesh mirrored onto the " +
                   "other cells, a cell of the piece the sweep cannot reach meshed by tetrahedra and mirrored " +
                   "alike; Off sweeps every cell. quad_wild_alpha lies from 0 to 1, and " +
                   "number_of_global_smooth_steps is 0 or larger. The quad_wild arguments are used only with " +
                   "algorithm_2d 'QuadWild' and are refused naming algorithm_2d otherwise. " + GmshUnusedValues,
                   Arguments = "name: str, selection: Selection, number_of_global_smooth_steps: int = 0, " +
                               "algorithm_2d: str = None, recombine_min_quality: float = None, " +
                               GmshRecombineArguments + ", transfinite_angle_deg: float = None, " +
                               "optimize_first_order_shell: str = None, elements_per_layer: list[int] = None, " +
                               "quad_wild_flow_profile: str = None, quad_wild_alpha: float = None, " +
                               "quad_wild_smooth: bool = None, symmetry: str = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_sweep_mesh(object name, object selection,
                                               object number_of_global_smooth_steps = null, object algorithm_2d = null,
                                               object recombine_min_quality = null,
                                               object algorithm_recombine = null,
                                               object transfinite_three_sided = null,
                                               object transfinite_four_sided = null,
                                               object transfinite_angle_deg = null,
                                               object optimize_first_order_shell = null,
                                               object elements_per_layer = null,
                                               object quad_wild_flow_profile = null, object quad_wild_alpha = null,
                                               object quad_wild_smooth = null, object symmetry = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            string algorithm2d = PythonValueConverter.ToText(algorithm_2d, "algorithm_2d", true);
            string algorithmRecombine = PythonValueConverter.ToText(algorithm_recombine, "algorithm_recombine", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            string optimizeFirstOrderShell =
                PythonValueConverter.ToText(optimize_first_order_shell, "optimize_first_order_shell", true);
            string quadWildFlowProfile =
                PythonValueConverter.ToText(quad_wild_flow_profile, "quad_wild_flow_profile", true);
            string symmetryText = PythonValueConverter.ToText(symmetry, "symmetry", true);
            _service.ValidateNewName(nameText);
            SweepMesh item = new SweepMesh(nameText);
            if (symmetryText != null)
                item.Symmetry = PythonValueConverter.ToEnum<SweepSymmetryEnum>(symmetryText, "symmetry");
            item.NumberOfGlobalSmoothSteps = ToNumberOfGlobalSmoothSteps(number_of_global_smooth_steps ?? 0);
            ApplySweepMesh(item, algorithm2d, recombine_min_quality, algorithmRecombine, transfiniteThreeSided,
                           transfinite_four_sided, transfinite_angle_deg, optimizeFirstOrderShell,
                           elements_per_layer);
            ApplySweepQuadWild(item, quadWildFlowProfile, quad_wild_alpha, quad_wild_smooth);
            _service.Add(item, nameText, selectionValue);
            return this[nameText];
        }

        [PythonAPI("Replaces a Gmsh sweep mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_sweep_mesh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_sweep_mesh(object old_name, object name = null,
                                                   object selection = null,
                                                   object number_of_global_smooth_steps = null,
                                                   object algorithm_2d = null,
                                                   object recombine_min_quality = null,
                                                   object algorithm_recombine = null,
                                                   object transfinite_three_sided = null,
                                                   object transfinite_four_sided = null,
                                                   object transfinite_angle_deg = null,
                                                   object optimize_first_order_shell = null,
                                                   object elements_per_layer = null,
                                                   object quad_wild_flow_profile = null, object quad_wild_alpha = null,
                                                   object quad_wild_smooth = null, object symmetry = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            string algorithm2d = PythonValueConverter.ToText(algorithm_2d, "algorithm_2d", true);
            string algorithmRecombine = PythonValueConverter.ToText(algorithm_recombine, "algorithm_recombine", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            string optimizeFirstOrderShell =
                PythonValueConverter.ToText(optimize_first_order_shell, "optimize_first_order_shell", true);
            string quadWildFlowProfile =
                PythonValueConverter.ToText(quad_wild_flow_profile, "quad_wild_flow_profile", true);
            string symmetryText = PythonValueConverter.ToText(symmetry, "symmetry", true);
            SweepMesh item = _service.CloneItem<SweepMesh>(oldName);
            if (symmetryText != null)
                item.Symmetry = PythonValueConverter.ToEnum<SweepSymmetryEnum>(symmetryText, "symmetry");
            if (number_of_global_smooth_steps != null)
                item.NumberOfGlobalSmoothSteps = ToNumberOfGlobalSmoothSteps(number_of_global_smooth_steps);
            ApplySweepMesh(item, algorithm2d, recombine_min_quality, algorithmRecombine, transfiniteThreeSided,
                           transfinite_four_sided, transfinite_angle_deg, optimizeFirstOrderShell,
                           elements_per_layer);
            ApplySweepQuadWild(item, quadWildFlowProfile, quad_wild_alpha, quad_wild_smooth);
            return Replace(oldName, nameText, item, selectionValue);
        }

        [PythonAPI("Adds a QuadWild shell mesh item for the selected geometry. Every parameter that is not " +
                   "given keeps the application default. triangulation_mesher is Gmsh, Netgen or Mmg, " +
                   "singularity_position and number_of_search_rounds are at least 0, and transfinite_angle_deg " +
                   "lies from 90 to 180. preview=True shows the triangulation without running QuadWild, so " +
                   "smooth, alpha, flow_profile, boundary_layer, transfinite_three_sided, transfinite_four_sided " +
                   "and transfinite_angle_deg are not used then, and transfinite_angle_deg is not used while " +
                   "transfinite_three_sided is 'No' and transfinite_four_sided False; judged on the values the " +
                   "item ends up with, an unused one is refused naming the argument that would use it and is " +
                   "left out of get_settings.",
                   Arguments = "name: str, selection: Selection, preview: bool = None, smooth: bool = None, " +
                               "alpha: float = None, triangulation_mesher: str = None, " +
                               "flow_profile: str = None, " +
                               "keep_files_on_failure: bool = None, debug_visualization: bool = None, " +
                               "old_algorithm: bool = None, pin_edge_nodes: bool = None, edge_rotations: bool = None, " +
                               "singularity_position: float = None, number_of_search_rounds: int = None, " +
                               "boundary_layer: str = None, transfinite_three_sided: str = None, " +
                               "transfinite_four_sided: bool = None, transfinite_angle_deg: float = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_shell_quad_wild(object name, object selection, object preview = null,
                                                    object smooth = null, object alpha = null,
                                                    object triangulation_mesher = null, object flow_profile = null,
                                                    object keep_files_on_failure = null,
                                                    object debug_visualization = null, object old_algorithm = null,
                                                    object pin_edge_nodes = null, object edge_rotations = null,
                                                    object singularity_position = null,
                                                    object number_of_search_rounds = null,
                                                    object boundary_layer = null, object transfinite_three_sided = null,
                                                    object transfinite_four_sided = null,
                                                    object transfinite_angle_deg = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            string triangulationMesher =
                PythonValueConverter.ToText(triangulation_mesher, "triangulation_mesher", true);
            string flowProfile = PythonValueConverter.ToText(flow_profile, "flow_profile", true);
            string boundaryLayer = PythonValueConverter.ToText(boundary_layer, "boundary_layer", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            _service.ValidateNewName(nameText);
            ShellQuadWild item = new ShellQuadWild(nameText);
            ApplyShellQuadWild(item, preview, smooth, alpha, triangulationMesher,
                               flowProfile, keep_files_on_failure, debug_visualization, old_algorithm,
                               pin_edge_nodes, edge_rotations, singularity_position, number_of_search_rounds,
                               boundaryLayer,
                               transfiniteThreeSided, transfinite_four_sided, transfinite_angle_deg);
            _service.Add(item, nameText, selectionValue);
            return this[nameText];
        }

        [PythonAPI("Replaces a QuadWild shell mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_shell_quad_wild", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_shell_quad_wild(object old_name, object name = null,
                                                        object selection = null, object preview = null,
                                                        object smooth = null, object alpha = null, object triangulation_mesher = null,
                                                        object flow_profile = null,
                                                        object keep_files_on_failure = null,
                                                        object debug_visualization = null,
                                                        object old_algorithm = null,
                                                        object pin_edge_nodes = null, object edge_rotations = null,
                                                    object singularity_position = null,
                                                    object number_of_search_rounds = null,
                                                    object boundary_layer = null, object transfinite_three_sided = null,
                                                    object transfinite_four_sided = null,
                                                    object transfinite_angle_deg = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            string triangulationMesher =
                PythonValueConverter.ToText(triangulation_mesher, "triangulation_mesher", true);
            string flowProfile = PythonValueConverter.ToText(flow_profile, "flow_profile", true);
            string boundaryLayer = PythonValueConverter.ToText(boundary_layer, "boundary_layer", true);
            string transfiniteThreeSided =
                PythonValueConverter.ToText(transfinite_three_sided, "transfinite_three_sided", true);
            ShellQuadWild item = _service.CloneItem<ShellQuadWild>(oldName);
            ApplyShellQuadWild(item, preview, smooth, alpha, triangulationMesher,
                               flowProfile, keep_files_on_failure, debug_visualization, old_algorithm,
                               pin_edge_nodes, edge_rotations, singularity_position, number_of_search_rounds,
                               boundaryLayer,
                               transfiniteThreeSided, transfinite_four_sided, transfinite_angle_deg);
            return Replace(oldName, nameText, item, selectionValue);
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override MeshSetupItem GetItem(string name)
        {
            return _service.GetItem(name);
        }
        protected override MeshSetupItemAPI CreateItemAPI(string name)
        {
            return new MeshSetupItemAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void SetItemsActive(MeshSetupItem[] items, bool active)
        {
            _service.SetActive(items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(MeshSetupItem item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "MeshSetupItems(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private MeshSetupItemAPI Replace(string oldName, string name, MeshSetupItem item, SelectionAPI selection)
        {
            return Replace(oldName, name, item, selection, null);
        }
        private MeshSetupItemAPI Replace(string oldName, string name, MeshSetupItem item, SelectionAPI selection,
                                         Action<MeshSetupItem> afterSelectionChanged)
        {
            string storedName = _service.ResolveItem(oldName).Name;
            string newName = name == null ? storedName : name;
            _service.Replace(storedName, item, newName, selection, afterSelectionChanged);
            //
            return this[newName];
        }
        // The meshing parameters and the shell MMG item carry lengths the dialog derives from the size of
        // the selected parts, and it rebuilds them whenever the selection changes and the user has typed
        // none of them in that dialog. One call is one such visit: the values this call gives are the
        // caller's and stop the rebuild, and a call that only moves the item lets the sizes follow the
        // geometry the way clicking the same change would - unless they no longer equal the part defaults
        // of the region the item had, because then an earlier call set them
        private Action<MeshSetupItem> GetPartDefaultsRebuild(bool valuesGiven)
        {
            if (valuesGiven) return null;
            //
            return delegate (MeshSetupItem item)
            {
                if (item is MeshingParameters meshingParameters) _service.ApplyPartDefaults(meshingParameters);
                else if (item is ShellMmg shellMmg) _service.ApplyPartDefaults(shellMmg);
            };
        }
        private static bool IsAnyGiven(params object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null) return true;
            }
            return false;
        }
        private static void ApplyLocalMeshSize(LocalMeshSize item, object elementSize, object numberOfElements)
        {
            if (elementSize != null && numberOfElements != null)
                throw new PythonAPIException("Only one of 'element_size' and 'number_of_elements' can be given.");
            //
            if (elementSize != null)
            {
                double size = PythonValueConverter.ToFiniteDouble(elementSize, "element_size");
                // The model raises a smaller size to this floor
                if (size < 1E-10)
                    throw new PythonAPIException("The argument 'element_size' must be larger than 0 (at least 1e-10), " +
                                                 "but " + size + " was given.");
                item.Type = LocalMeshSizeTypeEnum.ElementSize;
                item.MeshSize = size;
            }
            else if (numberOfElements != null)
            {
                int count = PythonValueConverter.ToInt(numberOfElements, "number_of_elements");
                if (count < 1)
                    throw new PythonAPIException("The argument 'number_of_elements' must be at least 1, but " + count +
                                                 " was given.");
                item.Type = LocalMeshSizeTypeEnum.NumberOfElements;
                item.NumOfElements = count;
            }
        }
        private static void ApplyMeshingParameters(MeshingParameters item, object maxSize, object minSize,
                                                   object grading, object elementsPerEdge,
                                                   object elementsPerCurve, object secondOrder,
                                                   object midsideNodesOnGeometry, object relativeSize,
                                                   object factorMax, object factorMin, object splitCompoundMesh,
                                                   object mergeCompoundParts, object mergeCells)
        {
            // Every value is converted and checked before the first one is assigned, because the setters of a range
            // move one limit along with the other
            bool? relative = null;
            if (relativeSize != null) relative = PythonValueConverter.ToBool(relativeSize, "relative_size");
            // The factors size only a relative Netgen mesh, while the sizes are used in either mode
            bool relativeSizeUsed = relative ?? IsRelativeSizeUsed(item);
            string kept = relative.HasValue ? "" : ", which the item keeps,";
            if (!relativeSizeUsed && (factorMax != null || factorMin != null))
                throw new PythonAPIException("The argument '" + (factorMax != null ? "factor_max" : "factor_min") +
                                             "' is not used with relative_size=False" + kept + " as the mesh is " +
                                             "then sized by max_size and min_size. Give relative_size=True to " +
                                             "size it by factor_max and factor_min.");
            double[] factors = ResolveRange(factorMax, factorMin, item.FactorMax, item.FactorMin, "factor_max",
                                            "factor_min", false);
            double[] sizes = ResolveRange(maxSize, minSize, item.MaxH, item.MinH, "max_size", "min_size", true);
            double? gradingValue = ToFraction(grading, "grading");
            double? edgeCount = ToPositive(elementsPerEdge, "elements_per_edge");
            double? curveCount = ToPositive(elementsPerCurve, "elements_per_curve");
            bool? second = null;
            if (secondOrder != null) second = PythonValueConverter.ToBool(secondOrder, "second_order");
            bool? midside = null;
            if (midsideNodesOnGeometry != null)
                midside = PythonValueConverter.ToBool(midsideNodesOnGeometry, "midside_nodes_on_geometry");
            // The dialog shows the switch only for second order elements, and the meshers use it only then
            if (midside.HasValue && !(second ?? item.SecondOrder))
                throw new PythonAPIException("The argument 'midside_nodes_on_geometry' is used only for second " +
                                             "order elements, so it cannot be given with second_order=False" +
                                             (second.HasValue ? "." : ", which the item keeps. Give " +
                                                                      "second_order=True as well."));
            bool split = splitCompoundMesh != null
                         ? PythonValueConverter.ToBool(splitCompoundMesh, "split_compound_mesh") : item.SplitCompoundMesh;
            bool merge = mergeCompoundParts != null
                         ? PythonValueConverter.ToBool(mergeCompoundParts, "merge_compound_parts")
                         : item.MergeCompoundParts;
            bool? cells = null;
            if (mergeCells != null) cells = PythonValueConverter.ToBool(mergeCells, "merge_cells");
            // The model turns one of the two off when the other is turned on
            if (split && merge)
                throw new PythonAPIException("The arguments 'split_compound_mesh' and 'merge_compound_parts' cannot " +
                                             "both be True" + (splitCompoundMesh != null && mergeCompoundParts != null
                                                               ? "." : ", and the item keeps the other one True; " +
                                                               "pass it as False."));
            //
            if (relative.HasValue)
            {
                item.RelativeSize = relative.Value;
                // The dialog offers the relative switch only in its advanced view, and its basic view meshes by
                // the sizes
                if (relative.Value) item.AdvancedView = true;
            }
            if (factors != null)
                SetOrderedRange(factors[0], factors[1], item.FactorMin, value => item.FactorMax = value,
                                value => item.FactorMin = value);
            if (sizes != null)
                SetOrderedRange(sizes[0], sizes[1], item.MinH, value => item.MaxH = value, value => item.MinH = value);
            if (gradingValue.HasValue) item.Grading = gradingValue.Value;
            if (edgeCount.HasValue) item.ElementsPerEdge = edgeCount.Value;
            if (curveCount.HasValue) item.ElementsPerCurve = curveCount.Value;
            if (second.HasValue) item.SecondOrder = second.Value;
            if (midside.HasValue) item.MidsideNodesOnGeometry = midside.Value;
            if (splitCompoundMesh != null || mergeCompoundParts != null)
            {
                item.SplitCompoundMesh = split;
                item.MergeCompoundParts = merge;
            }
            if (cells.HasValue) item.MergeCells = cells.Value;
        }
        // The maximum and the minimum the call leaves the item with, the given ones checked against the kept
        // ones, or null when neither is given. The setters of a maximum and a minimum move the other one along
        // when it is passed, so the pair is refused here rather than silently changed
        internal static double[] ResolveRange(object maxValue, object minValue, double currentMax, double currentMin,
                                              string maxName, string minName, bool isLength)
        {
            if (maxValue == null && minValue == null) return null;
            //
            double max = currentMax;
            double min = currentMin;
            if (maxValue != null) max = isLength ? ToNonNegative(maxValue, maxName) : ToFactor(maxValue, maxName);
            if (minValue != null) min = isLength ? ToNonNegative(minValue, minName) : ToFactor(minValue, minName);
            if (min > max)
            {
                string kept = maxValue == null ? " The item keeps '" + maxName + "' at " + max + "; pass it as well."
                                               : minValue == null ? " The item keeps '" + minName + "' at " + min +
                                                                    "; pass it as well." : "";
                throw new PythonAPIException("The argument '" + minName + "' (" + min + ") must not be larger than '" +
                                             maxName + "' (" + max + ")." + kept);
            }
            return new double[] { max, min };
        }
        // Sets a checked pair in the order in which neither setter moves the other value
        internal static void SetOrderedRange(double max, double min, double currentMin, Action<double> setMax,
                                             Action<double> setMin)
        {
            if (max >= currentMin)
            {
                setMax(max);
                setMin(min);
            }
            else
            {
                setMin(min);
                setMax(max);
            }
        }
        private static double ToNonNegative(object value, string argumentName)
        {
            double number = PythonValueConverter.ToFiniteDouble(value, argumentName);
            if (number < 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must be larger or equal to 0, but " +
                                             number + " was given.");
            return number;
        }
        private static double ToFactor(object value, string argumentName)
        {
            double number = PythonValueConverter.ToFiniteDouble(value, argumentName);
            if (number <= 0 || number > 1)
                throw new PythonAPIException("The argument '" + argumentName + "' must be larger than 0 and at most 1, " +
                                             "but " + number + " was given.");
            return number;
        }
        private static double? ToFraction(object value, string argumentName)
        {
            if (value == null) return null;
            //
            return ToFactor(value, argumentName);
        }
        private static double? ToPositive(object value, string argumentName)
        {
            if (value == null) return null;
            //
            double number = PythonValueConverter.ToFiniteDouble(value, argumentName);
            if (number <= 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must be larger than 0, but " + number +
                                             " was given.");
            return number;
        }
        private static int? ToNonNegativeInt(object value, string argumentName)
        {
            if (value == null) return null;
            //
            int number = PythonValueConverter.ToInt(value, argumentName);
            if (number < 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must be larger or equal to 0, but " +
                                             number + " was given.");
            return number;
        }
        private static double? ToAngle(object value, string argumentName, double min, double max, bool inclusive)
        {
            if (value == null) return null;
            //
            double number = PythonValueConverter.ToFiniteDouble(value, argumentName);
            bool outside = inclusive ? number < min || number > max : number <= min || number >= max;
            if (outside)
                throw new PythonAPIException("The argument '" + argumentName + "' must lie " +
                                             (inclusive ? "from " + min + " to " + max : "between " + min + " and " +
                                                          max + ", both excluded") + ", but " + number + " was given.");
            return number;
        }
        internal static double ToThickness(object value)
        {
            double number = PythonValueConverter.ToFiniteDouble(value, "thickness");
            if (number <= 0)
                throw new PythonAPIException("The argument 'thickness' must be larger than 0, but " + number +
                                             " was given.");
            return number;
        }
        // The sweep item raises a negative count to zero, so the value the script gave is refused instead
        private static int ToNumberOfGlobalSmoothSteps(object value)
        {
            int number = PythonValueConverter.ToInt(value, "number_of_global_smooth_steps");
            if (number < 0)
                throw new PythonAPIException("The argument 'number_of_global_smooth_steps' must be 0 or larger, but " +
                                             number + " was given.");
            return number;
        }
        internal static int ToNumberOfLayers(object value)
        {
            int number = PythonValueConverter.ToInt(value, "number_of_layers");
            if (number < 1)
                throw new PythonAPIException("The argument 'number_of_layers' must be at least 1, but " + number +
                                             " was given.");
            return number;
        }
        private static void ApplyNetgenMesh(NetgenMesh item, object numberOfOptimizeSteps2D, object numberOfOptimizeSteps3D,
                                            object quadDominated, string transfiniteThreeSided,
                                            object transfiniteFourSided, object transfiniteAngle)
        {
            int? steps2D = ToNonNegativeInt(numberOfOptimizeSteps2D, "number_of_optimize_steps_2d");
            int? steps3D = ToNonNegativeInt(numberOfOptimizeSteps3D, "number_of_optimize_steps_3d");
            TransfiniteThreeSidedEnum threeSided = ToTransfiniteThreeSided(transfiniteThreeSided,
                                                                           item.TransfiniteThreeSided);
            bool fourSided = transfiniteFourSided != null
                             ? PythonValueConverter.ToBool(transfiniteFourSided, "transfinite_four_sided")
                             : item.TransfiniteFourSided;
            RefuseUnusedTransfiniteAngle(transfiniteAngle, threeSided, fourSided);
            double? angle = ToAngle(transfiniteAngle, "transfinite_angle_deg", 0, 180, false);
            //
            if (steps2D.HasValue) item.OptimizeSteps2D = steps2D.Value;
            if (steps3D.HasValue) item.OptimizeSteps3D = steps3D.Value;
            if (quadDominated != null)
                item.QuadDominated = PythonValueConverter.ToBool(quadDominated, "quad_dominated");
            item.TransfiniteThreeSided = threeSided;
            item.TransfiniteFourSided = fourSided;
            if (angle.HasValue) item.TransfiniteAngleDeg = angle.Value;
        }
        private static void ApplyShellMmg(ShellMmg item, object relativeSize, object relativeHausdorff,
                                          object hausdorff, object keepModelEdges)
        {
            bool? relative = null;
            if (relativeSize != null) relative = PythonValueConverter.ToBool(relativeSize, "relative_size");
            double? relativeValue = ToPositive(relativeHausdorff, "relative_hausdorff");
            double? hausdorffValue = ToPositive(hausdorff, "hausdorff");
            // The dialog shows either the relative or the absolute distance, so the one it hides is refused
            bool relativeUsed = relative ?? item.RelativeSize;
            string kept = relative.HasValue ? "" : ", which the item keeps,";
            if (relativeUsed && hausdorffValue.HasValue)
                throw new PythonAPIException("The argument 'hausdorff' is not used with relative_size=True" + kept +
                                             " as the distance is then relative_hausdorff. Give " +
                                             "relative_size=False to use hausdorff.");
            if (!relativeUsed && relativeValue.HasValue)
                throw new PythonAPIException("The argument 'relative_hausdorff' is not used with " +
                                             "relative_size=False" + kept + " as the distance is then hausdorff. " +
                                             "Give relative_size=True to use relative_hausdorff.");
            //
            if (relative.HasValue) item.RelativeSize = relative.Value;
            if (relativeValue.HasValue) item.RelativeHausdorff = relativeValue.Value;
            if (hausdorffValue.HasValue) item.Hausdorff = hausdorffValue.Value;
            if (keepModelEdges != null)
                item.KeepModelEdges = PythonValueConverter.ToBool(keepModelEdges, "keep_model_edges");
        }
        private static void ApplyTetrahedralGmsh(TetrahedralGmsh item, string algorithm2D, string algorithm3D,
                                                 object recombineMinQuality, string algorithmRecombine,
                                                 string transfiniteThreeSided, object transfiniteFourSided,
                                                 object transfiniteAngle, string optimizeFirstOrderSolid,
                                                 string optimizeHighOrder)
        {
            ApplyAlgorithms(item, algorithm2D, algorithm3D);
            ApplyRecombination(item, recombineMinQuality, algorithmRecombine, transfiniteThreeSided,
                               transfiniteFourSided, transfiniteAngle);
            if (optimizeFirstOrderSolid != null)
                item.OptimizeFirstOrderSolid =
                    PythonValueConverter.ToEnum<GmshOptimizeFirstOrderSolidEnum>(optimizeFirstOrderSolid,
                                                                                 "optimize_first_order_solid");
            if (optimizeHighOrder != null)
                item.OptimizeHighOrder =
                    PythonValueConverter.ToEnum<GmshOptimizeHighOrderEnum>(optimizeHighOrder, "optimize_high_order");
        }
        private static void ApplyShellGmsh(ShellGmsh item, string algorithm2D, object recombineMinQuality,
                                           string algorithmRecombine, string transfiniteThreeSided,
                                           object transfiniteFourSided, object transfiniteAngle,
                                           string optimizeFirstOrderShell)
        {
            ApplyAlgorithms(item, algorithm2D, null);
            ApplyRecombination(item, recombineMinQuality, algorithmRecombine, transfiniteThreeSided,
                               transfiniteFourSided, transfiniteAngle);
            ApplyOptimizeFirstOrderShell(item, optimizeFirstOrderShell);
        }
        private static void ApplyTransfiniteMesh(TransfiniteMesh item, string algorithm2D, string algorithm3D,
                                                 object recombine, object transfiniteAngle,
                                                 string transfiniteThreeSided)
        {
            ApplyAlgorithms(item, algorithm2D, algorithm3D);
            if (recombine != null)
            {
                // The property grid offers recombination as a switch, and the simple algorithm is what it turns on
                item.AlgorithmRecombine = PythonValueConverter.ToBool(recombine, "recombine")
                                          ? GmshAlgorithmRecombineEnum.Simple : GmshAlgorithmRecombineEnum.None;
            }
            ApplyTransfiniteAngle(item, transfiniteAngle);
            if (transfiniteThreeSided != null)
                item.TransfiniteThreeSided =
                    PythonValueConverter.ToEnum<TransfiniteThreeSidedEnum>(transfiniteThreeSided,
                                                                           "transfinite_three_sided");
        }
        private static void ApplySweepMesh(SweepMesh item, string algorithm2D, object recombineMinQuality,
                                           string algorithmRecombine, string transfiniteThreeSided,
                                           object transfiniteFourSided, object transfiniteAngle,
                                           string optimizeFirstOrderShell, object elementsPerLayer)
        {
            // The 2D algorithm of a sweep may also be QuadWild, which meshes the source surfaces while gmsh
            // meshes the walls with the recombination and optimisation of the item
            if (algorithm2D != null)
                item.SweepAlgorithmMesh2D = PythonValueConverter.ToEnum<SweepAlgorithmMesh2DEnum>(algorithm2D,
                                                                                                   "algorithm_2d");
            ApplyRecombination(item, recombineMinQuality, algorithmRecombine, transfiniteThreeSided,
                               transfiniteFourSided, transfiniteAngle);
            ApplyOptimizeFirstOrderShell(item, optimizeFirstOrderShell);
            ApplyElementsPerLayer(item, elementsPerLayer);
        }
        private static void ApplySweepQuadWild(SweepMesh item, string flowProfile, object alpha, object smooth)
        {
            if (item.SourceMesher != SweepSourceMesherEnum.QuadWild)
            {
                string unused = flowProfile != null ? "quad_wild_flow_profile"
                                : alpha != null ? "quad_wild_alpha" : smooth != null ? "quad_wild_smooth" : null;
                if (unused != null)
                    throw new PythonAPIException("The argument '" + unused + "' is used only with algorithm_2d=" +
                                                 "'QuadWild', but the item meshes the source surfaces with " +
                                                 item.SweepAlgorithmMesh2D + ". Give algorithm_2d='QuadWild' " +
                                                 "to use it.");
            }
            if (flowProfile != null)
                item.QuadWildFlowProfile = PythonValueConverter.ToEnum<QuadWildFlowProfileEnum>(flowProfile,
                                                                                                 "quad_wild_flow_profile");
            if (alpha != null)
            {
                double value = PythonValueConverter.ToFiniteDouble(alpha, "quad_wild_alpha");
                if (value < 0 || value > 1)
                    throw new PythonAPIException("The argument 'quad_wild_alpha' must lie from 0 to 1, but " + value +
                                                 " was given.");
                item.QuadWildAlpha = value;
            }
            if (smooth != null) item.QuadWildSmooth = PythonValueConverter.ToBool(smooth, "quad_wild_smooth");
        }
        private static void ApplyShellQuadWild(ShellQuadWild item, object preview, object smooth,
                                               object alpha, string triangulationMesher,
                                               string flowProfile, object keepFilesOnFailure,
                                               object debugVisualization, object oldAlgorithm,
                                               object pinEdgeNodes, object edgeRotations, object singularityPosition,
                                               object numberOfSearchRounds, string boundaryLayer,
                                               string transfiniteThreeSided, object transfiniteFourSided,
                                               object transfiniteAngle)
        {
            // A preview shows the triangulation without running QuadWild, so the settings of the
            // quadrangulation, which the dialog then hides, are refused
            bool previewed = preview != null ? PythonValueConverter.ToBool(preview, "preview") : item.Preview;
            if (previewed)
            {
                string unused = smooth != null ? "smooth" : alpha != null ? "alpha"
                                : flowProfile != null ? "flow_profile" : boundaryLayer != null ? "boundary_layer"
                                : transfiniteThreeSided != null ? "transfinite_three_sided"
                                : transfiniteFourSided != null ? "transfinite_four_sided"
                                : transfiniteAngle != null ? "transfinite_angle_deg" : null;
                if (unused != null)
                    throw new PythonAPIException("The argument '" + unused + "' is not used with preview=True" +
                                                 (preview == null ? ", which the item keeps," : "") + " as the " +
                                                 "preview shows the triangulation without running QuadWild. Give " +
                                                 "preview=False to use it.");
            }
            TransfiniteThreeSidedEnum threeSided = ToTransfiniteThreeSided(transfiniteThreeSided,
                                                                           item.TransfiniteThreeSided);
            bool fourSided = transfiniteFourSided != null
                             ? PythonValueConverter.ToBool(transfiniteFourSided, "transfinite_four_sided")
                             : item.TransfiniteFourSided;
            RefuseUnusedTransfiniteAngle(transfiniteAngle, threeSided, fourSided);
            // The model holds the angle within 90 to 180 degrees
            double? angle = ToAngle(transfiniteAngle, "transfinite_angle_deg", 90, 180, true);
            //
            item.Preview = previewed;
            if (smooth != null) item.Smooth = PythonValueConverter.ToBool(smooth, "smooth");
            if (alpha != null) item.Alpha = PythonValueConverter.ToFiniteDouble(alpha, "alpha");
            if (triangulationMesher != null)
                item.TriangulationMesher =
                    (MesherTypeEnum)PythonValueConverter.ToEnum<QuadWildTriangulationMesherEnum>(triangulationMesher,
                                                                                                 "triangulation_mesher");
            if (flowProfile != null)
                item.FlowProfile = PythonValueConverter.ToEnum<QuadWildFlowProfileEnum>(flowProfile,
                                                                                         "flow_profile");
            if (keepFilesOnFailure != null)
                item.KeepFilesOnFailure = PythonValueConverter.ToBool(keepFilesOnFailure,
                                                                       "keep_files_on_failure");
            if (debugVisualization != null)
                item.DebugVisualization = PythonValueConverter.ToBool(debugVisualization, "debug_visualization");
            if (oldAlgorithm != null)
                item.OldAlgorithm = PythonValueConverter.ToBool(oldAlgorithm, "old_algorithm");
            if (pinEdgeNodes != null)
                item.PinEdgeNodes = PythonValueConverter.ToBool(pinEdgeNodes, "pin_edge_nodes");
            if (edgeRotations != null)
                item.EdgeRotations = PythonValueConverter.ToBool(edgeRotations, "edge_rotations");
            if (singularityPosition != null)
            {
                double position = PythonValueConverter.ToFiniteDouble(singularityPosition, "singularity_position");
                if (position < 0)
                    throw new PythonAPIException("The argument 'singularity_position' must be larger or equal to 0, " +
                                                 "but " + position + " was given.");
                item.SingularityPosition = position;
            }
            int? rounds = ToNonNegativeInt(numberOfSearchRounds, "number_of_search_rounds");
            if (rounds.HasValue) item.CountSearch = rounds.Value;
            if (boundaryLayer != null)
                item.BoundaryLayer = PythonValueConverter.ToEnum<QuadWildBoundaryLayerEnum>(boundaryLayer, "boundary_layer");
            item.TransfiniteThreeSided = threeSided;
            item.TransfiniteFourSided = fourSided;
            if (angle.HasValue) item.TransfiniteAngleDeg = angle.Value;
        }
        private static void ApplyAlgorithms(GmshSetupItem item, string algorithm2D, string algorithm3D)
        {
            if (algorithm2D != null)
                item.AlgorithmMesh2D = PythonValueConverter.ToEnum<GmshAlgorithmMesh2DEnum>(algorithm2D,
                                                                                             "algorithm_2d");
            if (algorithm3D != null)
                item.AlgorithmMesh3D = PythonValueConverter.ToEnum<GmshAlgorithmMesh3DEnum>(algorithm3D,
                                                                                             "algorithm_3d");
        }
        // The minimum quality and the angle are judged on the recombination and the transfinite settings the item
        // ends up with, the given ones or the ones it keeps
        private static void ApplyRecombination(GmshSetupItem item, object recombineMinQuality,
                                               string algorithmRecombine, string transfiniteThreeSided,
                                               object transfiniteFourSided, object transfiniteAngle)
        {
            GmshAlgorithmRecombineEnum recombine = algorithmRecombine != null
                ? PythonValueConverter.ToEnum<GmshAlgorithmRecombineEnum>(algorithmRecombine, "algorithm_recombine")
                : item.AlgorithmRecombine;
            TransfiniteThreeSidedEnum threeSided = ToTransfiniteThreeSided(transfiniteThreeSided,
                                                                           item.TransfiniteThreeSided);
            bool fourSided = transfiniteFourSided != null
                             ? PythonValueConverter.ToBool(transfiniteFourSided, "transfinite_four_sided")
                             : item.TransfiniteFourSided;
            if (recombineMinQuality != null && recombine == GmshAlgorithmRecombineEnum.None)
                throw new PythonAPIException("The argument 'recombine_min_quality' is not used with " +
                                             "algorithm_recombine='None'" +
                                             (algorithmRecombine == null ? ", which the item keeps," : "") +
                                             " as no triangles are then recombined. Give another " +
                                             "algorithm_recombine to use it.");
            RefuseUnusedTransfiniteAngle(transfiniteAngle, threeSided, fourSided);
            double? minQuality = null;
            if (recombineMinQuality != null)
                minQuality = PythonValueConverter.ToFiniteDouble(recombineMinQuality, "recombine_min_quality");
            double? angle = null;
            if (transfiniteAngle != null)
                angle = PythonValueConverter.ToFiniteDouble(transfiniteAngle, "transfinite_angle_deg");
            //
            item.AlgorithmRecombine = recombine;
            if (minQuality.HasValue) item.RecombineMinQuality = minQuality.Value;
            item.TransfiniteThreeSided = threeSided;
            item.TransfiniteFourSided = fourSided;
            if (angle.HasValue) item.TransfiniteAngleDeg = angle.Value;
        }
        private static TransfiniteThreeSidedEnum ToTransfiniteThreeSided(string transfiniteThreeSided,
                                                                         TransfiniteThreeSidedEnum kept)
        {
            if (transfiniteThreeSided == null) return kept;
            //
            return PythonValueConverter.ToEnum<TransfiniteThreeSidedEnum>(transfiniteThreeSided,
                                                                          "transfinite_three_sided");
        }
        // No face is structured while both transfinite settings are off, and the dialog then hides the angle
        private static void RefuseUnusedTransfiniteAngle(object transfiniteAngle,
                                                         TransfiniteThreeSidedEnum transfiniteThreeSided,
                                                         bool transfiniteFourSided)
        {
            if (transfiniteAngle == null || transfiniteThreeSided != TransfiniteThreeSidedEnum.No ||
                transfiniteFourSided) return;
            //
            throw new PythonAPIException("The argument 'transfinite_angle_deg' is not used while the item ends up " +
                                         "with transfinite_three_sided='No' and transfinite_four_sided=False, as " +
                                         "no face is then structured. Give transfinite_three_sided or " +
                                         "transfinite_four_sided to use it.");
        }
        private static void ApplyTransfiniteAngle(GmshSetupItem item, object transfiniteAngle)
        {
            if (transfiniteAngle == null) return;
            //
            item.TransfiniteAngleDeg = PythonValueConverter.ToFiniteDouble(transfiniteAngle, "transfinite_angle_deg");
        }
        private static void ApplyOptimizeFirstOrderShell(GmshSetupItem item, string optimizeFirstOrderShell)
        {
            if (optimizeFirstOrderShell == null) return;
            //
            item.OptimizeFirstOrderShell =
                PythonValueConverter.ToEnum<GmshOptimizeFirstOrderShellEnum>(optimizeFirstOrderShell,
                                                                             "optimize_first_order_shell");
        }
        private static void ApplyElementsPerLayer(GmshSetupItem item, object elementsPerLayer)
        {
            if (elementsPerLayer == null) return;
            //
            double[] values = PythonDoubleListConverter.ToDoubleArray(elementsPerLayer, "elements_per_layer");
            if (values == null || values.Length == 0)
                throw new PythonAPIException("The argument 'elements_per_layer' must not be empty.");
            int[] counts = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                counts[i] = PythonValueConverter.ToInt(values[i], "elements_per_layer");
                if (counts[i] < 1)
                    throw new PythonAPIException("Each value of 'elements_per_layer' must be larger than zero.");
            }
            item.NumOfElementsPerLayer = counts;
        }
    }
}
