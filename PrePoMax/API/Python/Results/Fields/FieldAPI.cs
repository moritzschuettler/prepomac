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
    public class FieldAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly int _incrementId;
        private readonly string _fieldName;
        private readonly ResultDataService _dataService;
        private readonly FieldComponentsAPI _components;


        // Properties
        [PythonAPI("The field name.", Returns = "str")]
        public string name { get { return _fieldName; } }
        [PythonAPI("Components available for this field.", Returns = "FieldComponents")]
        public FieldComponentsAPI components { get { return _components; } }


        // Constructors
        internal FieldAPI(string resultName, int stepId, int incrementId, string fieldName, ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _incrementId = incrementId;
            _fieldName = fieldName;
            _dataService = dataService;
            _components = new FieldComponentsAPI(_resultName, _stepId, _incrementId, _fieldName, _dataService);
        }


        // Methods
        [PythonAPI("Returns all components of this nodal field as one matrix.", Returns = "FieldMultiComponentData")]
        public FieldMultiComponentDataAPI get_data()
        {
            return _dataService.GetFieldData(_resultName, _stepId, _incrementId, _fieldName);
        }
        public override string ToString()
        {
            string[] names = _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName);
            return "Field(name='" + _fieldName + "', components=" + new StringListAPI(names).ToString() + ")";
        }
    }
}


