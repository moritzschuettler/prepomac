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

namespace CaeMesh.Meshing
{
    /// <summary>
    /// How the element layers are distributed along the sweep direction.
    /// </summary>
    [Serializable]
    public enum SweepBiasTypeEnum
    {
        [StandardValue("None", DisplayName = "None")]
        None,
        [StandardValue("Progression", DisplayName = "Progression")]
        Progression,
        [StandardValue("Bump", DisplayName = "Bump")]
        Bump,
    }
}
