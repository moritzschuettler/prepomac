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
        [PythonAPI("What a Minimum or Maximum filter returns, Row or Column, the return_value argument of " +
                   "create_minimum_filter and create_maximum_filter; None for a Sum or Average filter.",
                   Returns = "str or None")]
        public string return_value { get { return IsMinMax() ? _option : null; } }
        [PythonAPI("What a Sum or Average filter operates on, Rows or Columns, the operate_on argument of " +
                   "create_sum_filter and create_average_filter; None for a Minimum or Maximum filter.",
                   Returns = "str or None")]
        public string operate_on { get { return IsMinMax() ? null : _option; } }


        // Constructors
        internal ResultHistoryOutputFilterAPI(HistoryResultFilterTypeEnum type, string option)
        {
            _type = type;
            _option = option;
        }


        // Methods
        private bool IsMinMax()
        {
            return _type == HistoryResultFilterTypeEnum.Minimum || _type == HistoryResultFilterTypeEnum.Maximum;
        }
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
                   (IsMinMax() ? ", return_value=" : ", operate_on=") + StringListAPI.ToDisplayString(_option) + ")";
        }
    }
}
