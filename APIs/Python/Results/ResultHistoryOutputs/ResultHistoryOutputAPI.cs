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
using System;
using System.Collections.Generic;

namespace APIs.Python
{
    public class ResultHistoryOutputAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(ResultHistoryOutputFromField), "from_field" },
            { typeof(ResultHistoryOutputFromEquation), "from_equation" },
            { typeof(ResultHistoryOutputFromAffineFit), "from_affine_fit" },
            { typeof(ResultHistoryOutputFromIntegration), "from_integration" },
            { typeof(ResultHistoryOutputFromMergedHistories), "from_merged_histories" },
            { typeof(ResultHistoryOutputFromExtractedEntry), "from_extracted_entry" },
            { typeof(ResultHistoryOutputXY), "xy" },
            { typeof(ResultHistoryOutputFromTable), "from_table" }
        };
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
        [PythonAPI("The history output type, named after the add_* method that makes it without the add_ prefix, " +
                   "for example from_field or from_table, from_affine_fit for an output made in the user " +
                   "interface, which no add_* method makes, or None for a solver-generated history output.",
                   Returns = "str or None")]
        public string item_type
        {
            get
            {
                ResultHistoryOutput historyOutput = ResolveOrNull();
                return historyOutput != null ? GetItemType(historyOutput) : null;
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


        // Static methods
        internal static string GetItemType(ResultHistoryOutput historyOutput)
        {
            string itemType;
            if (ItemTypes.TryGetValue(historyOutput.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The history output '" + historyOutput.Name + "' is a " +
                                         historyOutput.GetType().Name + ", which no add_* method of the " +
                                         "Python API makes.");
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
