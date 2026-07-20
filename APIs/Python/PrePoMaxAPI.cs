// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class PrePoMaxAPI : BaseAPI
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly ResultsAPI _results;
        private readonly SelectionFactoryAPI _selection;


        // Properties
        [PythonAPI("Loaded result names and the currently selected result.", Returns = "Results")]
        public ResultsAPI results { get { return _results; } }
        [PythonAPI("Creates and highlights reusable view-aware selections.", Returns = "SelectionFactory")]
        public SelectionFactoryAPI selection { get { return _selection; } }


        // Constructors
        public PrePoMaxAPI(ResultsCollection results)
            : this(results, null, null)
        {
        }
        public PrePoMaxAPI(ResultsCollection results, IPythonCommandGateway commandGateway,
                           IPythonSelectionGateway selectionGateway)
        {
            _resolver = new ResultResolver(results);
            SelectionService selectionService = new SelectionService(_resolver, selectionGateway);
            ResultFieldOutputService resultFieldOutputService = new ResultFieldOutputService(_resolver, commandGateway);
            ResultHistoryOutputService resultHistoryOutputService =
                new ResultHistoryOutputService(_resolver, selectionService, commandGateway);
            _results = new ResultsAPI(_resolver, resultFieldOutputService, resultHistoryOutputService, commandGateway);
            _selection = new SelectionFactoryAPI(selectionService);
        }


        // Methods
        public override string ToString()
        {
            return "PrePoMaxAPI(results=" + _resolver.GetResultNames().Length + ")";
        }
    }
}
