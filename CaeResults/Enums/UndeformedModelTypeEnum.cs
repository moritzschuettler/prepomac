// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace CaeResults
{
    [Serializable]
    public enum UndeformedModelTypeEnum
    {
        [StandardValue("None", Description = "None")]
        None,
        //
        [StandardValue("WireframeBody", DisplayName = "Wireframe body", Description = "Wireframe body")]
        WireframeBody,
        //
        [StandardValue("SolidBody", DisplayName = "Solid body", Description = "Solid body")]
        SolidBody
    }
}
