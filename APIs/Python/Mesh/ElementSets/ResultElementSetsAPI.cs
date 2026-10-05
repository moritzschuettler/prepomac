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
    public class ResultElementSetsAPI : BaseAPI, IReadOnlyList<ResultElementSetAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;


        // Constructors
        internal ResultElementSetsAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
        }


        // Methods
        public ResultElementSetAPI __getitem__(string elementSetName)
        {
            return this[elementSetName];
        }
        public ResultElementSetAPI this[string elementSetName]
        {
            get { return _meshService.GetElementSet(_resultName, elementSetName); }
        }
        public int __len__()
        {
            return _meshService.GetElementSetNames(_resultName).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        ResultElementSetAPI IReadOnlyList<ResultElementSetAPI>.this[int index]
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
        public IEnumerator<ResultElementSetAPI> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool __contains__(string elementSetName)
        {
            return contains(elementSetName);
        }

        [PythonAPI("Returns user-visible result element set names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_meshService.GetElementSetNames(_resultName));
        }

        [PythonAPI("Returns the first result element set.", Returns = "ElementSet")]
        public ResultElementSetAPI first()
        {
            string[] names = _meshService.GetElementSetNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The element sets collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last result element set.", Returns = "ElementSet")]
        public ResultElementSetAPI last()
        {
            string[] names = _meshService.GetElementSetNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The element sets collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the result contains the specified user-visible element set.",
                   Arguments = "element_set_name: str", Returns = "bool")]
        public bool contains(string element_set_name)
        {
            return _meshService.GetElementSetNames(_resultName).Contains(element_set_name);
        }
        public override string ToString()
        {
            string[] names = _meshService.GetElementSetNames(_resultName);
            return "ElementSets(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _meshService.GetElementSetNames(_resultName);
            return "ElementSets(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
