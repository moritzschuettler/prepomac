// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh.Meshing
{
    [Serializable]
    public enum GmshAlgorithmMesh3DEnum
    {
        [StandardValue("Delaunay", DisplayName = "Delaunay")]
        Delaunay = 1,
        [StandardValue("InitialMeshOnly", DisplayName = "Initial mesh only")]
        InitialMeshOnly = 3,
        [StandardValue("Frontal", DisplayName = "Frontal")]
        Frontal = 4,
        [StandardValue("MMG3D", DisplayName = "MMG3D")]
        MMG3D = 7,
        [StandardValue("RTree", DisplayName = "R-tree")]
        RTree = 9,
        [StandardValue("HXT", DisplayName = "HXT")]
        HXT = 10,
    }
}
