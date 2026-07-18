// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

namespace PrePoMax.API.Python
{
    public class ResultHistoryOutputComponentDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly string _unit;
        private readonly object _time;
        private readonly object _values;
        private readonly StringListAPI _entryNames;


        // Properties
        [PythonAPI("History time values.", Returns = "numpy.ndarray or NumericArray")]
        public object time { get { return _time; } }
        [PythonAPI("History result values. Multiple entries are returned as a matrix with one column per entry.", Returns = "numpy.ndarray or NumericArray" )]
        public object values { get { return _values; } }
        [PythonAPI("The result unit abbreviation.", Returns = "str")]
        public string unit { get { return _unit; } }
        [PythonAPI("The number of time values.", Returns = "int")]
        public int count { get { return _count; } }
        [PythonAPI("The names corresponding to value columns or the single returned entry.", Returns = "StringList")]
        public StringListAPI entry_names { get { return _entryNames; } }


        // Constructors
        internal ResultHistoryOutputComponentDataAPI(double[] time, object values, string unit, string[] entryNames)
        {
            _count = time != null ? time.Length : 0;
            _unit = unit;
            _time = NumPyConverter.ToPythonArray(time);
            _values = NumPyConverter.ToPythonArray(values);
            _entryNames = new StringListAPI(entryNames);
        }


        // Methods
        public override string ToString()
        {
            return "HistoryData(count=" + _count + ", entries=" + _entryNames.Count + ", unit='" + _unit + "')";
        }
    }
}
