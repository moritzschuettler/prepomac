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
    // The services of the collections that live inside a step. They are handed over as one object, so
    // that adding a step scoped collection does not change the signatures above it.
    internal class StepContentServices
    {
        // Variables
        private readonly BoundaryConditionService _boundaryConditions;
        private readonly LoadService _loads;
        private readonly FieldOutputService _fieldOutputs;
        private readonly HistoryOutputService _historyOutputs;
        private readonly DefinedFieldService _definedFields;


        // Properties
        public BoundaryConditionService BoundaryConditions { get { return _boundaryConditions; } }
        public LoadService Loads { get { return _loads; } }
        public FieldOutputService FieldOutputs { get { return _fieldOutputs; } }
        public HistoryOutputService HistoryOutputs { get { return _historyOutputs; } }
        public DefinedFieldService DefinedFields { get { return _definedFields; } }


        // Constructors
        public StepContentServices(ModelResolver modelResolver, SelectionService selectionService,
                                   StepService stepService, IPythonModelGateway modelGateway,
                                   PythonFileNameResolver fileNameResolver)
        {
            _boundaryConditions =
                new BoundaryConditionService(modelResolver, selectionService, stepService, modelGateway);
            _loads = new LoadService(modelResolver, selectionService, stepService, modelGateway, fileNameResolver);
            _fieldOutputs =
                new FieldOutputService(modelResolver, selectionService, stepService, modelGateway);
            _historyOutputs =
                new HistoryOutputService(modelResolver, selectionService, stepService, modelGateway);
            _definedFields =
                new DefinedFieldService(modelResolver, selectionService, stepService, modelGateway, fileNameResolver);
        }
    }
}
