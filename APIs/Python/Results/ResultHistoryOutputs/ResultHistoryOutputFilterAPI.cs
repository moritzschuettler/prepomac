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
    public class ResultHistoryOutputFilterAPI : BaseAPI
    {
        // Variables
        private readonly HistoryResultFilterTypeEnum _type;
        private readonly string _option;


        // Properties
        [PythonAPI("The filter type: Minimum, Maximum, Sum or Average.", Returns = "str")]
        public string type { get { return _type.ToString(); } }
        [PythonAPI("The filter option: Row or Column for a Minimum or Maximum filter, Rows or Columns for a Sum " +
                   "or Average filter.", Returns = "str")]
        public string option { get { return _option; } }


        // Constructors
        internal ResultHistoryOutputFilterAPI(HistoryResultFilterTypeEnum type, string option)
        {
            _type = type;
            _option = option;
        }


        // Methods
        internal static ResultHistoryOutputFilterAPI FromBase(HistoryResultFilter filter)
        {
            if (filter == null || filter.Type == HistoryResultFilterTypeEnum.None) return null;
            return new ResultHistoryOutputFilterAPI(filter.Type, filter.Option);
        }
        internal HistoryResultFilter CreateBase()
        {
            HistoryResultFilter filter = new HistoryResultFilter();
            filter.Type = _type;
            filter.Option = _option;
            return filter;
        }
        public override string ToString()
        {
            return "HistoryOutputFilter(type=" + StringListAPI.ToDisplayString(type) +
                   ", option=" + StringListAPI.ToDisplayString(option) + ")";
        }
    }
}
