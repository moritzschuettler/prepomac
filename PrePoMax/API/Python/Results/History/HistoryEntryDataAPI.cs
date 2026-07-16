// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

namespace PrePoMax.API.Python
{
    public class HistoryEntryDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly string _entryName;
        private readonly string _unit;
        private readonly object _time;
        private readonly object _values;


        // Properties
        [PythonAPI("The selected history entry name.", Returns = "str")]
        public string entry_name { get { return _entryName; } }
        [PythonAPI("History time values for this entry.", Returns = "numpy.ndarray or NumericArray")]
        public object time { get { return _time; } }
        [PythonAPI("History result values for this entry.", Returns = "numpy.ndarray or NumericArray")]
        public object values { get { return _values; } }
        [PythonAPI("The result unit abbreviation.", Returns = "str")]
        public string unit { get { return _unit; } }
        [PythonAPI("The number of time values.", Returns = "int")]
        public int count { get { return _count; } }


        // Constructors
        internal HistoryEntryDataAPI(string entryName, double[] time, double[] values, string unit)
        {
            _count = time != null ? time.Length : 0;
            _entryName = entryName;
            _unit = unit;
            _time = NumPyConverter.ToPythonArray(time);
            _values = NumPyConverter.ToPythonArray(values);
        }


        // Methods
        public override string ToString()
        {
            return "HistoryEntryData(entry_name=" + StringListAPI.ToDisplayString(_entryName) +
                   ", count=" + _count + ", unit=" + StringListAPI.ToDisplayString(_unit) + ")";
        }
    }
}
