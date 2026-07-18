// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace PrePoMax.API.Python
{
    public class PrePoMaxAPI : BaseAPI
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly ResultsAPI _results;


        // Properties
        [PythonAPI("Loaded result names and the currently selected result.", Returns = "Results")]
        public ResultsAPI results { get { return _results; } }


        // Constructors
        public PrePoMaxAPI(ResultsCollection results)
            : this(results, null)
        {
        }
        internal PrePoMaxAPI(ResultsCollection results, IPythonCommandGateway commandGateway)
        {
            _resolver = new ResultResolver(results);
            ResultFieldOutputService resultFieldOutputService = new ResultFieldOutputService(_resolver, commandGateway);
            _results = new ResultsAPI(_resolver, resultFieldOutputService, commandGateway);
        }


        // Methods
        public override string ToString()
        {
            return "PrePoMaxAPI(results=" + _resolver.GetResultNames().Length + ")";
        }
    }
}
