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

namespace CaeGlobals
{
    public enum GeometrySelectModeEnum
    {
        [StandardValue("SelectLocation", DisplayName = "Selection by location")]
        SelectLocation,
        [StandardValue("SelectId", DisplayName = "Selection by ID")]
        SelectId
    }
}
