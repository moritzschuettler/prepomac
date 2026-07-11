// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System.Linq;

namespace PrePoMax.API.Python
{
    public class FieldMultiComponentDataAPI : BaseAPI
    {
        // Variables
        private readonly int _count;
        private readonly string _location;
        private readonly string _unit;
        private readonly object _ids;
        private readonly object _values;
        private readonly StringListAPI _componentNames;


        // Properties
        [PythonAPI("The node IDs corresponding to the rows of the values matrix.", Returns = "numpy.ndarray or int[]")]
        public object ids { get { return _ids; } }
        [PythonAPI("The field values with shape (node_count, component_count).", Returns = "numpy.ndarray or float[,]" )]
        public object values { get { return _values; } }
        [PythonAPI("The component names corresponding to the value columns.", Returns = "StringList")]
        public StringListAPI component_names { get { return _componentNames; } }
        [PythonAPI("The field location. Field results are node-based in PrePoMax results.", Returns = "str")]
        public string location { get { return _location; } }
        [PythonAPI("The result unit abbreviation.", Returns = "str")]
        public string unit { get { return _unit; } }
        [PythonAPI("The number of nodes.", Returns = "int")]
        public int count { get { return _count; } }


        // Constructors
        internal FieldMultiComponentDataAPI(int[] ids, float[,] values, string[] componentNames, string location, string unit)
        {
            _count = ids != null ? ids.Length : 0;
            _location = location;
            _unit = unit;
            _ids = NumPyConverter.ToPythonArray(ids);
            _values = NumPyConverter.ToPythonArray(values);
            _componentNames = new StringListAPI(componentNames);
        }


        // Methods
        public override string ToString()
        {
            return "FieldData(count=" + _count + ", components=" + _componentNames.Count +
                   ", location='" + _location + "', unit='" + _unit + "')";
        }
    }
}
