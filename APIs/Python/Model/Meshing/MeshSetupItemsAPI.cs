// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python.Model
{
    public class MeshSetupItemsAPI : ModelItemsAPI<MeshSetupItem, MeshSetupItemAPI>
    {
        // Variables
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
                   Arguments = "name: str, selection: Selection, max_size: float = None, min_size: float = None, " +
                               "fineness: float = None, grading: float = None, elements_per_edge: float = None, " +
                               "elements_per_curve: float = None, second_order: bool = None, " +
                               "quad_dominated: bool = None, midside_nodes_on_geometry: bool = None, " +
                               "use_mmg: bool = None, keep_model_edges: bool = None",
                   Returns = "MeshSetupItem",
                   Example = "pmx.model.mesh.setup_items.add_meshing_parameters('MP-1', selection, max_size=5.0)")]
        public MeshSetupItemAPI add_meshing_parameters(string name, SelectionAPI selection, object max_size = null,
                                                       object min_size = null, object fineness = null,
                                                       object grading = null, object elements_per_edge = null,
                                                       object elements_per_curve = null, object second_order = null,
                                                       object quad_dominated = null,
                                                       object midside_nodes_on_geometry = null,
                                                       object use_mmg = null, object keep_model_edges = null)
        {
            MeshingParameters parameters = _service.CreateDefaultMeshingParameters(name);
            ApplyMeshingParameters(parameters, max_size, min_size, fineness, grading, elements_per_edge,
                                   elements_per_curve, second_order, quad_dominated, midside_nodes_on_geometry,
                                   use_mmg, keep_model_edges);
            _service.Add(parameters, name, selection);
            return this[name];
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
            if (element_size != null && number_of_elements != null)
                throw new PythonAPIException("Only one of 'element_size' and 'number_of_elements' can be given.");
            //
            LocalMeshSize localMeshSize = new LocalMeshSize(name);
            if (element_size != null)
            {
                localMeshSize.Type = LocalMeshSizeTypeEnum.ElementSize;
                localMeshSize.MeshSize = PythonValueConverter.ToDouble(element_size, "element_size");
            }
            else
            {
                localMeshSize.Type = LocalMeshSizeTypeEnum.NumberOfElements;
                localMeshSize.NumOfElements = (int)PythonValueConverter.ToDouble(number_of_elements,
                                                                                 "number_of_elements");
            }
            _service.Add(localMeshSize, name, selection);
            return this[name];
        }

        [PythonAPI("Adds a thicken shell mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, thickness: float, number_of_layers: int = 1, " +
                               "offset: float = 0, keep_model_edges: bool = False", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_thicken_shell_mesh(string name, SelectionAPI selection, object thickness,
                                                       int number_of_layers = 1, object offset = null,
                                                       bool keep_model_edges = false)
        {
            ThickenShellMesh item = new ThickenShellMesh(name);
            item.Thickness = PythonValueConverter.ToDouble(thickness, "thickness");
            item.NumberOfLayers = number_of_layers;
            if (offset != null) item.Offset = PythonValueConverter.ToDouble(offset, "offset");
            item.KeepModelEdges = keep_model_edges;
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Adds a Gmsh tetrahedral mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, algorithm_2d: str = None, " +
                               "algorithm_3d: str = None, recombine_min_quality: float = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_tetrahedral_gmsh(string name, SelectionAPI selection, string algorithm_2d = null,
                                                     string algorithm_3d = null, object recombine_min_quality = null)
        {
            TetrahedralGmsh item = new TetrahedralGmsh(name);
            ApplyGmshParameters(item, algorithm_2d, algorithm_3d, recombine_min_quality);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Adds a Gmsh shell mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, algorithm_2d: str = None, " +
                               "algorithm_3d: str = None, recombine_min_quality: float = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_shell_gmsh(string name, SelectionAPI selection, string algorithm_2d = null,
                                               string algorithm_3d = null, object recombine_min_quality = null)
        {
            ShellGmsh item = new ShellGmsh(name);
            ApplyGmshParameters(item, algorithm_2d, algorithm_3d, recombine_min_quality);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Adds a Gmsh transfinite mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, allow_pyramid_elements: bool = False, " +
                               "algorithm_2d: str = None, algorithm_3d: str = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_transfinite_mesh(string name, SelectionAPI selection,
                                                     bool allow_pyramid_elements = false, string algorithm_2d = null,
                                                     string algorithm_3d = null)
        {
            TransfiniteMesh item = new TransfiniteMesh(name);
            item.AllowPyramidElements = allow_pyramid_elements;
            ApplyGmshParameters(item, algorithm_2d, algorithm_3d, null);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Adds a Gmsh extrude mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, direction: list[float] = None, " +
                               "extrude_center: list[float] = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_extrude_mesh(string name, SelectionAPI selection, object direction = null,
                                                 object extrude_center = null)
        {
            ExtrudeMesh item = new ExtrudeMesh(name);
            if (direction != null) item.Direction = ToVector(direction, "direction");
            if (extrude_center != null) item.ExtrudeCenter = ToVector(extrude_center, "extrude_center");
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Adds a Gmsh revolve mesh item for the selected geometry. The angle is in degrees.",
                   Arguments = "name: str, selection: Selection, axis_center: list[float] = None, " +
                               "axis_direction: list[float] = None, angle: float = None, middle_r: float = None",
                   Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_revolve_mesh(string name, SelectionAPI selection, object axis_center = null,
                                                 object axis_direction = null, object angle = null,
                                                 object middle_r = null)
        {
            RevolveMesh item = new RevolveMesh(name);
            if (axis_center != null) item.AxisCenter = ToVector(axis_center, "axis_center");
            if (axis_direction != null) item.AxisDirection = ToVector(axis_direction, "axis_direction");
            if (angle != null) item.AngleDeg = PythonValueConverter.ToDouble(angle, "angle");
            if (middle_r != null) item.MiddleR = PythonValueConverter.ToDouble(middle_r, "middle_r");
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Adds a Gmsh sweep mesh item for the selected source surfaces. The sweep direction, " +
                   "the side surfaces and the layer edges are derived by the model from the selection.",
                   Arguments = "name: str, selection: Selection, number_of_global_smooth_steps: int = 0, " +
                               "algorithm_2d: str = None, algorithm_3d: str = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_sweep_mesh(string name, SelectionAPI selection,
                                               int number_of_global_smooth_steps = 0, string algorithm_2d = null,
                                               string algorithm_3d = null)
        {
            SweepMesh item = new SweepMesh(name);
            item.NumberOfGlobalSmoothSteps = number_of_global_smooth_steps;
            ApplyGmshParameters(item, algorithm_2d, algorithm_3d, null);
            _service.Add(item, name, selection);
            return this[name];
        }

        [PythonAPI("Adds a QuadWild shell mesh item for the selected geometry.",
                   Arguments = "name: str, selection: Selection, preview: bool = False, remesh: bool = True, " +
                               "smooth: bool = True, alpha: float = None", Returns = "MeshSetupItem")]
        public MeshSetupItemAPI add_shell_quad_wild(string name, SelectionAPI selection, bool preview = false,
                                                    bool remesh = true, bool smooth = true, object alpha = null)
        {
            ShellQuadWild item = new ShellQuadWild(name);
            item.Preview = preview;
            item.Remesh = remesh;
            item.Smooth = smooth;
            if (alpha != null) item.Alpha = PythonValueConverter.ToDouble(alpha, "alpha");
            _service.Add(item, name, selection);
            return this[name];
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
        private static void ApplyMeshingParameters(MeshingParameters parameters, object maxSize, object minSize,
                                                   object fineness, object grading, object elementsPerEdge,
                                                   object elementsPerCurve, object secondOrder, object quadDominated,
                                                   object midsideNodesOnGeometry, object useMmg,
                                                   object keepModelEdges)
        {
            if (maxSize != null) parameters.MaxH = PythonValueConverter.ToDouble(maxSize, "max_size");
            if (minSize != null) parameters.MinH = PythonValueConverter.ToDouble(minSize, "min_size");
            if (fineness != null) parameters.Fineness = PythonValueConverter.ToDouble(fineness, "fineness");
            if (grading != null) parameters.Grading = PythonValueConverter.ToDouble(grading, "grading");
            if (elementsPerEdge != null)
                parameters.ElementsPerEdge = PythonValueConverter.ToDouble(elementsPerEdge, "elements_per_edge");
            if (elementsPerCurve != null)
                parameters.ElementsPerCurve = PythonValueConverter.ToDouble(elementsPerCurve, "elements_per_curve");
            if (secondOrder != null) parameters.SecondOrder = ToBool(secondOrder, "second_order");
            if (quadDominated != null) parameters.QuadDominated = ToBool(quadDominated, "quad_dominated");
            if (midsideNodesOnGeometry != null)
                parameters.MidsideNodesOnGeometry = ToBool(midsideNodesOnGeometry, "midside_nodes_on_geometry");
            if (useMmg != null) parameters.UseMmg = ToBool(useMmg, "use_mmg");
            if (keepModelEdges != null) parameters.KeepModelEdges = ToBool(keepModelEdges, "keep_model_edges");
        }
        private static void ApplyGmshParameters(GmshSetupItem item, string algorithm2D, string algorithm3D,
                                                object recombineMinQuality)
        {
            if (algorithm2D != null)
                item.AlgorithmMesh2D = PythonValueConverter.ToEnum<GmshAlgorithmMesh2DEnum>(algorithm2D,
                                                                                            "algorithm_2d");
            if (algorithm3D != null)
                item.AlgorithmMesh3D = PythonValueConverter.ToEnum<GmshAlgorithmMesh3DEnum>(algorithm3D,
                                                                                            "algorithm_3d");
            if (recombineMinQuality != null)
                item.RecombineMinQuality = PythonValueConverter.ToDouble(recombineMinQuality,
                                                                         "recombine_min_quality");
        }
        private static bool ToBool(object value, string argumentName)
        {
            if (value is bool) return (bool)value;
            //
            throw new PythonAPIException("The argument '" + argumentName + "' must be True or False.");
        }
        private static double[] ToVector(object value, string argumentName)
        {
            double[] vector = PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (vector == null || vector.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            //
            return vector;
        }
    }
}
