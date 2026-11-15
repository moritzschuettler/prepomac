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
    [Serializable]
    [PmxType(2102)]
    public enum ElementSizeTypeEnum
    {
        [StandardValue("ScaleFactor", DisplayName = "Scale factor")]
        ScaleFactor,
        [StandardValue("NumberOfElements", DisplayName = "Number of elements")]
        NumberOfElements,
        [StandardValue("MultiLayerd", DisplayName = "Multi-layered")]
        MultiLayerd,
    }
}
