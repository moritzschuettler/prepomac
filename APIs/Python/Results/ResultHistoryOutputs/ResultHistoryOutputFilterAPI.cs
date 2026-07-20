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
        [PythonAPI("The filter type. Possible values are 'minimum', 'maximum', 'sum', and 'average'.",
                   Returns = "str")]
        public string type { get { return GetTypeName(_type); } }
        [PythonAPI("The filter option. Possible values are 'row', 'column', 'rows', and 'columns'.",
                   Returns = "str")]
        public string option { get { return GetOptionName(_option); } }


        // Constructors
        internal ResultHistoryOutputFilterAPI(HistoryResultFilterTypeEnum type, string option)
        {
            _type = type;
            _option = option;
        }


        // Methods
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
        private static string GetTypeName(HistoryResultFilterTypeEnum type)
        {
            if (type == HistoryResultFilterTypeEnum.Minimum) return "minimum";
            if (type == HistoryResultFilterTypeEnum.Maximum) return "maximum";
            if (type == HistoryResultFilterTypeEnum.Sum) return "sum";
            if (type == HistoryResultFilterTypeEnum.Average) return "average";
            throw new PythonAPIException("Unsupported history filter type '" + type + "'.");
        }
        private static string GetOptionName(string option)
        {
            if (option == HistoryResultFilter.Row) return "row";
            if (option == HistoryResultFilter.Column) return "column";
            if (option == HistoryResultFilter.Rows) return "rows";
            if (option == HistoryResultFilter.Columns) return "columns";
            throw new PythonAPIException("Unsupported history filter option '" + option + "'.");
        }
    }
}
