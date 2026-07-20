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
        [PythonAPI("The result surface face IDs.", Returns = "numpy.ndarray or IntList")]
        public object face_ids
        {
            get { return NumPyConverter.ToPythonArray(MeshService.GetSurfaceFaceIds(ResultName, name)); }
        }
        [PythonAPI("The node IDs belonging to the result surface.", Returns = "numpy.ndarray or IntList")]
        public object node_ids
        {
            get { return NumPyConverter.ToPythonArray(MeshService.GetSurfaceNodeIds(ResultName, name)); }
        }
        [PythonAPI("The result surface area.", Returns = "float")]
        public double area { get { return MeshService.GetSurfaceArea(ResultName, name); } }


        // Constructors
        internal ResultSurfaceAPI(string resultName, string name, MeshDataService meshService)
            : base(resultName, name, RegionTypeEnum.SurfaceName, meshService)
        {
        }


        // Methods
        public override string ToString()
        {
            return "Surface(name=" + StringListAPI.ToDisplayString(name) +
                   ", faces=" + MeshService.GetSurfaceFaceIds(ResultName, name).Length +
                   ", nodes=" + MeshService.GetSurfaceNodeIds(ResultName, name).Length + ")";
        }
    }
}
