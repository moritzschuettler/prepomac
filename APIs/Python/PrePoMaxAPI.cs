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
        private readonly ViewAPI _view;
        private readonly IPythonJobGateway _jobGateway;
        private readonly IPythonModelGateway _modelGateway;


        // Properties
        [PythonAPI("The model tree of the currently open model.", Returns = "Model")]
        public ModelAPI model { get { return _model; } }
        [PythonAPI("Loaded result names and the currently selected result.", Returns = "Results")]
        public ResultsAPI results { get { return _results; } }
        [PythonAPI("Highlights selections in the viewport and clears the highlight; pmx.create makes the " +
                   "selections.", Returns = "SelectionFactory")]
        public SelectionFactoryAPI selection { get { return _selection; } }
        [PythonAPI("Creates the detached parts that model items take as arguments.", Returns = "Create")]
        public CreateAPI create { get { return _create; } }
        [PythonAPI("The analyses of the model.", Returns = "Jobs")]
        public JobsAPI jobs { get { return _jobs; } }
        [PythonAPI("What the application shows - the displayed tree, the camera, the plotted result - and a " +
                   "picture of it. None of it is a model change, so none of it is recorded.", Returns = "View")]
        public ViewAPI view { get { return _view; } }
        [PythonAPI("The work directory of the session: the -w of a script or regeneration run, otherwise " +
                   "the one the settings give, where an analysis without a work directory of its own runs and " +
                   "where a replayed journal writes the files its session saved or exported. Read only. A " +
                   "session without one raises, telling to pass -w or set it in Settings - Solver.",
                   Returns = "str")]
        public string work_directory
        {
            get
            {
                string workDirectory = _jobGateway == null ? null : _jobGateway.GetWorkDirectory();
                if (string.IsNullOrWhiteSpace(workDirectory))
                    throw new PythonAPIException("pmx.work_directory: no work directory: pass -w or set it in " +
                                                 "Settings - Solver");
                return workDirectory;
            }
        }
        [PythonAPI("True while a recorded script runs again rather than on the run that recorded it: Regenerate, " +
                   "Undo, Redo, OK of Edit History or a batch regeneration (-r) re-running it. A script uses it to " +
                   "accept what its own first run left behind, as a journal's _output accepts the files that run " +
                   "wrote. False on the first run, in the console and in an automation call. Read only.",
                   Returns = "bool")]
        public bool is_rerun
        {
            get { return _modelGateway != null && _modelGateway.IsRerunningRecordedScript(); }
        }


        // Constructors
        public PrePoMaxAPI(ResultsCollection results)
            : this(results, null, null, null, null, null, null)
        {
        }
        public PrePoMaxAPI(ResultsCollection results, IPythonCommandGateway commandGateway,
                           IPythonSelectionGateway selectionGateway, IPythonModelGateway modelGateway,
                           IPythonMeshGateway meshGateway, IPythonJobGateway jobGateway,
                           IPythonViewGateway viewGateway)
        {
            _resolver = new ResultResolver(results);
            _modelResolver = new ModelResolver(modelGateway);
            PythonFileNameResolver fileNameResolver = new PythonFileNameResolver(jobGateway);
            MeshResolver meshResolver = new MeshResolver(_resolver, selectionGateway);
            SelectionService selectionService = new SelectionService(meshResolver, selectionGateway);
            ResultFieldOutputService resultFieldOutputService =
                new ResultFieldOutputService(_resolver, commandGateway, fileNameResolver);
            ResultHistoryOutputService resultHistoryOutputService =
                new ResultHistoryOutputService(_resolver, selectionService, commandGateway, fileNameResolver);
            _documentService = new DocumentService(_modelResolver, modelGateway, _resolver, jobGateway,
                                                   fileNameResolver);
            ParameterService parameterService = new ParameterService(_modelResolver, modelGateway, fileNameResolver);
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
                new StepContentServices(_modelResolver, selectionService, stepService, modelGateway,
                                        fileNameResolver);
            FieldServices fieldServices =
                new FieldServices(_modelResolver, selectionService, modelGateway, fileNameResolver);
            SectionService sectionService =
                new SectionService(_modelResolver, selectionService, modelGateway);
            GeometryPartService geometryPartService =
                new GeometryPartService(_modelResolver, selectionService, meshGateway, modelGateway);
            ModelPartService modelPartService =
                new ModelPartService(_modelResolver, selectionService, meshGateway, modelGateway);
            MeshSetupItemService meshSetupItemService =
                new MeshSetupItemService(_modelResolver, selectionService, meshGateway, modelGateway);
            MeshToolService meshToolService =
                new MeshToolService(_modelResolver, selectionService, meshGateway, fileNameResolver);
            MeshEditService meshEditService =
                new MeshEditService(_modelResolver, selectionService, meshGateway);
            _results = new ResultsAPI(_resolver, resultFieldOutputService, resultHistoryOutputService,
                                      selectionService, commandGateway, _documentService);
            _model = new ModelAPI(_modelResolver, _documentService, parameterService, geometryPartService,
                                  modelPartService, meshSetupItemService, meshToolService,
                                  meshEditService, materialService,
                                  sectionService, fieldServices, regionServices, stepService,
                                  stepContentServices, surfaceInteractionService, contactPairService,
                                  constraintService);
            _selection = new SelectionFactoryAPI(selectionService);
            _create = new CreateAPI(selectionService);
            _jobs = new JobsAPI(new JobService(jobGateway));
            _view = new ViewAPI(viewGateway);
            _jobGateway = jobGateway;
            _modelGateway = modelGateway;
        }


        // Methods
        [PythonAPI("Creates a new empty model. Not available inside a recorded script, because it replaces " +
                   "the command history. Refused while an analysis is running, naming it. Undefined is refused " +
                   "for either argument. In a session with a window, a model with unsaved changes is refused " +
                   "unless discard_changes is True: save it with pmx.save first, or ask the user before " +
                   "discarding. A session without a window discards them.",
                   Arguments = "model_space: str, unit_system: str, discard_changes: bool = False", Returns = "None",
                   Example = "pmx.new_model('ThreeD', 'MM_TON_S_C')")]
        public void new_model(object model_space, object unit_system, object discard_changes = null)
        {
            string modelSpace = PythonValueConverter.ToText(model_space, "model_space", false);
            string unitSystem = PythonValueConverter.ToText(unit_system, "unit_system", false);
            bool discardChanges = PythonValueConverter.ToBoolOrDefault(discard_changes, false, "discard_changes");
            _documentService.NewModel(modelSpace, unitSystem, discardChanges);
        }

        [PythonAPI("Opens a .pmx or .pmh model file. Not available inside a recorded script, because it " +
                   "replaces the command history. Refused while an analysis is running, naming it. A result file " +
                   "is opened by pmx.results.open. A relative file_name is taken from pmx.work_directory. In a " +
                   "session with a window, a model with unsaved changes is refused unless discard_changes is " +
                   "True: save it with pmx.save first, or ask the user before discarding. A session without a " +
                   "window discards them.",
                   Arguments = "file_name: str, discard_changes: bool = False", Returns = "None")]
        public void open(object file_name, object discard_changes = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            bool discardChanges = PythonValueConverter.ToBoolOrDefault(discard_changes, false, "discard_changes");
            _documentService.OpenFile(fileName, discardChanges);
        }

        [PythonAPI("Imports a geometry or mesh file into the current model using command history. The import " +
                   "takes .stp, .step, .igs, .iges, .brep, .stl, .unv, .vol, .inp, .mesh, .obj and .pmp files; " +
                   "only_materials imports the materials of an .inp file and is refused for another type. A model " +
                   "whose model space or unit system is undefined is refused, as the dialog refuses to import " +
                   "without a model: create one with new_model, or set both model properties first.",
                   Arguments = "file_name: str, only_materials: bool = False", Returns = "None")]
        public void import_file(object file_name, object only_materials = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            bool onlyMaterials = PythonValueConverter.ToBoolOrDefault(only_materials, false, "only_materials");
            _documentService.ImportFile(fileName, onlyMaterials);
        }

        [PythonAPI("Saves the model to a .pmx file using command history.", Arguments = "file_name: str",
                   Returns = "None")]
        public void save(object file_name)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            _documentService.SaveToPmx(fileName);
        }

        [PythonAPI("Exports the model to a CalculiX .inp file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void export_to_calculix(object file_name)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            _documentService.ExportToCalculix(fileName);
        }

        [PythonAPI("Exports the model to an Abaqus .inp file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void export_to_abaqus(object file_name)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            _documentService.ExportToAbaqus(fileName);
        }

        [PythonAPI("Exports the model to a .3mf file using command history. The edge thickness ratio must be " +
                   "finite and larger than zero.",
                   Arguments = "file_name: str, edge_thickness_ratio: float = 1", Returns = "None")]
        public void export_to_3mf(object file_name, object edge_thickness_ratio = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            _documentService.ExportTo3mf(fileName, edge_thickness_ratio);
        }

        [PythonAPI("Exports the named geometry parts using command history. The file extension picks the format: " +
                   ".stp or .step and .brep need CAD parts, .stl takes any geometry part and .mesh (Mmg) a solid or " +
                   "shell part. Give 'format' to override the extension.",
                   Arguments = "part_names: list[str] or str, file_name: str, format: str = None", Returns = "None")]
        public void export_geometry(object part_names, object file_name, object format = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            string formatText = PythonValueConverter.ToText(format, "format", true);
            _documentService.ExportGeometry(part_names, fileName, formatText);
        }

        [PythonAPI("Exports the nodes and elements of the named mesh parts using command history. The file " +
                   "extension picks the format: .inp is CalculiX unless format='Abaqus', .msh is Gmsh, .mesh is Mmg " +
                   "for a solid or shell part and .stl the visualization surface. Give 'format' to override the extension.",
                   Arguments = "part_names: list[str] or str, file_name: str, format: str = None", Returns = "None")]
        public void export_mesh(object part_names, object file_name, object format = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            string formatText = PythonValueConverter.ToText(format, "format", true);
            _documentService.ExportMesh(part_names, fileName, formatText);
        }

        [PythonAPI("Exports the whole model as a solver input deck using command history. An .inp file is " +
                   "CalculiX unless format='Abaqus'.",
                   Arguments = "file_name: str, format: str = None", Returns = "None")]
        public void export_model(object file_name, object format = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            string formatText = PythonValueConverter.ToText(format, "format", true);
            _documentService.ExportModel(fileName, formatText);
        }

        [PythonAPI("Exports the named parts of the current result in their deformed shape using command history. " +
                   "The file extension picks the format: .inp is a CalculiX mesh, .stl the deformed visualization " +
                   "surface. Give 'format' to override the extension.",
                   Arguments = "part_names: list[str] or str, file_name: str, format: str = None", Returns = "None")]
        public void export_results(object part_names, object file_name, object format = null)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            string formatText = PythonValueConverter.ToText(format, "format", true);
            _documentService.ExportResults(part_names, fileName, formatText);
        }
        public override string ToString()
        {
            return "PrePoMaxAPI(model=" + (_model.is_open() ? "open" : "none") +
                   ", results=" + _resolver.GetResultNames().Length + ")";
        }
    }
}
