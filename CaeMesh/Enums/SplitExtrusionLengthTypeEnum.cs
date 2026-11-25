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
    [PmxType(2060)]
    public enum SplitExtrusionLengthTypeEnum
    {
        [StandardValue("ThroughRegion", DisplayName = "Through the region")]
        ThroughRegion,
        [StandardValue("Value", DisplayName = "Value")]
        Value
    }

}
