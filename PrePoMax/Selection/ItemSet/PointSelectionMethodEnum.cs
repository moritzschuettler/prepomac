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

namespace PrePoMax
{
    [Serializable]
    public enum PointSelectionMethodEnum
    {
        [StandardValue("OnPoint", Description = "On point", DisplayName = "On point")]
        OnPoint,
        [StandardValue("BetweenTwoPoints", Description = "Between two points", DisplayName = "Between two points")]
        BetweenTwoPoints,
        [StandardValue("CircleCenter", Description = "Circle center by 3 points", DisplayName = "Circle center by 3 points")]
        CircleCenter
    }
}
