// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

namespace APIs.Python
{
    public class FieldDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly string _location;
        private readonly string _unit;
        private readonly IntListAPI _ids;
        private readonly object _values;


        // Properties
        [PythonAPI("The node IDs corresponding to the values array.", Returns = "IntList")]
        public IntListAPI ids { get { return _ids; } }
        [PythonAPI("The scalar component values.", Returns = "numpy.ndarray or NumericArray")]
        public object values { get { return _values; } }
        [PythonAPI("The field location. Field results are node-based in PrePoMax results.", Returns = "str")]
        public string location { get { return _location; } }
        [PythonAPI("The result unit abbreviation.", Returns = "str")]
        public string unit { get { return _unit; } }
        [PythonAPI("The number of values.", Returns = "int")]
        public int count { get { return _count; } }


        // Constructors
        internal FieldDataAPI(int[] ids, float[] values, string location, string unit)
        {
            _count = values != null ? values.Length : 0;
            _location = location;
            _unit = unit;
            _ids = new IntListAPI(ids);
            _values = NumPyConverter.ToPythonArray(values);
        }


        // Methods
        public override string ToString()
        {
            return "FieldData(count=" + _count + ", location='" + _location + "', unit='" + _unit + "')";
        }
    }
}
