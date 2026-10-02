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

namespace CaeMesh
{
    [Serializable]
    public enum NetgenTransfiniteFacesEnum
    {
        // The Netgen transfinite setting of an earlier version, kept so that its files still load: any value but None
        // is read as the 4-sided faces on
        [StandardValue("None", DisplayName = "None")]
        None = 0,
        [StandardValue("All", DisplayName = "All")]
        All = 2,
    }
}
