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
    public class ResultElementSetsAPI : BaseAPI
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
        public bool contains(string elementSetName)
        {
            return _meshService.GetElementSetNames(_resultName).Contains(elementSetName);
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
