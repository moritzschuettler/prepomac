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
    public class ResultNodeSetAPI : ResultMeshRegionAPI
    {
        // Properties
        [PythonAPI("The number of nodes in the result node set.", Returns = "int")]
        public int count { get { return MeshService.GetNodeSetIds(ResultName, name).Length; } }
        [PythonAPI("The node IDs in this result node set.", Returns = "numpy.ndarray or IntList")]
        public object node_ids
        {
            get { return NumPyConverter.ToPythonArray(MeshService.GetNodeSetIds(ResultName, name)); }
        }


        // Constructors
        internal ResultNodeSetAPI(string resultName, string name, MeshDataService meshService)
            : base(resultName, name, RegionTypeEnum.NodeSetName, meshService)
        {
        }


        // Methods
        public override string ToString()
        {
            return "NodeSet(name=" + StringListAPI.ToDisplayString(name) + ", nodes=" + count + ")";
        }
    }
}
