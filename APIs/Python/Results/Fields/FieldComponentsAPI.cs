// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

using System.Collections;
using System.Collections.Generic;

namespace APIs.Python
{
    public class FieldComponentsAPI : BaseAPI, IReadOnlyList<FieldComponentAPI>
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
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        FieldComponentAPI IReadOnlyList<FieldComponentAPI>.this[int index]
        {
            get
            {
                string[] keys = get_names().to_array();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<FieldComponentAPI> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [PythonAPI("Returns component names available for this field.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName));
        }

        [PythonAPI("Returns the first component available for this field.", Returns = "FieldComponent")]
        public FieldComponentAPI first()
        {
            string[] names = _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName);
            if (names.Length == 0) throw new PythonAPIException("The field components collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last component available for this field.", Returns = "FieldComponent")]
        public FieldComponentAPI last()
        {
            string[] names = _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName);
            if (names.Length == 0) throw new PythonAPIException("The field components collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if this field contains the specified component.", Arguments = "component_name: str", Returns = "bool")]
        public bool contains(string component_name)
        {
            return _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName).Contains(component_name);
        }
        public override string ToString()
        {
            string[] names = _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName);
            return "FieldComponents(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _dataService.GetComponentNames(_resultName, _stepId, _incrementId, _fieldName);
            return "FieldComponents(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}

