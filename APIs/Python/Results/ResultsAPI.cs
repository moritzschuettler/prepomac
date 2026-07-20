// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python
{
    public class ResultsAPI : BaseAPI
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly ResultFieldOutputService _resultFieldOutputService;
        private readonly ResultHistoryOutputService _resultHistoryOutputService;
        private readonly IPythonCommandGateway _commandGateway;


        // Properties
        [PythonAPI("The currently selected result.", Returns = "Result")]
        public ResultAPI current
        {
            get
            {
                string resultName = _resolver.GetCurrentResultName();
                if (resultName == null) throw new PythonAPIException("No current result is selected.");
                return new ResultAPI(_resolver, resultName, _resultFieldOutputService, _resultHistoryOutputService);
            }
        }
        [PythonAPI("User-defined field outputs for the currently selected result.", Returns = "FieldOutputs")]
        public ResultFieldOutputsAPI field_outputs { get { return current.field_outputs; } }


        // Constructors
        internal ResultsAPI(ResultResolver resolver, ResultFieldOutputService resultFieldOutputService,
                            ResultHistoryOutputService resultHistoryOutputService,
                            IPythonCommandGateway commandGateway)
        {
            _resolver = resolver;
            _resultFieldOutputService = resultFieldOutputService;
            _resultHistoryOutputService = resultHistoryOutputService;
            _commandGateway = commandGateway;
        }


        // Methods
        public int __len__()
        {
            return _resolver.GetResultNames().Length;
        }

        [PythonAPI("Returns the loaded result names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_resolver.GetResultNames());
        }

        [PythonAPI("Sets the current result using its loaded path name and returns it.",
                   Arguments = "result_name: str", Returns = "Result")]
        public ResultAPI set_current(string result_name)
        {
            if (!_resolver.ContainsResult(result_name))
                throw new PythonAPIException("The result '" + result_name + "' is not loaded.");
            if (_commandGateway == null)
                throw new PythonAPIException("Result selection is not available in this Python API context.");
            _commandGateway.SetCurrentResults(result_name);
            if (!_resolver.IsCurrentResult(result_name))
                throw new PythonAPIException("The result '" + result_name + "' could not be selected.");
            return current;
        }

        [PythonAPI("Returns true if a result with the specified name is loaded.", Arguments = "result_name: str", Returns = "bool")]
        public bool contains(string resultName)
        {
            return _resolver.ContainsResult(resultName);
        }

        public override string ToString()
        {
            string[] names = _resolver.GetResultNames();
            return "Results(count=" + names.Length + ", current=" +
                   StringListAPI.ToDisplayString(_resolver.GetCurrentResultName()) +
                   ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _resolver.GetResultNames();
            return "Results(count=" + names.Length + ", current=" +
                   StringListAPI.ToDisplayString(_resolver.GetCurrentResultName()) + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
