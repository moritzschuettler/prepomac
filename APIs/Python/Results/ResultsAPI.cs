// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections;
using System.Collections.Generic;

namespace APIs.Python
{
    public class ResultsAPI : BaseAPI, IReadOnlyList<string>
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly ResultFieldOutputService _resultFieldOutputService;
        private readonly ResultHistoryOutputService _resultHistoryOutputService;
        private readonly SelectionService _selectionService;
        private readonly IPythonCommandGateway _commandGateway;


        // Properties
        [PythonAPI("The currently selected result.", Returns = "Result")]
        public ResultAPI current
        {
            get
            {
                string resultName = _resolver.GetCurrentResultName();
                if (resultName == null) throw new PythonAPIException("No current result is selected.");
                return new ResultAPI(_resolver, resultName, _resultFieldOutputService,
                                     _resultHistoryOutputService, _selectionService, _commandGateway);
            }
        }
        [PythonAPI("Result history outputs for the currently selected result.", Returns = "HistoryOutputs")]
        public ResultHistoryOutputsAPI history_outputs { get { return current.history_outputs; } }
        [PythonAPI("User-defined field outputs for the currently selected result.", Returns = "FieldOutputs")]
        public ResultFieldOutputsAPI field_outputs { get { return current.field_outputs; } }


        // Constructors
        internal ResultsAPI(ResultResolver resolver, ResultFieldOutputService resultFieldOutputService,
                            ResultHistoryOutputService resultHistoryOutputService,
                            SelectionService selectionService, IPythonCommandGateway commandGateway)
        {
            _resolver = resolver;
            _resultFieldOutputService = resultFieldOutputService;
            _resultHistoryOutputService = resultHistoryOutputService;
            _selectionService = selectionService;
            _commandGateway = commandGateway;
        }


        // Methods
        public int __len__()
        {
            return _resolver.GetResultNames().Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        string IReadOnlyList<string>.this[int index]
        {
            get
            {
                string[] keys = get_names().to_array();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return keys[index];
            }
        }
        public IEnumerator<string> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return key;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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

        [PythonAPI("Removes the current result using command history.", Returns = "None")]
        public void remove_current()
        {
            if (_resolver.GetCurrentResultName() == null)
                throw new PythonAPIException("No current result is selected.");
            EnsureCommandsAvailable();
            _commandGateway.RemoveCurrentResults();
        }
        [PythonAPI("Removes every loaded result using command history.", Returns = "None")]
        public void clear()
        {
            EnsureCommandsAvailable();
            _commandGateway.ClearResults();
        }

        [PythonAPI("Returns true if a result with the specified name is loaded.", Arguments = "result_name: str", Returns = "bool")]
        public bool contains(string result_name)
        {
            return _resolver.ContainsResult(result_name);
        }

        //
        private void EnsureCommandsAvailable()
        {
            if (_commandGateway == null)
                throw new PythonAPIException("Result changes are not available in this Python API context.");
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
