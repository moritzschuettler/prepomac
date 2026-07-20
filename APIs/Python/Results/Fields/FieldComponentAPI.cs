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
    public class FieldComponentAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly int _incrementId;
        private readonly string _fieldName;
        private readonly string _componentName;
        private readonly ResultDataService _dataService;


        // Properties
        [PythonAPI("The component name.", Returns = "str")]
        public string name { get { return _componentName; } }


        // Constructors
        internal FieldComponentAPI(string resultName, int stepId, int incrementId, string fieldName, string componentName,
                                    ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _incrementId = incrementId;
            _fieldName = fieldName;
            _componentName = componentName;
            _dataService = dataService;
        }


        // Methods
        [PythonAPI("Returns all nodal IDs and component values in one bulk extraction.", Returns = "FieldData")]
        public FieldDataAPI get_data()
        {
            return _dataService.GetComponentData(_resultName, _stepId, _incrementId, _fieldName, _componentName);
        }
        [PythonAPI("Returns this component value for one node ID.", Arguments = "node_id: int", Returns = "float")]
        public float get_value(int nodeId)
        {
            return _dataService.GetNodalValue(_resultName, _stepId, _incrementId, _fieldName, _componentName, nodeId);
        }
        public override string ToString()
        {
            return "FieldComponent(name='" + _componentName + "')";
        }
    }
}

