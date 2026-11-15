// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    [PmxType(2114)]
    public enum SweepSourceTypeEnum
    {
        [StandardValue("SelectedSurfaces", DisplayName = "Selected surfaces")]
        SelectedSurfaces,
        //
        [StandardValue("Automatic", DisplayName = "Automatic")]
        Automatic
    }
}
