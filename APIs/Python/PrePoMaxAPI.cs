// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python.Model;
using CaeResults;

namespace APIs.Python
{
    public class PrePoMaxAPI : BaseAPI
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly ModelResolver _modelResolver;
        private readonly DocumentService _documentService;
        private readonly ResultsAPI _results;
        private readonly ModelAPI _model;
        private readonly SelectionFactoryAPI _selection;
        private readonly CreateAPI _create;
        private readonly JobsAPI _jobs;


        // Properties
        [PythonAPI("The model tree of the currently open model.", Returns = "Model")]
        public ModelAPI model { get { return _model; } }
        [PythonAPI("Loaded result names and the currently selected result.", Returns = "Results")]
        public ResultsAPI results { get { return _results; } }
        [PythonAPI("Creates and highlights reusable view-aware selections.", Returns = "SelectionFactory")]
        public SelectionFactoryAPI selection { get { return _selection; } }
        [PythonAPI("Creates the detached parts that model items take as arguments.", Returns = "Create")]
        public CreateAPI create { get { return _create; } }
        [PythonAPI("The analyses of the model.", Returns = "Jobs")]
        public JobsAPI jobs { get { return _jobs; } }


        // Constructors
        public PrePoMaxAPI(ResultsCollection results)
            : this(results, null, null, null, null, null)
        {
        }
        public PrePoMaxAPI(ResultsCollection results, IPythonCommandGateway commandGateway,
                           IPythonSelectionGateway selectionGateway, IPythonModelGateway modelGateway,
                           IPythonMeshGateway meshGateway, IPythonJobGateway jobGateway)
        {
            _resolver = new ResultResolver(results);
            _modelResolver = new ModelResolver(modelGateway);
            MeshResolver meshResolver = new MeshResolver(_resolver, selectionGateway);
            SelectionService selectionService = new SelectionService(meshResolver, selectionGateway);
            ResultFieldOutputService resultFieldOutputService = new ResultFieldOutputService(_resolver, commandGateway);
            ResultHistoryOutputService resultHistoryOutputService =
                new ResultHistoryOutputService(_resolver, selectionService, commandGateway);
            _documentService = new DocumentService(_modelResolver, modelGateway, _resolver);
            ParameterService parameterService = new ParameterService(_modelResolver, modelGateway);
            RegionServices regionServices = new RegionServices(_modelResolver, selectionService, modelGateway);
            MaterialService materialService = new MaterialService(_modelResolver, modelGateway);
            StepService stepService = new StepService(_modelResolver, modelGateway);
            SurfaceInteractionService surfaceInteractionService =
                new SurfaceInteractionService(_modelResolver, modelGateway);
            ConstraintService constraintService =
                new ConstraintService(_modelResolver, selectionService, modelGateway);
            ContactPairService contactPairService =
                new ContactPairService(_modelResolver, selectionService, surfaceInteractionService,
                                       modelGateway);
            StepContentServices stepContentServices =
                new StepContentServices(_modelResolver, selectionService, stepService, modelGateway);
            FieldServices fieldServices = new FieldServices(_modelResolver, selectionService, modelGateway);
            SectionService sectionService =
                new SectionService(_modelResolver, selectionService, modelGateway);
            GeometryPartService geometryPartService =
                new GeometryPartService(_modelResolver, selectionService, meshGateway, modelGateway);
            ModelPartService modelPartService =
                new ModelPartService(_modelResolver, selectionService, meshGateway, modelGateway);
            MeshSetupItemService meshSetupItemService =
                new MeshSetupItemService(_modelResolver, selectionService, meshGateway, modelGateway);
            MeshToolService meshToolService =
                new MeshToolService(_modelResolver, selectionService, meshGateway);
            MeshEditService meshEditService =
                new MeshEditService(_modelResolver, selectionService, meshGateway);
            _results = new ResultsAPI(_resolver, resultFieldOutputService, resultHistoryOutputService,
                                      selectionService, commandGateway);
            _model = new ModelAPI(_modelResolver, _documentService, parameterService, geometryPartService,
                                  modelPartService, meshSetupItemService, meshToolService,
                                  meshEditService, materialService,
                                  sectionService, fieldServices, regionServices, stepService,
                                  stepContentServices, surfaceInteractionService, contactPairService,
                                  constraintService);
            _selection = new SelectionFactoryAPI(selectionService);
            _create = new CreateAPI(selectionService);
            _jobs = new JobsAPI(new JobService(jobGateway));
        }


