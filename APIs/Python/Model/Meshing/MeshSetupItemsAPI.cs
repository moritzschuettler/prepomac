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

namespace APIs.Python.Model
{
    public class MeshSetupItemsAPI : ModelItemsAPI<MeshSetupItem, MeshSetupItemAPI>
    {
        // Variables
        // The Gmsh settings the property grid offers, split the way the mesher types share them
        private const string MeshingParameterArguments =
            "relative_size: bool = None, factor_max: float = None, factor_min: float = None, " +
            "max_size: float = None, min_size: float = None, fineness: float = None, grading: float = None, " +
            "elements_per_edge: float = None, elements_per_curve: float = None, second_order: bool = None, " +
            "midside_nodes_on_geometry: bool = None, split_compound_mesh: bool = None, " +
            "merge_compound_parts: bool = None";
        private const string NetgenMeshArguments =
            "optimize_steps_2d: int = None, optimize_steps_3d: int = None, quad_dominated: bool = None, " +
            "transfinite_faces: str = None, transfinite_angle: float = None";
        private const string ShellMmgArguments =
            "relative_size: bool = None, relative_hausdorff: float = None, hausdorff: float = None, " +
            "keep_model_edges: bool = None";
        private const string GmshRecombineArguments =
            "algorithm_recombine: str = None, transfinite_three_sided: bool = None, " +
            "transfinite_four_sided: bool = None";
        private const string GmshLayerArguments =
            "element_size_type: str = None, element_scale_factor: float = None, " +
            "number_of_elements: int = None, layer_sizes: list[float] = None, " +
            "elements_per_layer: list[int] = None";
        private readonly MeshSetupItemService _service;


