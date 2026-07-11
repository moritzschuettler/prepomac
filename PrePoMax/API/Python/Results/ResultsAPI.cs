// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System.Linq;

namespace PrePoMax.API.Python
{
    public class ResultsAPI : BaseAPI
    {
        // Variables
        private readonly ResultResolver _resolver;


        // Constructors
        internal ResultsAPI(ResultResolver resolver)
        {
            _resolver = resolver;
        }


        // Methods
        public int __len__()
        {
            return _resolver.GetResultNames().Length;
        }
        public ResultAPI __getitem__(string resultName)
        {
            return this[resultName];
        }
        public ResultAPI this[string resultName]
        {
            get
            {
                _resolver.ResolveResult(resultName);
                return new ResultAPI(_resolver, resultName);
            }
        }

        [PythonAPI("Returns the loaded result names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_resolver.GetResultNames());
        }

        [PythonAPI("Returns true if a result with the specified name is loaded.", Arguments = "result_name: str", Returns = "bool")]
        public bool contains(string resultName)
        {
            return _resolver.ContainsResult(resultName);
        }

        // Backward-compatible console test methods from the prototype API.
        public StringListAPI get_result_names()
        {
            return get_names();
        }
        public StringListAPI get_field_names(string resultName)
        {
            return new StringListAPI(_resolver.ResolveResult(resultName).GetVisibleFieldNames());
        }
        public StringListAPI get_field_component_names(string resultName, string fieldName)
        {
            return new StringListAPI(_resolver.ResolveResult(resultName).GetFieldComponentNames(fieldName));
        }
        public override string ToString()
        {
            string[] names = _resolver.GetResultNames();
            return "Results(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
