// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeMesh
{
    // The standard metrics come first; the rest are the element quality measures of gmsh, named as gmsh takes them
    [PmxType(2153)]
    public enum ElementQualityMetricEnum
    {
        [StandardValue("ScaledJacobian", DisplayName = "Scaled Jacobian")]
        ScaledJacobian,
        [StandardValue("AspectRatio", DisplayName = "Aspect ratio")]
        AspectRatio,
        [StandardValue("EdgeRatio", DisplayName = "Edge ratio")]
        EdgeRatio,
        [StandardValue("MinimumAngle", DisplayName = "Min angle")]
        MinimumAngle,
        [StandardValue("MaximumAngle", DisplayName = "Max angle")]
        MaximumAngle,
        [StandardValue("Skew", DisplayName = "Skew")]
        Skew,
        [StandardValue("Warpage", DisplayName = "Warpage")]
        Warpage,
        [StandardValue("Taper", DisplayName = "Taper")]
        Taper,
        [StandardValue("minDetJac", DisplayName = "Min Jacobian determinant")]
        minDetJac,
        [StandardValue("maxDetJac", DisplayName = "Max Jacobian determinant")]
        maxDetJac,
        [StandardValue("minSJ", DisplayName = "Min scaled Jacobian")]
        minSJ,
        [StandardValue("minSICN", DisplayName = "Min signed inverse condition number")]
        minSICN,
        [StandardValue("minSIGE", DisplayName = "Min signed inverse gradient error")]
        minSIGE,
        [StandardValue("gamma", DisplayName = "Gamma")]
        gamma,
        [StandardValue("innerRadius", DisplayName = "Inner radius")]
        innerRadius,
        [StandardValue("outerRadius", DisplayName = "Outer radius")]
        outerRadius,
        [StandardValue("minIsotropy", DisplayName = "Min isotropy")]
        minIsotropy,
        [StandardValue("angleShape", DisplayName = "Angle shape")]
        angleShape,
        [StandardValue("minEdge", DisplayName = "Min edge length")]
        minEdge,
        [StandardValue("maxEdge", DisplayName = "Max edge length")]
        maxEdge,
        [StandardValue("volume", DisplayName = "Volume")]
        volume
    }
}
