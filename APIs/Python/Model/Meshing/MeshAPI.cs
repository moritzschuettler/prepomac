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
    // The meshing branch of the model tree
    public class MeshAPI : BaseAPI
    {
        // Variables
        private readonly MeshSetupItemService _service;
        private readonly MeshToolService _toolService;
        private readonly MeshSetupItemsAPI _setupItems;


        // Properties
        [PythonAPI("The mesh setup items of the model.", Returns = "MeshSetupItems")]
        public MeshSetupItemsAPI setup_items { get { return _setupItems; } }


        // Constructors
        internal MeshAPI(MeshSetupItemService service, MeshToolService toolService)
        {
            _service = service;
            _toolService = toolService;
            _setupItems = new MeshSetupItemsAPI(service);
        }


        // Methods
        [PythonAPI("Meshes the named geometry parts using command history. The call returns when the meshing " +
                   "has finished. It reports every named part with the reason it was not meshed, and an empty " +
                   "reason for one that was, because meshing several parts can succeed for some of them. A " +
                   "call that meshes nothing at all raises instead, and records no command.",
                   Arguments = "part_names: list[str] or str", Returns = "dict[str, str]",
                   Example = "failed = {k: v for k, v in pmx.model.mesh.generate('Solid-1').items() if v}")]
        public Dictionary<string, string> generate(object part_names)
        {
            return _service.CreateMeshes(_service.ResolveMeshablePartNames(part_names));
        }

        [PythonAPI("Creates a boundary layer on the selected geometry using command history. thickness is " +
                   "larger than 0. As in the dialog, the model is in 3D model space.",
                   Arguments = "selection: Selection, thickness: float", Returns = "None")]
        public void generate_boundary_layer(object selection, object thickness)
        {
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            _toolService.CreateBoundaryLayer(selectionValue, thickness);
        }

        [PythonAPI("Remeshes the elements of a region using command history. The region is a selection or " +
                   "an ElementSetName region, which is what the dialog offers; the selection target is elements, " +
                   "as the dialog picks them, or geometry. Every parameter that is not " +
                   "given keeps the application default. max_size and min_size are at least 0, a min_size larger " +
                   "than the max_size it ends up with is refused naming both, and hausdorff is larger than 0.",
                   Arguments = "region: Selection or Region, max_size: float = None, " +
                               "min_size: float = None, hausdorff: float = None", Returns = "None")]
        public void remesh_elements(object region, object max_size = null, object min_size = null,
                                    object hausdorff = null)
        {
            _toolService.RemeshElements(region, max_size, min_size, hausdorff);
        }

        [PythonAPI("Thickens the shell mesh of the named parts using command history. thickness is larger " +
                   "than 0 and number_of_layers at least 1. As in the dialog, the model is in 3D model space " +
                   "and every named part is a manifold, consistently oriented shell part.",
                   Arguments = "part_names: list[str] or str, thickness: float, number_of_layers: int = 1, " +
                               "offset: float = 0, keep_model_edges: bool = False", Returns = "None")]
        public void thicken_shell_mesh(object part_names, object thickness, object number_of_layers = null,
                                       object offset = null, object keep_model_edges = null)
        {
            int numberOfLayers = PythonValueConverter.ToIntOrDefault(number_of_layers, 1, "number_of_layers");
            bool keepModelEdges = PythonValueConverter.ToBoolOrDefault(keep_model_edges, false, "keep_model_edges");
            _toolService.ThickenShellMesh(part_names, thickness, numberOfLayers, offset, keepModelEdges);
        }

        [PythonAPI("Splits a part mesh with a surface using command history. This is a one time operation " +
                   "rather than a model item. The base part region is a selection or a PartName region and " +
                   "the splitter surface region is a selection or a SurfaceName region, which is what the " +
                   "dialog offers. The base part selection target is parts, as the dialog picks them, or " +
                   "geometry, and the splitter surface selection target is surfaces. As in the dialog, the " +
                   "base part is a solid part meshed with tetrahedra only.",
                   Arguments = "base_part_region: Selection or Region, " +
                               "splitter_surface_region: Selection or Region, offset: float = None, " +
                               "max_size: float = None, min_size: float = None, hausdorff: float = None, " +
                               "exact: bool = False", Returns = "None")]
        public void split_part_mesh_using_surface(object base_part_region, object splitter_surface_region,
                                                  object offset = null, object max_size = null,
                                                  object min_size = null, object hausdorff = null,
                                                  object exact = null)
        {
            bool isExact = PythonValueConverter.ToBoolOrDefault(exact, false, "exact");
            _toolService.SplitPartMeshUsingSurface(base_part_region, splitter_surface_region, offset,
                                                   max_size, min_size, hausdorff, isExact);
        }

        [PythonAPI("Updates the nodal coordinates of the model mesh from a file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void update_nodal_coordinates_from_file(object file_name)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            _toolService.UpdateNodalCoordinatesFromFile(fileName);
        }
        public override string ToString()
        {
            return "Mesh(setup_items=" + _setupItems.Count + ")";
        }
    }
}
