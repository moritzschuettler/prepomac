// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh
{
    // The names are the ones gmsh takes for its element quality measures
    [Serializable]
    public enum GmshElementQualityMetricEnum
    {
        minDetJac,
        maxDetJac,
        minSJ,
        minSICN,
        minSIGE,
        gamma,
        innerRadius,
        outerRadius,
        minIsotropy,
        angleShape,
        minEdge,
        maxEdge,
        volume
    }
}
