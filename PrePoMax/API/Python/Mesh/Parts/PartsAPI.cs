// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Linq;

namespace PrePoMax.API.Python
{
    public class PartsAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;


        // Constructors
        internal PartsAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
        }


        // Methods
        public PartAPI __getitem__(string partName)
        {
            return this[partName];
        }
        public PartAPI this[string partName]
        {
            get { return _meshService.GetPart(_resultName, partName); }
        }
        public int __len__()
        {
            return _meshService.GetPartNames(_resultName).Length;
        }

        [PythonAPI("Returns result mesh part names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_meshService.GetPartNames(_resultName));
        }

        [PythonAPI("Returns true if the result mesh contains the specified part.", Arguments = "part_name: str", Returns = "bool")]
        public bool contains(string partName)
        {
            return _meshService.GetPartNames(_resultName).Contains(partName);
        }
        public override string ToString()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            return "Parts(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
