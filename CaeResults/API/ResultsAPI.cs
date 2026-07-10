// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeGlobals.CaeResults;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CaeResults
{
    public class ResultsAPI : BaseAPI
    {
        // Variables                                                                                                                
        private readonly ResultsCollection _results;


        // Constructors                                                                                                             
        public ResultsAPI(ResultsCollection results)
        {
            _results = results;
        }


        // Methods                                                                                                                  
        public StringListAPI get_result_names()
        {
            return new StringListAPI(_results.GetResultNames());
        }
        public StringListAPI get_field_names(string resultName)
        {
            return new StringListAPI(_results.GetResult(resultName).GetVisibleFieldNames());
        }
        public StringListAPI get_field_component_names(string resultName, string fieldName)
        {
            return new StringListAPI(_results.GetResult(resultName).GetFieldComponentNames(fieldName));
        }
    }
}