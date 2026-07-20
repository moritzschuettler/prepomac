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
    internal class ResultResolver
    {
        // Variables
        private readonly ResultsCollection _results;


        // Constructors
        public ResultResolver(ResultsCollection results)
        {
            _results = results;
        }


        // Methods
        public string[] GetResultNames()
        {
            if (_results == null) return new string[0];
            return _results.GetResultNames();
        }
        public string GetCurrentResultName()
        {
            if (_results == null) return null;
            return _results.GetCurrentResultName();
        }
        public bool IsCurrentResult(string resultName)
        {
            string currentResultName = GetCurrentResultName();
            return currentResultName != null &&
                   string.Equals(currentResultName, resultName, System.StringComparison.OrdinalIgnoreCase);
        }
        public bool ContainsResult(string resultName)
        {
            return _results != null && _results.ContainsResult(resultName);
        }
        public FeResults ResolveResult(string resultName)
        {
            if (_results == null || !_results.ContainsResult(resultName))
                throw new PythonAPIException("The result '" + resultName + "' is no longer loaded.");
            FeResults result = _results.GetResult(resultName);
            if (result == null)
                throw new PythonAPIException("The result '" + resultName + "' is no longer loaded.");
            return result;
        }
    }
}
