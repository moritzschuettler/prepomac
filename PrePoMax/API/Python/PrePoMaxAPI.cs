// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
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
        [PythonAPI("Loaded PrePoMax results selected explicitly by result name.", Returns = "Results")]
        public ResultsAPI results { get { return _results; } }


        // Constructors
        public PrePoMaxAPI(ResultsCollection results)
        {
            _resolver = new ResultResolver(results);
            _results = new ResultsAPI(_resolver);
        }


        // Methods
        public override string ToString()
        {
            return "PrePoMaxAPI(results=" + _resolver.GetResultNames().Length + ")";
        }
    }
}
