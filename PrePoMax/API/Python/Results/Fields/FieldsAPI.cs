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
    public class FieldsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly int _stepId;
        private readonly int _incrementId;
        private readonly ResultDataService _dataService;


        // Constructors
        internal FieldsAPI(string resultName, int stepId, int incrementId, ResultDataService dataService)
        {
            _resultName = resultName;
            _stepId = stepId;
            _incrementId = incrementId;
            _dataService = dataService;
        }


        // Methods
        public FieldAPI __getitem__(string fieldName)
        {
            return this[fieldName];
        }
        public FieldAPI this[string fieldName]
        {
            get
            {
                if (!contains(fieldName))
                    throw new PythonAPIException("The field '" + fieldName + "' does not exist for step " + _stepId +
                                                 ", increment " + _incrementId + ".");
                return new FieldAPI(_resultName, _stepId, _incrementId, fieldName, _dataService);
            }
        }
        public int __len__()
        {
            return _dataService.GetFieldNames(_resultName, _stepId, _incrementId).Length;
        }

        [PythonAPI("Returns field names available for this increment.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_dataService.GetFieldNames(_resultName, _stepId, _incrementId));
        }

        [PythonAPI("Returns true if this increment contains the specified field.", Arguments = "field_name: str", Returns = "bool")]
        public bool contains(string fieldName)
        {
            return _dataService.GetFieldNames(_resultName, _stepId, _incrementId).Contains(fieldName);
        }
        public override string ToString()
        {
            string[] names = _dataService.GetFieldNames(_resultName, _stepId, _incrementId);
            return "Fields(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