        // Methods
        [PythonAPI("Creates a new empty model. Not available inside a recorded script, because it replaces " +
                   "the command history.",
                   Arguments = "model_space: str, unit_system: str", Returns = "None",
                   Example = "pmx.new('ThreeD', 'MM_TON_S_C')")]
        public void new_model(string model_space, string unit_system)
        {
            _documentService.NewModel(model_space, unit_system);
        }

        [PythonAPI("Opens a .pmx, .pmh or .frd file. Not available inside a recorded script, because it " +
                   "replaces the command history.", Arguments = "file_name: str", Returns = "None")]
        public void open(string file_name)
        {
            _documentService.OpenFile(file_name);
        }

        [PythonAPI("Imports a geometry or mesh file into the current model using command history.",
                   Arguments = "file_name: str, only_materials: bool = False", Returns = "None")]
        public void import_file(string file_name, bool only_materials = false)
        {
            _documentService.ImportFile(file_name, only_materials);
        }

        [PythonAPI("Saves the model to a .pmx file using command history.", Arguments = "file_name: str",
                   Returns = "None")]
        public void save(string file_name)
        {
            _documentService.SaveToPmx(file_name);
        }

        [PythonAPI("Exports the model to a CalculiX .inp file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void export_to_calculix(string file_name)
        {
            _documentService.ExportToCalculix(file_name);
        }

        [PythonAPI("Exports the model to an Abaqus .inp file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void export_to_abaqus(string file_name)
        {
            _documentService.ExportToAbaqus(file_name);
        }

        [PythonAPI("Exports the named parts to an .stl file using command history.",
                   Arguments = "part_names: list[str] or str, file_name: str", Returns = "None")]
        public void export_to_stl(object part_names, string file_name)
        {
            _documentService.ExportToStl(part_names, file_name);
        }

        [PythonAPI("Exports the model to a .3mf file using command history.",
                   Arguments = "file_name: str, edge_thickness_ratio: float = 1", Returns = "None")]
        public void export_to_3mf(string file_name, object edge_thickness_ratio = null)
        {
            _documentService.ExportTo3mf(file_name, edge_thickness_ratio);
        }

        [PythonAPI("Exports the meshed parts to a Gmsh .msh file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void export_parts_as_gmsh_mesh(string file_name)
        {
            _documentService.ExportPartsAsGmshMesh(file_name);
        }

        [PythonAPI("Exports the named parts to an Mmg .mesh file using command history.",
                   Arguments = "part_names: list[str] or str, file_name: str, combine: bool = False",
                   Returns = "None")]
        public void export_parts_as_mmg_mesh(object part_names, string file_name, bool combine = false)
        {
            _documentService.ExportPartsAsMmgMesh(part_names, file_name, combine);
        }

        [PythonAPI("Exports the named CAD parts to a .step file using command history.",
                   Arguments = "part_names: list[str] or str, file_name: str", Returns = "None")]
        public void export_cad_parts_as_step(object part_names, string file_name)
        {
            _documentService.ExportCadPartsAsStep(part_names, file_name);
        }

        [PythonAPI("Exports the named CAD parts to a .brep file using command history.",
                   Arguments = "part_names: list[str] or str, file_name: str", Returns = "None")]
        public void export_cad_parts_as_brep(object part_names, string file_name)
        {
            _documentService.ExportCadPartsAsBrep(part_names, file_name);
        }

        [PythonAPI("Exports the named deformed parts to a CalculiX .inp file using command history.",
                   Arguments = "part_names: list[str] or str, file_name: str", Returns = "None")]
        public void export_deformed_parts_to_calculix(object part_names, string file_name)
        {
            _documentService.ExportDeformedPartsToCalculix(part_names, file_name);
        }
        public override string ToString()
        {
            return "PrePoMaxAPI(model=" + (_model.is_open() ? "open" : "none") +
                   ", results=" + _resolver.GetResultNames().Length + ")";
        }
    }
}
