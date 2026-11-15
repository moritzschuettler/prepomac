// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeMesh
{
    /* Optimization of a linear mesh
    Netgen
    HighOrder
    HighOrderElastic
    HighOrderFastCurving
    Laplace2D
    Relocate2D
    Relocate3D
    QuadQuasiStructured
    UntangleMeshGeometry
    */
    [Serializable]
    [PmxType(2109)]
    public enum GmshOptimizeFirstOrderSolidEnum
    {
        [StandardValue("None", DisplayName = "None")]
        None,
        [StandardValue("Gmsh", DisplayName = "Gmsh")]
        Gmsh,
        [StandardValue("Netgen", DisplayName = "Netgen")]
        Netgen,
        [StandardValue("Relocate3D", DisplayName = "Relocate 3D")]
        Relocate3D,
    }
}
