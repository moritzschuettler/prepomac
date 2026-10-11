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
    // Keeps the amplitude, distribution and initial condition services together
    internal class FieldServices
    {
        // Variables
        private readonly AmplitudeService _amplitudes;
        private readonly DistributionService _distributions;
        private readonly InitialConditionService _initialConditions;


        // Properties
        public AmplitudeService Amplitudes { get { return _amplitudes; } }
        public DistributionService Distributions { get { return _distributions; } }
        public InitialConditionService InitialConditions { get { return _initialConditions; } }


        // Constructors
        public FieldServices(ModelResolver modelResolver, SelectionService selectionService,
                             IPythonModelGateway modelGateway, PythonFileNameResolver fileNameResolver)
        {
            _amplitudes = new AmplitudeService(modelResolver, selectionService, modelGateway);
            _distributions = new DistributionService(modelResolver, selectionService, modelGateway, fileNameResolver);
            _initialConditions = new InitialConditionService(modelResolver, selectionService, modelGateway);
        }
    }
}
