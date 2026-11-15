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
    [PmxType(2049)]
    public enum ConvertPyramidsToEnum
    {
        [StandardValue("Wedges", DisplayName = "Collapsed wedges")]
        Wedges,
        [StandardValue("Hexahedrons", DisplayName = "Collapsed hexahedrons")]
        Hexahedrons
    }

}
