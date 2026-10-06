// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    // The root of the model tree. It mirrors the application model tree, so results, jobs, selection
    // and create are siblings of this object, not members of it.
    public class ModelAPI : BaseAPI
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly DocumentService _documentService;
        private readonly ModelPropertiesAPI _properties;
        private readonly ParametersAPI _parameters;
        private readonly GeometryAPI _geometry;
        private readonly MeshAPI _mesh;
        private readonly ModelPartsAPI _parts;
        private readonly NodesAPI _nodes;
        private readonly ElementsAPI _elements;
        private readonly AmplitudesAPI _amplitudes;
        private readonly DistributionsAPI _distributions;
        private readonly InitialConditionsAPI _initialConditions;
        private readonly MaterialsAPI _materials;
        private readonly SectionsAPI _sections;
        private readonly NodeSetsAPI _nodeSets;
        private readonly ElementSetsAPI _elementSets;
        private readonly SurfacesAPI _surfaces;
        private readonly ReferencePointsAPI _referencePoints;
        private readonly CoordinateSystemsAPI _coordinateSystems;
        private readonly StepsAPI _steps;
        private readonly ConstraintsAPI _constraints;
        private readonly SurfaceInteractionsAPI _surfaceInteractions;
        private readonly ContactPairsAPI _contactPairs;
        private readonly KeywordsAPI _keywords;
        private readonly TreeAPI _tree;


        // Properties
        [PythonAPI("The model name.", Returns = "str")]
        public string name { get { return _documentService.GetModelName(); } }
        [PythonAPI("The model name, model space and unit system.", Returns = "ModelProperties")]
        public ModelPropertiesAPI properties { get { return _properties; } }
        [PythonAPI("The equation parameters of the model.", Returns = "Parameters")]
        public ParametersAPI parameters { get { return _parameters; } }
        [PythonAPI("The geometry branch of the model.", Returns = "Geometry")]
        public GeometryAPI geometry { get { return _geometry; } }
        [PythonAPI("The meshing branch of the model.", Returns = "Mesh")]
        public MeshAPI mesh { get { return _mesh; } }
        [PythonAPI("The nodes of the model mesh.", Returns = "Nodes")]
        public NodesAPI nodes { get { return _nodes; } }
        [PythonAPI("The elements of the model mesh.", Returns = "Elements")]
        public ElementsAPI elements { get { return _elements; } }
        [PythonAPI("The mesh parts of the model.", Returns = "ModelParts")]
        public ModelPartsAPI parts { get { return _parts; } }
        [PythonAPI("The amplitudes of the model.", Returns = "Amplitudes")]
        public AmplitudesAPI amplitudes { get { return _amplitudes; } }
        [PythonAPI("The distributions of the model.", Returns = "Distributions")]
        public DistributionsAPI distributions { get { return _distributions; } }
        [PythonAPI("The initial conditions of the model.", Returns = "InitialConditions")]
        public InitialConditionsAPI initial_conditions { get { return _initialConditions; } }
        [PythonAPI("The materials of the model.", Returns = "Materials")]
        public MaterialsAPI materials { get { return _materials; } }
        [PythonAPI("The sections of the model.", Returns = "Sections")]
        public SectionsAPI sections { get { return _sections; } }
        [PythonAPI("The node sets of the model.", Returns = "NodeSets")]
        public NodeSetsAPI node_sets { get { return _nodeSets; } }
        [PythonAPI("The element sets of the model.", Returns = "ElementSets")]
        public ElementSetsAPI element_sets { get { return _elementSets; } }
        [PythonAPI("The surfaces of the model.", Returns = "Surfaces")]
        public SurfacesAPI surfaces { get { return _surfaces; } }
        [PythonAPI("The reference points of the model.", Returns = "ReferencePoints")]
        public ReferencePointsAPI reference_points { get { return _referencePoints; } }
        [PythonAPI("The coordinate systems of the model.", Returns = "CoordinateSystems")]
        public CoordinateSystemsAPI coordinate_systems { get { return _coordinateSystems; } }
        [PythonAPI("The constraints of the model.", Returns = "Constraints")]
        public ConstraintsAPI constraints { get { return _constraints; } }
        [PythonAPI("The surface interactions of the model.", Returns = "SurfaceInteractions")]
        public SurfaceInteractionsAPI surface_interactions { get { return _surfaceInteractions; } }
        [PythonAPI("The contact pairs of the model.", Returns = "ContactPairs")]
        public ContactPairsAPI contact_pairs { get { return _contactPairs; } }
        [PythonAPI("The user CalculiX keyword editor state.", Returns = "Keywords")]
        public KeywordsAPI keywords { get { return _keywords; } }
        [PythonAPI("The editable model tree layout.", Returns = "Tree")]
        public TreeAPI tree { get { return _tree; } }
        [PythonAPI("The analysis steps of the model.", Returns = "Steps")]
        public StepsAPI steps { get { return _steps; } }


        // Constructors
        internal ModelAPI(ModelResolver modelResolver, DocumentService documentService,
                          ParameterService parameterService, GeometryPartService geometryPartService,
                          ModelPartService modelPartService, MeshSetupItemService meshSetupItemService,
                          MeshToolService meshToolService, MeshEditService meshEditService,
                          MaterialService materialService, SectionService sectionService,
                          FieldServices fieldServices,
                          RegionServices regionServices, StepService stepService,
                          StepContentServices stepContentServices,
                          SurfaceInteractionService surfaceInteractionService,
                          ContactPairService contactPairService, ConstraintService constraintService)
        {
            _modelResolver = modelResolver;
            _documentService = documentService;
            _properties = new ModelPropertiesAPI(documentService);
            _parameters = new ParametersAPI(parameterService);
            _geometry = new GeometryAPI(geometryPartService);
            _mesh = new MeshAPI(meshSetupItemService, meshToolService);
            _parts = new ModelPartsAPI(modelPartService);
            _nodes = new NodesAPI(meshEditService);
            _elements = new ElementsAPI(meshEditService);
            _amplitudes = new AmplitudesAPI(fieldServices.Amplitudes);
            _distributions = new DistributionsAPI(fieldServices.Distributions);
            _initialConditions = new InitialConditionsAPI(fieldServices.InitialConditions);
            _materials = new MaterialsAPI(materialService);
            _sections = new SectionsAPI(sectionService);
            _nodeSets = new NodeSetsAPI(regionServices.NodeSets);
            _elementSets = new ElementSetsAPI(regionServices.ElementSets, meshEditService);
            _surfaces = new SurfacesAPI(regionServices.Surfaces);
            _referencePoints = new ReferencePointsAPI(regionServices.ReferencePoints);
            _coordinateSystems = new CoordinateSystemsAPI(regionServices.CoordinateSystems);
            _steps = new StepsAPI(stepService, stepContentServices);
            _constraints = new ConstraintsAPI(constraintService);
            _surfaceInteractions = new SurfaceInteractionsAPI(surfaceInteractionService);
            _contactPairs = new ContactPairsAPI(contactPairService);
            _keywords = new KeywordsAPI(_modelResolver);
            _tree = new TreeAPI(_modelResolver);
        }


        // Methods
        [PythonAPI("Returns true when a model is open.", Returns = "bool")]
        public bool is_open()
        {
            return _modelResolver.ModelGateway != null && _modelResolver.ModelGateway.GetModel() != null;
        }
        public override string ToString()
        {
            if (!is_open()) return "Model(open=False)";
            //
            FeModel model = _modelResolver.GetModel();
            return "Model(name=" + StringListAPI.ToDisplayString(model.Name) +
                   ", geometry_parts=" + _geometry.parts.Count + ", parts=" + _parts.Count +
                   ", parameters=" + _parameters.Count + ", node_sets=" + _nodeSets.Count + ")";
        }
    }
}


