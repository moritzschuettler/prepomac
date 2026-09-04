// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python.Model
{
    // Keeps the five region services together, so that the model root does not take five more
    // constructor arguments for what is one area of the tree.
    internal class RegionServices
    {
        // Variables
        private readonly NodeSetService _nodeSets;
        private readonly ElementSetService _elementSets;
        private readonly SurfaceService _surfaces;
        private readonly ReferencePointService _referencePoints;
        private readonly CoordinateSystemService _coordinateSystems;


        // Properties
        public NodeSetService NodeSets { get { return _nodeSets; } }
        public ElementSetService ElementSets { get { return _elementSets; } }
        public SurfaceService Surfaces { get { return _surfaces; } }
        public ReferencePointService ReferencePoints { get { return _referencePoints; } }
        public CoordinateSystemService CoordinateSystems { get { return _coordinateSystems; } }


        // Constructors
        public RegionServices(ModelResolver modelResolver, SelectionService selectionService,
                              IPythonModelGateway modelGateway)
        {
            _nodeSets = new NodeSetService(modelResolver, selectionService, modelGateway);
            _elementSets = new ElementSetService(modelResolver, selectionService, modelGateway);
            _surfaces = new SurfaceService(modelResolver, selectionService, modelGateway);
            _referencePoints = new ReferencePointService(modelResolver, selectionService, modelGateway);
            _coordinateSystems = new CoordinateSystemService(modelResolver, selectionService, modelGateway);
        }
    }
}
