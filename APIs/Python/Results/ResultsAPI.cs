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
using APIs.Python.Model;

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
        private readonly DocumentService _documentService;


        // Properties
        [PythonAPI("The currently selected result. Setting it to the loaded path name of a result, or to a " +
                   "Result, selects that result the way the results list of the toolbar does; the result that " +
                   "is already current records nothing.",
                   Returns = "Result")]
        public object current
        {
            get { return GetCurrent(); }
            set { SetCurrent(value); }
        }
        [PythonAPI("Result history outputs for the currently selected result.", Returns = "HistoryOutputs")]
        public ResultHistoryOutputsAPI history_outputs { get { return GetCurrent().history_outputs; } }
        [PythonAPI("User-defined field outputs for the currently selected result.", Returns = "FieldOutputs")]
        public ResultFieldOutputsAPI field_outputs { get { return GetCurrent().field_outputs; } }


        // Constructors
        internal ResultsAPI(ResultResolver resolver, ResultFieldOutputService resultFieldOutputService,
                            ResultHistoryOutputService resultHistoryOutputService,
                            SelectionService selectionService, IPythonCommandGateway commandGateway,
                            DocumentService documentService)
        {
            _resolver = resolver;
            _resultFieldOutputService = resultFieldOutputService;
            _resultHistoryOutputService = resultHistoryOutputService;
            _selectionService = selectionService;
            _commandGateway = commandGateway;
            _documentService = documentService;
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
                string[] keys = get_names().ToArray();
                int position = index < 0 ? keys.Length + index : index;
                if (position < 0 || position >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return keys[position];
            }
        }
        public IEnumerator<string> GetEnumerator()
        {
            foreach (string key in get_names().ToArray()) yield return key;
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

        [PythonAPI("Opens a .frd, .dat, .odb or .foam result file beside the current model using command " +
                   "history. The model and its command history are kept; a .pmx or .pmh file is refused, " +
                   "because pmx.open opens it.",
                   Arguments = "file_name: str", Returns = "None",
                   Example = "pmx.results.open('Analysis-1.frd')")]
        public void open(object file_name)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            if (_documentService == null)
                throw new PythonAPIException("Opening a result file is not available in this Python API context.");
            //
            _documentService.OpenResultFile(fileName);
        }
        [PythonAPI("Removes the current result using command history. As Close Current Result does, the first " +
                   "remaining result becomes the current one, and removing the only result clears the results.",
                   Returns = "None")]
        public void remove_current()
        {
            if (_resolver.GetCurrentResultName() == null)
                throw new PythonAPIException("No current result is selected.");
            EnsureCommandsAvailable();
            if (_resolver.GetResultNames().Length <= 1)
            {
                _commandGateway.ClearResults();
                return;
            }
            //
            _commandGateway.RemoveCurrentResults();
            string[] remainingNames = _resolver.GetResultNames();
            if (remainingNames.Length > 0 && !_resolver.IsCurrentResult(remainingNames[0]))
                _commandGateway.SetCurrentResults(remainingNames[0]);
        }
        [PythonAPI("Removes every loaded result using command history.", Returns = "None")]
        public void clear()
        {
            EnsureCommandsAvailable();
            _commandGateway.ClearResults();
        }

        [PythonAPI("Returns true if a result with the specified name is loaded.", Arguments = "name: str", Returns = "bool")]
        public bool contains(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", true);
            return _resolver.ContainsResult(nameText);
        }

        //
        private ResultAPI GetCurrent()
        {
            string resultName = _resolver.GetCurrentResultName();
            if (resultName == null) throw new PythonAPIException("No current result is selected.");
            return new ResultAPI(_resolver, resultName, _resultFieldOutputService,
                                 _resultHistoryOutputService, _selectionService, _commandGateway);
        }
        private void SetCurrent(object value)
        {
            ResultAPI result = value as ResultAPI;
            string resultName = result != null ? result.name : value as string;
            if (resultName == null)
                throw new PythonAPIException("The current result is set to the name of a loaded result or to a Result.");
            if (!_resolver.ContainsResult(resultName))
                throw new PythonAPIException("The result '" + resultName + "' is not loaded.");
            resultName = _resolver.GetStoredResultName(resultName);
            if (_commandGateway == null)
                throw new PythonAPIException("Result selection is not available in this Python API context.");
            if (_resolver.IsCurrentResult(resultName)) return;
            _commandGateway.SetCurrentResults(resultName);
            if (!_resolver.IsCurrentResult(resultName))
                throw new PythonAPIException("The result '" + resultName + "' could not be selected.");
        }
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
