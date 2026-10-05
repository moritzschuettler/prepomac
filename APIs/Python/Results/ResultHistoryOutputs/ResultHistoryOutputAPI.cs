// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeResults;

namespace APIs.Python
{
    public class ResultHistoryOutputAPI : BaseAPI
    {
        // Variables
        protected readonly string _resultName;
        protected readonly string _historyOutputName;
        private readonly ResultHistoryOutputDataService _resultHistoryService;
        private readonly ResultHistoryOutputFieldsAPI _fields;


        // Properties
        [PythonAPI("The history output name.", Returns = "str")]
        public string name { get { return _historyOutputName; } }
        [PythonAPI("The fields in this history output.", Returns = "HistoryFields")]
        public ResultHistoryOutputFieldsAPI fields { get { return _fields; } }
        [PythonAPI("Whether the history output was defined by the user instead of produced by the solver.",
                   Returns = "bool")]
        public bool is_user_defined { get { return ResolveOrNull() != null; } }
        [PythonAPI("The history output definition type, or None for a solver-generated history output.",
                   Returns = "str or None")]
        public string type_name
        {
            get
            {
                ResultHistoryOutput historyOutput = ResolveOrNull();
                return historyOutput != null ? historyOutput.GetType().Name : null;
            }
        }
        [PythonAPI("The first history-output filter, or None when it is disabled or the output is solver-generated.",
                   Returns = "HistoryOutputFilter or None")]
        public ResultHistoryOutputFilterAPI filter1
        {
            get
            {
                ResultHistoryOutput historyOutput = ResolveOrNull();
                return historyOutput == null ? null : ResultHistoryOutputFilterAPI.FromBase(historyOutput.Filter1);
            }
        }
        [PythonAPI("The second history-output filter, or None when it is disabled or the output is solver-generated.",
                   Returns = "HistoryOutputFilter or None")]
        public ResultHistoryOutputFilterAPI filter2
        {
            get
            {
                ResultHistoryOutput historyOutput = ResolveOrNull();
                return historyOutput == null ? null : ResultHistoryOutputFilterAPI.FromBase(historyOutput.Filter2);
            }
        }


        // Constructors
        internal ResultHistoryOutputAPI(string resultName, string historyOutputName,
                                        ResultHistoryOutputDataService resultHistoryService)
        {
            _resultName = resultName;
            _historyOutputName = historyOutputName;
            _resultHistoryService = resultHistoryService;
            _fields = new ResultHistoryOutputFieldsAPI(_resultName, _historyOutputName, _resultHistoryService);
        }


        // Methods
        protected ResultHistoryOutput ResolveOrNull()
        {
            return _resultHistoryService.ResolveDefinitionOrNull(_resultName, _historyOutputName);
        }
        protected ResultHistoryOutput Resolve()
        {
            ResultHistoryOutput historyOutput = ResolveOrNull();
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + _historyOutputName +
                                             "' is not a user-defined history output.");
            return historyOutput;
        }
        protected static string FormatRegionName(ResultHistoryOutput historyOutput)
        {
            if (string.IsNullOrEmpty(historyOutput.RegionName)) return null;
            return historyOutput.RegionName;
        }
        // A history name, a field name or a component name may contain a dot, so a source is read back as
        // the three names a source argument takes rather than as the dotted display name
        internal static StringListAPI ToSourceNames(HistoryResultData source)
        {
            return new StringListAPI(new string[] { source.SetName, source.FieldName, source.ComponentName });
        }
        internal static StringListAPI[] ToSourceNames(HistoryResultData[] sources)
        {
            StringListAPI[] sourceNames = new StringListAPI[sources.Length];
            for (int i = 0; i < sources.Length; i++) sourceNames[i] = ToSourceNames(sources[i]);
            return sourceNames;
        }
        public override string ToString()
        {
            return "HistoryOutput(name='" + _historyOutputName + "', fields=" +
                   _resultHistoryService.GetFieldNames(_resultName, _historyOutputName).Length + ")";
        }
    }
}
