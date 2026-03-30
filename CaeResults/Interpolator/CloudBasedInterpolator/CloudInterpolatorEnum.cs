// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeResults
{
    public enum CloudInterpolatorEnum
    {
        [StandardValue("ClosestPoint", DisplayName = "Closest point")]
        ClosestPoint,
        [StandardValue("Gauss", DisplayName = "Gauss")]
        Gauss,
        [StandardValue("Shepard", DisplayName = "Shepard")]
        Shepard
    }
}
