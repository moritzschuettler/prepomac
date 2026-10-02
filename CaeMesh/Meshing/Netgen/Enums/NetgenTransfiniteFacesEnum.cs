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
        // The values are the transfinite_faces entry of the Netgen mesh parameters file. Netgen meshes the faces the one
        // definition all meshers follow finds, so 1, a size check written by an earlier version, is read as All
        [StandardValue("None", DisplayName = "None")]
        None = 0,
        [StandardValue("All", DisplayName = "All")]
        All = 2,
    }
}
