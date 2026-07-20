// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;

namespace APIs.Python
{
    public class ResultElementSetAPI : ResultMeshRegionAPI
    {
        // Properties
        [PythonAPI("The number of elements in the result element set.", Returns = "int")]
        public int count { get { return MeshService.GetElementSetIds(ResultName, name).Length; } }
        [PythonAPI("The element IDs in this result element set.", Returns = "numpy.ndarray or IntList")]
        public object element_ids
        {
            get { return NumPyConverter.ToPythonArray(MeshService.GetElementSetIds(ResultName, name)); }
        }


        // Constructors
        internal ResultElementSetAPI(string resultName, string name, MeshDataService meshService)
            : base(resultName, name, RegionTypeEnum.ElementSetName, meshService)
        {
        }


        // Methods
        public override string ToString()
        {
            return "ElementSet(name=" + StringListAPI.ToDisplayString(name) + ", elements=" + count + ")";
        }
    }
}
