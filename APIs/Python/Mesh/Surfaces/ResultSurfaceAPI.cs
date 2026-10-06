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
    public class ResultSurfaceAPI : ResultMeshRegionAPI
    {
        // Properties
        [PythonAPI("The result surface area.", Returns = "float")]
        public double area { get { return MeshService.GetSurfaceArea(ResultName, name); } }


        // Constructors
        internal ResultSurfaceAPI(string resultName, string name, MeshDataService meshService)
            : base(resultName, name, RegionTypeEnum.SurfaceName, meshService)
        {
        }


        // Methods
        [PythonAPI("Returns the result surface face IDs.", Returns = "IntList")]
        public IntListAPI get_face_ids()
        {
            return new IntListAPI(MeshService.GetSurfaceFaceIds(ResultName, name));
        }
        [PythonAPI("Returns the node IDs belonging to the result surface.", Returns = "IntList")]
        public IntListAPI get_node_ids()
        {
            return new IntListAPI(MeshService.GetSurfaceNodeIds(ResultName, name));
        }
        public override string ToString()
        {
            return "Surface(name=" + StringListAPI.ToDisplayString(name) +
                   ", faces=" + MeshService.GetSurfaceFaceIds(ResultName, name).Length +
                   ", nodes=" + MeshService.GetSurfaceNodeIds(ResultName, name).Length + ")";
        }
    }
}
