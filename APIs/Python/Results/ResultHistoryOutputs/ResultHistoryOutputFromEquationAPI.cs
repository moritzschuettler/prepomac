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
    public class ResultHistoryOutputFromEquationAPI : ResultHistoryOutputAPI
    {
        // Properties
        [PythonAPI("The history output equation.", Returns = "str")]
        public string equation { get { return ResolveFromEquation().Equation; } }
        [PythonAPI("The user-defined result unit.", Returns = "str")]
        public string unit { get { return ResolveFromEquation().Unit; } }
        [PythonAPI("The generated component name.", Returns = "str")]
        public string component_name { get { return ResultHistoryOutputFromEquation.ComponentName; } }


        // Constructors
        internal ResultHistoryOutputFromEquationAPI(string resultName, string historyOutputName,
                                                    ResultHistoryOutputDataService resultHistoryService)
            : base(resultName, historyOutputName, resultHistoryService)
        {
        }


        // Methods
        private ResultHistoryOutputFromEquation ResolveFromEquation()
        {
            ResultHistoryOutputFromEquation historyOutput = Resolve() as ResultHistoryOutputFromEquation;
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + _historyOutputName +
                                             "' is not an equation history output.");
            return historyOutput;
        }
        public override string ToString()
        {
            ResultHistoryOutputFromEquation historyOutput = ResolveFromEquation();
            return "HistoryOutputFromEquation(name=" + StringListAPI.ToDisplayString(_historyOutputName) +
                   ", equation=" + StringListAPI.ToDisplayString(historyOutput.Equation) +
                   ", unit=" + StringListAPI.ToDisplayString(historyOutput.Unit) + ")";
        }
    }
}