        // Constructors
        internal MeshSetupItemsAPI(MeshSetupItemService service)
            : base("mesh setup item")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds meshing parameters for the selected geometry. Every parameter that is not given " +
                   "keeps the application default.",
                   Arguments = "name: str, selection: Selection, " + MeshingParameterArguments,
                   Returns = "MeshSetupItem",
                   Example = "pmx.model.mesh.setup_items.add_meshing_parameters('MP-1', selection, max_size=5.0)")]
        public MeshSetupItemAPI add_meshing_parameters(string name, SelectionAPI selection, object max_size = null,
                                                       object min_size = null, object fineness = null,
                                                       object grading = null, object elements_per_edge = null,
                                                       object elements_per_curve = null, object second_order = null,
                                                       object midside_nodes_on_geometry = null,
                                                       object relative_size = null, object factor_max = null,
                                                       object factor_min = null,
                                                       object split_compound_mesh = null,
                                                       object merge_compound_parts = null)
        {
            MeshingParameters item = _service.CreateDefaultMeshingParameters(name);
            _service.PrepareItem(item, name, selection);
            _service.ApplyPartDefaults(item);
            ApplyMeshingParameters(item, max_size, min_size, fineness, grading, elements_per_edge,
                                   elements_per_curve, second_order, midside_nodes_on_geometry,
                                   relative_size, factor_max, factor_min, split_compound_mesh,
                                   merge_compound_parts);
            _service.AddPreparedItem(item);
            return this[name];
        }

        [PythonAPI("Replaces meshing parameters, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               MeshingParameterArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_meshing_parameters(string old_name, string name = null,
                                                           SelectionAPI selection = null, object max_size = null,
                                                           object min_size = null, object fineness = null,
                                                           object grading = null, object elements_per_edge = null,
                                                           object elements_per_curve = null,
                                                           object second_order = null,
                                                           object midside_nodes_on_geometry = null,
                                                           object relative_size = null, object factor_max = null,
                                                           object factor_min = null,
                                                           object split_compound_mesh = null,
                                                           object merge_compound_parts = null)
        {
            MeshingParameters item = (MeshingParameters)_service.CloneItem(old_name);
            bool valuesGiven = IsAnyGiven(max_size, min_size, fineness, grading, elements_per_edge,
                                          elements_per_curve, second_order, midside_nodes_on_geometry,
                                          relative_size, factor_max, factor_min, split_compound_mesh,
                                          merge_compound_parts);
            ApplyMeshingParameters(item, max_size, min_size, fineness, grading, elements_per_edge,
                                   elements_per_curve, second_order, midside_nodes_on_geometry,
                                   relative_size, factor_max, factor_min, split_compound_mesh,
                                   merge_compound_parts);
            return Replace(old_name, name, item, selection, GetPartDefaultsRebuild(valuesGiven));
        }

        [PythonAPI("Adds a local mesh size for the selected geometry. Use either element_size or " +
                   "number_of_elements.",
                   Arguments = "name: str, selection: Selection, element_size: float = None, " +
                               "number_of_elements: int = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_local_mesh_size(string name, SelectionAPI selection, object element_size = null,
                                                    object number_of_elements = null)
        {
            if (element_size == null && number_of_elements == null)
                throw new PythonAPIException("Either 'element_size' or 'number_of_elements' must be given.");
            //
            LocalMeshSize item = new LocalMeshSize(name);
            ApplyLocalMeshSize(item, element_size, number_of_elements);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a local mesh size, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               "element_size: float = None, number_of_elements: int = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_local_mesh_size(string old_name, string name = null,
                                                        SelectionAPI selection = null, object element_size = null,
                                                        object number_of_elements = null)
        {
            LocalMeshSize item = (LocalMeshSize)_service.CloneItem(old_name);
            ApplyLocalMeshSize(item, element_size, number_of_elements);
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a shell MMG mesh setup item, which selects MMG for the selected STL shell parts.",
                   Arguments = "name: str, selection: Selection, " + ShellMmgArguments,
                   Returns = "MeshSetupItem",
                   Example = "pmx.model.mesh.setup_items.add_shell_mmg('Shell_Mmg-1', selection)")]
        public MeshSetupItemAPI add_shell_mmg(string name, SelectionAPI selection, object relative_size = null,
                                              object relative_hausdorff = null, object hausdorff = null,
                                              object keep_model_edges = null)
        {
            ShellMmg item = _service.CreateDefaultShellMmg(name);
            _service.PrepareItem(item, name, selection);
            _service.ApplyPartDefaults(item);
            ApplyShellMmg(item, relative_size, relative_hausdorff, hausdorff, keep_model_edges);
            _service.AddPreparedItem(item);
            return this[name];
        }

        [PythonAPI("Replaces a shell MMG mesh setup item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               ShellMmgArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_shell_mmg(string old_name, string name = null,
                                                  SelectionAPI selection = null, object relative_size = null,
                                                  object relative_hausdorff = null, object hausdorff = null,
                                                  object keep_model_edges = null)
        {
            ShellMmg item = (ShellMmg)_service.CloneItem(old_name);
            bool valuesGiven = IsAnyGiven(relative_size, relative_hausdorff, hausdorff, keep_model_edges);
            ApplyShellMmg(item, relative_size, relative_hausdorff, hausdorff, keep_model_edges);
            return Replace(old_name, name, item, selection, GetPartDefaultsRebuild(valuesGiven));
        }

        [PythonAPI("Adds a thicken shell mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, thickness: float, number_of_layers: int = 1, " +
                               "offset: float = 0, keep_model_edges: bool = False", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_thicken_shell_mesh(string name, SelectionAPI selection, object thickness,
                                                       int number_of_layers = 1, object offset = null,
                                                       bool keep_model_edges = false)
        {
            ThickenShellMesh item = new ThickenShellMesh(name);
            item.Thickness = PythonValueConverter.ToFiniteDouble(thickness, "thickness");
            item.NumberOfLayers = number_of_layers;
            if (offset != null) item.Offset = PythonValueConverter.ToFiniteDouble(offset, "offset");
            item.KeepModelEdges = keep_model_edges;
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a thicken shell mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               "thickness: float = None, number_of_layers: int = None, offset: float = None, " +
                               "keep_model_edges: bool = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_thicken_shell_mesh(string old_name, string name = null,
                                                           SelectionAPI selection = null, object thickness = null,
                                                           object number_of_layers = null, object offset = null,
                                                           object keep_model_edges = null)
        {
            ThickenShellMesh item = (ThickenShellMesh)_service.CloneItem(old_name);
            if (thickness != null) item.Thickness = PythonValueConverter.ToFiniteDouble(thickness, "thickness");
            if (number_of_layers != null)
                item.NumberOfLayers = PythonValueConverter.ToInt(number_of_layers, "number_of_layers");
            if (offset != null) item.Offset = PythonValueConverter.ToFiniteDouble(offset, "offset");
            if (keep_model_edges != null)
                item.KeepModelEdges = PythonValueConverter.ToBool(keep_model_edges, "keep_model_edges");
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a Netgen mesh item for the selected solid or shell parts. The mesh size comes " +
                   "from the meshing parameters; the item carries the settings that apply to Netgen alone.",
                   Arguments = "name: str, selection: Selection, " + NetgenMeshArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_netgen_mesh(string name, SelectionAPI selection, object optimize_steps_2d = null,
                                                object optimize_steps_3d = null, object quad_dominated = null,
                                                string transfinite_faces = null,
                                                object transfinite_angle = null)
        {
            NetgenMesh item = new NetgenMesh(name);
            ApplyNetgenMesh(item, optimize_steps_2d, optimize_steps_3d, quad_dominated, transfinite_faces,
                            transfinite_angle);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a Netgen mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               NetgenMeshArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_netgen_mesh(string old_name, string name = null, SelectionAPI selection = null,
                                                    object optimize_steps_2d = null, object optimize_steps_3d = null,
                                                    object quad_dominated = null,
                                                    string transfinite_faces = null,
                                                    object transfinite_angle = null)
        {
            NetgenMesh item = (NetgenMesh)_service.CloneItem(old_name);
            ApplyNetgenMesh(item, optimize_steps_2d, optimize_steps_3d, quad_dominated, transfinite_faces,
                            transfinite_angle);
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a Gmsh tetrahedral mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, algorithm_2d: str = None, " +
                               "algorithm_3d: str = None, recombine_min_quality: float = None, " +
                               GmshRecombineArguments + ", optimize_first_order_solid: str = None, " +
                               "optimize_high_order: str = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_tetrahedral_gmsh(string name, SelectionAPI selection, string algorithm_2d = null,
                                                     string algorithm_3d = null,
                                                     object recombine_min_quality = null,
                                                     string algorithm_recombine = null,
                                                     object transfinite_three_sided = null,
                                                     object transfinite_four_sided = null,
                                                     string optimize_first_order_solid = null,
                                                     string optimize_high_order = null)
        {
            TetrahedralGmsh item = new TetrahedralGmsh(name);
            ApplyTetrahedralGmsh(item, algorithm_2d, algorithm_3d, recombine_min_quality, algorithm_recombine,
                                 transfinite_three_sided, transfinite_four_sided, optimize_first_order_solid,
                                 optimize_high_order);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a Gmsh tetrahedral mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_tetrahedral_gmsh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_tetrahedral_gmsh(string old_name, string name = null,
                                                         SelectionAPI selection = null, string algorithm_2d = null,
                                                         string algorithm_3d = null,
                                                         object recombine_min_quality = null,
                                                         string algorithm_recombine = null,
                                                         object transfinite_three_sided = null,
                                                         object transfinite_four_sided = null,
                                                         string optimize_first_order_solid = null,
                                                         string optimize_high_order = null)
        {
            TetrahedralGmsh item = (TetrahedralGmsh)_service.CloneItem(old_name);
            ApplyTetrahedralGmsh(item, algorithm_2d, algorithm_3d, recombine_min_quality, algorithm_recombine,
                                 transfinite_three_sided, transfinite_four_sided, optimize_first_order_solid,
                                 optimize_high_order);
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a Gmsh shell mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, algorithm_2d: str = None, " +
                               "algorithm_3d: str = None, recombine_min_quality: float = None, " +
                               GmshRecombineArguments + ", transfinite_angle: float = None, " +
                               "optimize_first_order_shell: str = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_shell_gmsh(string name, SelectionAPI selection, string algorithm_2d = null,
                                               string algorithm_3d = null, object recombine_min_quality = null,
                                               string algorithm_recombine = null,
                                               object transfinite_three_sided = null,
                                               object transfinite_four_sided = null,
                                               object transfinite_angle = null,
                                               string optimize_first_order_shell = null)
        {
            ShellGmsh item = new ShellGmsh(name);
            ApplyShellGmsh(item, algorithm_2d, algorithm_3d, recombine_min_quality, algorithm_recombine,
                           transfinite_three_sided, transfinite_four_sided, transfinite_angle,
                           optimize_first_order_shell);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a Gmsh shell mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_shell_gmsh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_shell_gmsh(string old_name, string name = null,
                                                   SelectionAPI selection = null, string algorithm_2d = null,
                                                   string algorithm_3d = null, object recombine_min_quality = null,
                                                   string algorithm_recombine = null,
                                                   object transfinite_three_sided = null,
                                                   object transfinite_four_sided = null,
                                                   object transfinite_angle = null,
                                                   string optimize_first_order_shell = null)
        {
            ShellGmsh item = (ShellGmsh)_service.CloneItem(old_name);
            ApplyShellGmsh(item, algorithm_2d, algorithm_3d, recombine_min_quality, algorithm_recombine,
                           transfinite_three_sided, transfinite_four_sided, transfinite_angle,
                           optimize_first_order_shell);
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a Gmsh transfinite mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, allow_pyramid_elements: bool = False, " +
                               "algorithm_2d: str = None, algorithm_3d: str = None, recombine: bool = None, " +
                               "transfinite_angle: float = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_transfinite_mesh(string name, SelectionAPI selection,
                                                     bool allow_pyramid_elements = false, string algorithm_2d = null,
                                                     string algorithm_3d = null, object recombine = null,
                                                     object transfinite_angle = null)
        {
            TransfiniteMesh item = new TransfiniteMesh(name);
            item.AllowPyramidElements = allow_pyramid_elements;
            ApplyTransfiniteMesh(item, algorithm_2d, algorithm_3d, recombine, transfinite_angle);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a Gmsh transfinite mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_transfinite_mesh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_transfinite_mesh(string old_name, string name = null,
                                                         SelectionAPI selection = null,
                                                         object allow_pyramid_elements = null,
                                                         string algorithm_2d = null, string algorithm_3d = null,
                                                         object recombine = null, object transfinite_angle = null)
        {
            TransfiniteMesh item = (TransfiniteMesh)_service.CloneItem(old_name);
            if (allow_pyramid_elements != null)
                item.AllowPyramidElements = PythonValueConverter.ToBool(allow_pyramid_elements,
                                                                        "allow_pyramid_elements");
            ApplyTransfiniteMesh(item, algorithm_2d, algorithm_3d, recombine, transfinite_angle);
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a Gmsh extrude mesh item for the selected source surfaces. The extrusion " +
                   "direction and center are derived by the model from the selection.",
                   Arguments = "name: str, selection: Selection, algorithm_2d: str = None, " +
                               "recombine_min_quality: float = None, " + GmshRecombineArguments +
                               ", transfinite_angle: float = None, " + GmshLayerArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_extrude_mesh(string name, SelectionAPI selection, object direction = null,
                                                 object extrude_center = null, string algorithm_2d = null,
                                                 object recombine_min_quality = null,
                                                 string algorithm_recombine = null,
                                                 object transfinite_three_sided = null,
                                                 object transfinite_four_sided = null,
                                                 object transfinite_angle = null, string element_size_type = null,
                                                 object element_scale_factor = null,
                                                 object number_of_elements = null, object layer_sizes = null,
                                                 object elements_per_layer = null)
        {
            RejectDerived(direction, "direction");
            RejectDerived(extrude_center, "extrude_center");
            ExtrudeMesh item = new ExtrudeMesh(name);
            ApplyLayeredGmsh(item, algorithm_2d, recombine_min_quality, algorithm_recombine,
                             transfinite_three_sided, transfinite_four_sided, transfinite_angle, element_size_type,
                             element_scale_factor, number_of_elements, layer_sizes, elements_per_layer);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a Gmsh extrude mesh item, keeping every value that is not given. The extrusion " +
                   "direction and center are derived and are rejected when they are given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_extrude_mesh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_extrude_mesh(string old_name, string name = null,
                                                     SelectionAPI selection = null, object direction = null,
                                                     object extrude_center = null, string algorithm_2d = null,
                                                     object recombine_min_quality = null,
                                                     string algorithm_recombine = null,
                                                     object transfinite_three_sided = null,
                                                     object transfinite_four_sided = null,
                                                     object transfinite_angle = null,
                                                     string element_size_type = null,
                                                     object element_scale_factor = null,
                                                     object number_of_elements = null, object layer_sizes = null,
                                                     object elements_per_layer = null)
        {
            RejectDerived(direction, "direction");
            RejectDerived(extrude_center, "extrude_center");
            ExtrudeMesh item = (ExtrudeMesh)_service.CloneItem(old_name);
            ApplyLayeredGmsh(item, algorithm_2d, recombine_min_quality, algorithm_recombine,
                             transfinite_three_sided, transfinite_four_sided, transfinite_angle, element_size_type,
                             element_scale_factor, number_of_elements, layer_sizes, elements_per_layer);
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a Gmsh revolve mesh item for the selected source surfaces. The revolution axis, " +
                   "angle and middle radius are derived by the model from the selection.",
                   Arguments = "name: str, selection: Selection, create_full_360_model: bool = None, " +
                               "algorithm_2d: str = None, recombine_min_quality: float = None, " +
                               GmshRecombineArguments + ", transfinite_angle: float = None, " + GmshLayerArguments,
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_revolve_mesh(string name, SelectionAPI selection, object axis_center = null,
                                                 object axis_direction = null, object angle = null,
                                                 object middle_r = null, object create_full_360_model = null,
                                                 string algorithm_2d = null, object recombine_min_quality = null,
                                                 string algorithm_recombine = null,
                                                 object transfinite_three_sided = null,
                                                 object transfinite_four_sided = null,
                                                 object transfinite_angle = null, string element_size_type = null,
                                                 object element_scale_factor = null,
                                                 object number_of_elements = null, object layer_sizes = null,
                                                 object elements_per_layer = null)
        {
            RejectDerived(axis_center, "axis_center");
            RejectDerived(axis_direction, "axis_direction");
            RejectDerived(angle, "angle");
            RejectDerived(middle_r, "middle_r");
            RevolveMesh item = new RevolveMesh(name);
            ApplyRevolveMesh(item, create_full_360_model, algorithm_2d, recombine_min_quality, algorithm_recombine,
                             transfinite_three_sided, transfinite_four_sided, transfinite_angle, element_size_type,
                             element_scale_factor, number_of_elements, layer_sizes, elements_per_layer);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a Gmsh revolve mesh item, keeping every value that is not given. The revolution " +
                   "axis, angle and middle radius are derived and are rejected when they are given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_revolve_mesh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_revolve_mesh(string old_name, string name = null,
                                                     SelectionAPI selection = null, object axis_center = null,
                                                     object axis_direction = null, object angle = null,
                                                     object middle_r = null, object create_full_360_model = null,
                                                     string algorithm_2d = null,
                                                     object recombine_min_quality = null,
                                                     string algorithm_recombine = null,
                                                     object transfinite_three_sided = null,
                                                     object transfinite_four_sided = null,
                                                     object transfinite_angle = null,
                                                     string element_size_type = null,
                                                     object element_scale_factor = null,
                                                     object number_of_elements = null, object layer_sizes = null,
                                                     object elements_per_layer = null)
        {
            RejectDerived(axis_center, "axis_center");
            RejectDerived(axis_direction, "axis_direction");
            RejectDerived(angle, "angle");
            RejectDerived(middle_r, "middle_r");
            RevolveMesh item = (RevolveMesh)_service.CloneItem(old_name);
            ApplyRevolveMesh(item, create_full_360_model, algorithm_2d, recombine_min_quality, algorithm_recombine,
                             transfinite_three_sided, transfinite_four_sided, transfinite_angle, element_size_type,
                             element_scale_factor, number_of_elements, layer_sizes, elements_per_layer);
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a Gmsh sweep mesh item for the selected source surfaces. The sweep direction, " +
                   "the side surfaces and the layer edges are derived by the model from the selection. A part " +
                   "selection - a solid or a compound of solids - lets the model find the source surfaces " +
                   "itself. Either selection may span several parts that stand apart from each other, and " +
                   "each of them is then swept on its own. The 2D algorithm QuadWild meshes the source surfaces " +
                   "with QuadWild, set up by the quad_wild arguments, and gmsh the side surfaces. With the " +
                   "symmetry Automatic, the default, a compound whose cells are mirror images across the planes " +
                   "it is split on is swept one symmetric piece at a time and the mesh mirrored onto the other " +
                   "cells; Off sweeps every cell.",
                   Arguments = "name: str, selection: Selection, number_of_global_smooth_steps: int = 0, " +
                               "algorithm_2d: str = None, algorithm_3d: str = None, " +
                               "recombine_min_quality: float = None, " + GmshRecombineArguments +
                               ", transfinite_angle: float = None, optimize_first_order_shell: str = None, " +
                               "elements_per_layer: list[int] = None, keep_imprint_edges: bool = False, " +
                               "quad_wild_flow_profile: str = None, quad_wild_alpha: float = None, " +
                               "quad_wild_smooth: bool = None, symmetry: str = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_sweep_mesh(string name, SelectionAPI selection,
                                               int number_of_global_smooth_steps = 0, string algorithm_2d = null,
                                               string algorithm_3d = null, object recombine_min_quality = null,
                                               string algorithm_recombine = null,
                                               object transfinite_three_sided = null,
                                               object transfinite_four_sided = null,
                                               object transfinite_angle = null,
                                               string optimize_first_order_shell = null,
                                               object elements_per_layer = null, bool keep_imprint_edges = false,
                                               string quad_wild_flow_profile = null, object quad_wild_alpha = null,
                                               object quad_wild_smooth = null, string symmetry = null)
        {
            SweepMesh item = new SweepMesh(name);
            if (symmetry != null) item.Symmetry = PythonValueConverter.ToEnum<SweepSymmetryEnum>(symmetry, "symmetry");
            item.NumberOfGlobalSmoothSteps = number_of_global_smooth_steps;
            item.KeepImprintEdges = keep_imprint_edges;
            ApplySweepMesh(item, algorithm_2d, algorithm_3d, recombine_min_quality, algorithm_recombine,
                           transfinite_three_sided, transfinite_four_sided, transfinite_angle,
                           optimize_first_order_shell, elements_per_layer);
            ApplySweepQuadWild(item, quad_wild_flow_profile, quad_wild_alpha, quad_wild_smooth);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a Gmsh sweep mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_sweep_mesh", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_sweep_mesh(string old_name, string name = null,
                                                   SelectionAPI selection = null,
                                                   object number_of_global_smooth_steps = null,
                                                   string algorithm_2d = null, string algorithm_3d = null,
                                                   object recombine_min_quality = null,
                                                   string algorithm_recombine = null,
                                                   object transfinite_three_sided = null,
                                                   object transfinite_four_sided = null,
                                                   object transfinite_angle = null,
                                                   string optimize_first_order_shell = null,
                                                   object elements_per_layer = null, object keep_imprint_edges = null,
                                                   string quad_wild_flow_profile = null, object quad_wild_alpha = null,
                                                   object quad_wild_smooth = null, string symmetry = null)
        {
            SweepMesh item = (SweepMesh)_service.CloneItem(old_name);
            if (symmetry != null) item.Symmetry = PythonValueConverter.ToEnum<SweepSymmetryEnum>(symmetry, "symmetry");
            if (number_of_global_smooth_steps != null)
                item.NumberOfGlobalSmoothSteps =
                    PythonValueConverter.ToInt(number_of_global_smooth_steps, "number_of_global_smooth_steps");
            if (keep_imprint_edges != null)
                item.KeepImprintEdges = PythonValueConverter.ToBool(keep_imprint_edges, "keep_imprint_edges");
            ApplySweepMesh(item, algorithm_2d, algorithm_3d, recombine_min_quality, algorithm_recombine,
                           transfinite_three_sided, transfinite_four_sided, transfinite_angle,
                           optimize_first_order_shell, elements_per_layer);
            ApplySweepQuadWild(item, quad_wild_flow_profile, quad_wild_alpha, quad_wild_smooth);
            return Replace(old_name, name, item, selection);
        }

        [PythonAPI("Adds a QuadWild shell mesh item for the selected geometry. Every parameter that is not " +
                   "given keeps the application default.",
                   Arguments = "name: str, selection: Selection, preview: bool = None, smooth: bool = None, " +
                               "alpha: float = None, triangulation_mesher: str = None, " +
                               "flow_profile: str = None, " +
                               "keep_files_on_failure: bool = None, debug_visualization: bool = None, " +
                               "old_algorithm: bool = None, pin_edge_nodes: bool = None, edge_rotations: bool = None, " +
                               "singularity_position: float = None, count_search: int = None, " +
                               "boundary_layer: str = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_shell_quad_wild(string name, SelectionAPI selection, object preview = null,
                                                    object smooth = null, object alpha = null,
                                                    string triangulation_mesher = null, string flow_profile = null,
                                                    object keep_files_on_failure = null,
                                                    object debug_visualization = null, object old_algorithm = null,
                                                    object pin_edge_nodes = null, object edge_rotations = null,
                                                    object singularity_position = null, object count_search = null,
                                                    string boundary_layer = null)
        {
            ShellQuadWild item = new ShellQuadWild(name);
            ApplyShellQuadWild(item, preview, smooth, alpha, triangulation_mesher,
                               flow_profile, keep_files_on_failure, debug_visualization, old_algorithm,
                               pin_edge_nodes, edge_rotations, singularity_position, count_search, boundary_layer);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Replaces a QuadWild shell mesh item, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, and the " +
                               "arguments of add_shell_quad_wild", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI replace_shell_quad_wild(string old_name, string name = null,
                                                        SelectionAPI selection = null, object preview = null,
                                                        object smooth = null, object alpha = null, string triangulation_mesher = null,
                                                        string flow_profile = null,
                                                        object keep_files_on_failure = null,
                                                        object debug_visualization = null,
                                                        object old_algorithm = null,
                                                        object pin_edge_nodes = null, object edge_rotations = null,
                                                    object singularity_position = null, object count_search = null,
                                                    string boundary_layer = null)
        {
            ShellQuadWild item = (ShellQuadWild)_service.CloneItem(old_name);
            ApplyShellQuadWild(item, preview, smooth, alpha, triangulation_mesher,
                               flow_profile, keep_files_on_failure, debug_visualization, old_algorithm,
                               pin_edge_nodes, edge_rotations, singularity_position, count_search, boundary_layer);
            return Replace(old_name, name, item, selection);
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
            string newName = name == null ? oldName : name;
            _service.Replace(oldName, item, newName, selection, afterSelectionChanged);
            //
            return this[newName];
        }
        // The meshing parameters and the shell MMG item carry lengths the dialog derives from the size of
        // the selected parts, and it rebuilds them whenever the selection changes and the user has typed
        // none of them in that dialog. One call is one such visit: the values this call gives are the
        // caller's and stop the rebuild, and a call that only moves the item lets the sizes follow the
        // geometry the way clicking the same change would
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
                item.Type = LocalMeshSizeTypeEnum.ElementSize;
                item.MeshSize = PythonValueConverter.ToFiniteDouble(elementSize, "element_size");
            }
            else if (numberOfElements != null)
            {
                item.Type = LocalMeshSizeTypeEnum.NumberOfElements;
                item.NumOfElements = PythonValueConverter.ToInt(numberOfElements, "number_of_elements");
            }
        }
        private static void ApplyMeshingParameters(MeshingParameters item, object maxSize, object minSize,
                                                   object fineness, object grading, object elementsPerEdge,
                                                   object elementsPerCurve, object secondOrder,
                                                   object midsideNodesOnGeometry, object relativeSize,
                                                   object factorMax, object factorMin, object splitCompoundMesh,
                                                   object mergeCompoundParts)
        {
            if (relativeSize != null) item.RelativeSize = PythonValueConverter.ToBool(relativeSize, "relative_size");
            if (factorMax != null) item.FactorMax = PythonValueConverter.ToFiniteDouble(factorMax, "factor_max");
            if (factorMin != null) item.FactorMin = PythonValueConverter.ToFiniteDouble(factorMin, "factor_min");
            if (maxSize != null) item.MaxH = PythonValueConverter.ToFiniteDouble(maxSize, "max_size");
            if (minSize != null) item.MinH = PythonValueConverter.ToFiniteDouble(minSize, "min_size");
            if (fineness != null) item.Fineness = PythonValueConverter.ToFiniteDouble(fineness, "fineness");
            if (grading != null) item.Grading = PythonValueConverter.ToFiniteDouble(grading, "grading");
            if (elementsPerEdge != null)
                item.ElementsPerEdge = PythonValueConverter.ToFiniteDouble(elementsPerEdge, "elements_per_edge");
            if (elementsPerCurve != null)
                item.ElementsPerCurve = PythonValueConverter.ToFiniteDouble(elementsPerCurve, "elements_per_curve");
            if (secondOrder != null)
                item.SecondOrder = PythonValueConverter.ToBool(secondOrder, "second_order");
            if (midsideNodesOnGeometry != null)
                item.MidsideNodesOnGeometry = PythonValueConverter.ToBool(midsideNodesOnGeometry,
                                                                          "midside_nodes_on_geometry");
            if (splitCompoundMesh != null)
                item.SplitCompoundMesh = PythonValueConverter.ToBool(splitCompoundMesh, "split_compound_mesh");
            if (mergeCompoundParts != null)
                item.MergeCompoundParts = PythonValueConverter.ToBool(mergeCompoundParts, "merge_compound_parts");
        }
        private static void ApplyNetgenMesh(NetgenMesh item, object optimizeSteps2D, object optimizeSteps3D,
                                            object quadDominated, string transfiniteFaces,
                                            object transfiniteAngle)
        {
            if (optimizeSteps2D != null)
                item.OptimizeSteps2D = PythonValueConverter.ToInt(optimizeSteps2D, "optimize_steps_2d");
            if (optimizeSteps3D != null)
                item.OptimizeSteps3D = PythonValueConverter.ToInt(optimizeSteps3D, "optimize_steps_3d");
            if (quadDominated != null)
                item.QuadDominated = PythonValueConverter.ToBool(quadDominated, "quad_dominated");
            if (transfiniteFaces != null)
                item.TransfiniteFaces = PythonValueConverter.ToEnum<NetgenTransfiniteFacesEnum>(transfiniteFaces,
                                                                                         "transfinite_faces");
            if (transfiniteAngle != null)
                item.TransfiniteAngleDeg = PythonValueConverter.ToFiniteDouble(transfiniteAngle, "transfinite_angle");
        }
        private static void ApplyShellMmg(ShellMmg item, object relativeSize, object relativeHausdorff,
                                          object hausdorff, object keepModelEdges)
        {
            if (relativeSize != null) item.RelativeSize = PythonValueConverter.ToBool(relativeSize, "relative_size");
            if (relativeHausdorff != null)
                item.RelativeHausdorff = PythonValueConverter.ToFiniteDouble(relativeHausdorff, "relative_hausdorff");
            if (hausdorff != null) item.Hausdorff = PythonValueConverter.ToFiniteDouble(hausdorff, "hausdorff");
            if (keepModelEdges != null)
                item.KeepModelEdges = PythonValueConverter.ToBool(keepModelEdges, "keep_model_edges");
        }
        private static void ApplyTetrahedralGmsh(TetrahedralGmsh item, string algorithm2D, string algorithm3D,
                                                 object recombineMinQuality, string algorithmRecombine,
                                                 object transfiniteThreeSided, object transfiniteFourSided,
                                                 string optimizeFirstOrderSolid, string optimizeHighOrder)
        {
            ApplyAlgorithms(item, algorithm2D, algorithm3D);
            ApplyRecombination(item, recombineMinQuality, algorithmRecombine, transfiniteThreeSided,
                               transfiniteFourSided);
            if (optimizeFirstOrderSolid != null)
                item.OptimizeFirstOrderSolid =
                    PythonValueConverter.ToEnum<GmshOptimizeFirstOrderSolidEnum>(optimizeFirstOrderSolid,
                                                                                 "optimize_first_order_solid");
            if (optimizeHighOrder != null)
                item.OptimizeHighOrder =
                    PythonValueConverter.ToEnum<GmshOptimizeHighOrderEnum>(optimizeHighOrder, "optimize_high_order");
        }
        private static void ApplyShellGmsh(ShellGmsh item, string algorithm2D, string algorithm3D,
                                           object recombineMinQuality, string algorithmRecombine,
                                           object transfiniteThreeSided, object transfiniteFourSided,
                                           object transfiniteAngle, string optimizeFirstOrderShell)
        {
            ApplyAlgorithms(item, algorithm2D, algorithm3D);
            ApplyRecombination(item, recombineMinQuality, algorithmRecombine, transfiniteThreeSided,
                               transfiniteFourSided);
            ApplyTransfiniteAngle(item, transfiniteAngle);
            ApplyOptimizeFirstOrderShell(item, optimizeFirstOrderShell);
        }
        private static void ApplyTransfiniteMesh(TransfiniteMesh item, string algorithm2D, string algorithm3D,
                                                 object recombine, object transfiniteAngle)
        {
            ApplyAlgorithms(item, algorithm2D, algorithm3D);
            if (recombine != null)
            {
                // The property grid offers recombination as a switch, and the simple algorithm is what it turns on
                item.AlgorithmRecombine = PythonValueConverter.ToBool(recombine, "recombine")
                                          ? GmshAlgorithmRecombineEnum.Simple : GmshAlgorithmRecombineEnum.None;
            }
            ApplyTransfiniteAngle(item, transfiniteAngle);
        }
        private static void ApplyLayeredGmsh(GmshSetupItem item, string algorithm2D, object recombineMinQuality,
                                             string algorithmRecombine, object transfiniteThreeSided,
                                             object transfiniteFourSided, object transfiniteAngle,
                                             string elementSizeType, object elementScaleFactor,
                                             object numberOfElements, object layerSizes, object elementsPerLayer)
        {
            ApplyAlgorithms(item, algorithm2D, null);
            ApplyRecombination(item, recombineMinQuality, algorithmRecombine, transfiniteThreeSided,
                               transfiniteFourSided);
            ApplyTransfiniteAngle(item, transfiniteAngle);
            if (elementSizeType != null)
                item.ElementSizeType = PythonValueConverter.ToEnum<ElementSizeTypeEnum>(elementSizeType,
                                                                                        "element_size_type");
            // The setters clamp a value out of their domain, so the domain is checked before they run
            if (elementScaleFactor != null)
            {
                double scaleFactor = PythonValueConverter.ToFiniteDouble(elementScaleFactor,
                                                                         "element_scale_factor");
                if (scaleFactor <= 0)
                    throw new PythonAPIException("The argument 'element_scale_factor' must be larger than zero.");
                item.ElementScaleFactor = scaleFactor;
            }
            if (numberOfElements != null)
            {
                int count = PythonValueConverter.ToInt(numberOfElements, "number_of_elements");
                if (count < 1)
                    throw new PythonAPIException("The argument 'number_of_elements' must be larger than zero.");
                item.NumberOfElements = count;
            }
            if (layerSizes != null) item.LayerSizes = ToLayerSizes(layerSizes);
            ApplyElementsPerLayer(item, elementsPerLayer);
            // The effective definition is checked, so a replacement that keeps one of the two arrays is
            // checked against the array it keeps
            if (layerSizes != null || elementsPerLayer != null ||
                item.ElementSizeType == ElementSizeTypeEnum.MultiLayerd)
                ValidateLayerArrays(item);
        }
        private static void ApplyRevolveMesh(RevolveMesh item, object createFull360Model, string algorithm2D,
                                             object recombineMinQuality, string algorithmRecombine,
                                             object transfiniteThreeSided, object transfiniteFourSided,
                                             object transfiniteAngle, string elementSizeType,
                                             object elementScaleFactor, object numberOfElements, object layerSizes,
                                             object elementsPerLayer)
        {
            ApplyLayeredGmsh(item, algorithm2D, recombineMinQuality, algorithmRecombine, transfiniteThreeSided,
                             transfiniteFourSided, transfiniteAngle, elementSizeType, elementScaleFactor,
                             numberOfElements, layerSizes, elementsPerLayer);
            if (createFull360Model != null)
                item.CreateFull360Model = PythonValueConverter.ToBool(createFull360Model, "create_full_360_model");
        }
        private static void ApplySweepMesh(SweepMesh item, string algorithm2D, string algorithm3D,
                                           object recombineMinQuality, string algorithmRecombine,
                                           object transfiniteThreeSided, object transfiniteFourSided,
                                           object transfiniteAngle, string optimizeFirstOrderShell,
                                           object elementsPerLayer)
        {
            // The 2D algorithm of a sweep may also be QuadWild, which meshes the source surfaces
            if (algorithm2D != null)
                item.SweepAlgorithmMesh2D = PythonValueConverter.ToEnum<SweepAlgorithmMesh2DEnum>(algorithm2D,
                                                                                                   "algorithm_2d");
            ApplyAlgorithms(item, null, algorithm3D);
            ApplyRecombination(item, recombineMinQuality, algorithmRecombine, transfiniteThreeSided,
                               transfiniteFourSided);
            ApplyTransfiniteAngle(item, transfiniteAngle);
            ApplyOptimizeFirstOrderShell(item, optimizeFirstOrderShell);
            ApplyElementsPerLayer(item, elementsPerLayer);
        }
        private static void ApplySweepQuadWild(SweepMesh item, string flowProfile, object alpha, object smooth)
        {
            if (flowProfile != null)
                item.QuadWildFlowProfile = PythonValueConverter.ToEnum<QuadWildFlowProfileEnum>(flowProfile,
                                                                                                 "quad_wild_flow_profile");
            if (alpha != null) item.QuadWildAlpha = PythonValueConverter.ToFiniteDouble(alpha, "quad_wild_alpha");
            if (smooth != null) item.QuadWildSmooth = PythonValueConverter.ToBool(smooth, "quad_wild_smooth");
        }
        private static void ApplyShellQuadWild(ShellQuadWild item, object preview, object smooth,
                                               object alpha, string triangulationMesher,
                                               string flowProfile, object keepFilesOnFailure,
                                               object debugVisualization, object oldAlgorithm,
                                               object pinEdgeNodes, object edgeRotations, object singularityPosition,
                                               object countSearch, string boundaryLayer)
        {
            if (preview != null) item.Preview = PythonValueConverter.ToBool(preview, "preview");
            if (smooth != null) item.Smooth = PythonValueConverter.ToBool(smooth, "smooth");
            if (alpha != null) item.Alpha = PythonValueConverter.ToFiniteDouble(alpha, "alpha");
            if (triangulationMesher != null)
                item.TriangulationMesher = PythonValueConverter.ToEnum<MesherTypeEnum>(triangulationMesher,
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
                item.SingularityPosition = PythonValueConverter.ToFiniteDouble(singularityPosition, "singularity_position");
            if (countSearch != null)
                item.CountSearch = PythonValueConverter.ToInt(countSearch, "count_search");
            if (boundaryLayer != null)
                item.BoundaryLayer = PythonValueConverter.ToEnum<QuadWildBoundaryLayerEnum>(boundaryLayer, "boundary_layer");
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
        private static void ApplyRecombination(GmshSetupItem item, object recombineMinQuality,
                                               string algorithmRecombine, object transfiniteThreeSided,
                                               object transfiniteFourSided)
        {
            if (recombineMinQuality != null)
                item.RecombineMinQuality = PythonValueConverter.ToFiniteDouble(recombineMinQuality,
                                                                               "recombine_min_quality");
            if (algorithmRecombine != null)
                item.AlgorithmRecombine =
                    PythonValueConverter.ToEnum<GmshAlgorithmRecombineEnum>(algorithmRecombine,
                                                                            "algorithm_recombine");
            if (transfiniteThreeSided != null)
                item.TransfiniteThreeSided = PythonValueConverter.ToBool(transfiniteThreeSided,
                                                                         "transfinite_three_sided");
            if (transfiniteFourSided != null)
                item.TransfiniteFourSided = PythonValueConverter.ToBool(transfiniteFourSided,
                                                                        "transfinite_four_sided");
        }
        private static void ApplyTransfiniteAngle(GmshSetupItem item, object transfiniteAngle)
        {
            if (transfiniteAngle == null) return;
            //
            item.TransfiniteAngleDeg = PythonValueConverter.ToFiniteDouble(transfiniteAngle, "transfinite_angle");
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
        private static double[] ToLayerSizes(object layerSizes)
        {
            double[] sizes = PythonDoubleListConverter.ToDoubleArray(layerSizes, "layer_sizes");
            if (sizes == null || sizes.Length == 0)
                throw new PythonAPIException("The argument 'layer_sizes' must not be empty.");
            foreach (double size in sizes)
            {
                if (double.IsNaN(size) || double.IsInfinity(size) || size <= 0)
                    throw new PythonAPIException("Each layer size must be a finite value larger than zero.");
            }
            //
            return sizes;
        }
        private static void ValidateLayerArrays(GmshSetupItem item)
        {
            double[] sizes = item.LayerSizes;
            int[] counts = item.NumOfElementsPerLayer;
            if (sizes == null || sizes.Length == 0 || counts == null || counts.Length == 0)
                throw new PythonAPIException("The layer sizes and the numbers of elements per layer must " +
                                             "both be defined.");
            if (sizes.Length != counts.Length)
                throw new PythonAPIException("The layer sizes and the numbers of elements per layer must " +
                                             "contain the same number of values, but " + sizes.Length + " and " +
                                             counts.Length + " were given.");
        }
        private static void RejectDerived(object value, string argumentName)
        {
            if (value == null) return;
            //
            throw new PythonAPIException("The argument '" + argumentName + "' is derived from the selected " +
                                         "geometry and cannot be supplied.");
        }
    }
}
