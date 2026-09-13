// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python
{
    public class FieldExtremesAPI : BaseAPI
    {
        // Variables
        private readonly string _componentName;
        private readonly string _unit;
        private readonly float _minValue;
        private readonly int _minNodeId;
        private readonly float _maxValue;
        private readonly int _maxNodeId;


        // Properties
        [PythonAPI("The component the extremes were read from.", Returns = "str")]
        public string component_name { get { return _componentName; } }
        [PythonAPI("The smallest component value.", Returns = "float")]
        public float min_value { get { return _minValue; } }
        [PythonAPI("The ID of the node carrying the smallest value.", Returns = "int")]
        public int min_node_id { get { return _minNodeId; } }
        [PythonAPI("The largest component value.", Returns = "float")]
        public float max_value { get { return _maxValue; } }
        [PythonAPI("The ID of the node carrying the largest value.", Returns = "int")]
        public int max_node_id { get { return _maxNodeId; } }
        [PythonAPI("The result unit abbreviation.", Returns = "str")]
        public string unit { get { return _unit; } }


        // Constructors
        internal FieldExtremesAPI(string componentName, float minValue, int minNodeId, float maxValue,
                                  int maxNodeId, string unit)
        {
            _componentName = componentName;
            _minValue = minValue;
            _minNodeId = minNodeId;
            _maxValue = maxValue;
            _maxNodeId = maxNodeId;
            _unit = unit;
        }


        // Methods
        public override string ToString()
        {
            return "FieldExtremes(component='" + _componentName + "', min=" + _minValue + " at node " + _minNodeId +
                   ", max=" + _maxValue + " at node " + _maxNodeId + ", unit='" + _unit + "')";
        }
    }
}
