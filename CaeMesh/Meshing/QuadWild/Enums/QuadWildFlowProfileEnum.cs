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

namespace CaeMesh
{
    [Serializable]
    public enum QuadWildFlowProfileEnum
    {
        [StandardValue("Aligned", DisplayName = "Aligned")]
        Aligned,
        [StandardValue("NoAlignment", DisplayName = "No alignment")]
        NoAlignment
    }
}
