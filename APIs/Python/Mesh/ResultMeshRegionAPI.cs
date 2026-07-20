// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;

namespace APIs.Python
{
    public abstract class ResultMeshRegionAPI : BaseAPI
    {
        // Variables
        private readonly string _resultName;
        private readonly string _name;
        private readonly RegionTypeEnum _regionType;
        internal readonly MeshDataService MeshService;


        // Properties
        [PythonAPI("The existing result mesh region name.", Returns = "str")]
        public string name { get { return _name; } }
        internal string ResultName { get { return _resultName; } }
        internal RegionTypeEnum RegionType { get { return _regionType; } }


        // Constructors
        internal ResultMeshRegionAPI(string resultName, string name, RegionTypeEnum regionType,
                                     MeshDataService meshService)
        {
            _resultName = resultName;
            _name = name;
            _regionType = regionType;
            MeshService = meshService;
        }


        // Methods
        internal void ValidateForResult(string resultName)
        {
            if (!string.Equals(_resultName, resultName, StringComparison.Ordinal))
                throw new PythonAPIException("The region '" + _name +
                                             "' belongs to a different result.");
            MeshService.ValidateUserRegion(_resultName, _regionType, _name);
        }
    }
}
