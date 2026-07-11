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
    public class FieldComponentsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly int _incrementId;
        private readonly string _fieldName;
        private readonly ResultDataService _dataService;


        // Constructors
        internal FieldComponentsAPI(string resultName, int stepId, int incrementId, string fieldName,
                                     ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _incrementId = incrementId;
            _fieldName = fieldName;
            _dataService = dataService;
        }


        // Methods
        public FieldComponentAPI __getitem__(string componentName)
        {
            return this[componentName];
        }
        public FieldComponentAPI this[string componentName]
        {
            get
            {
                if (!contains(componentName))
                    throw new PythonAPIException("The component '" + componentName + "' does not exist in field '" +
                                                 _fieldName + "'.");
                return new FieldComponentAPI(_resultName, _stepId, _incrementId, _fieldName, componentName, _dataService);
            }
        }
        public int __len__()
        {
            return _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName).Length;
        }

        [PythonAPI("Returns component names available for this field.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName));
        }

        [PythonAPI("Returns true if this field contains the specified component.", Arguments = "component_name: str", Returns = "bool")]
        public bool contains(string componentName)
        {
            return _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName).Contains(componentName);
        }
        public override string ToString()
        {
            string[] names = _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName);
            return "FieldComponents(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}

