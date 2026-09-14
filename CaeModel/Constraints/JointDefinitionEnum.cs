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

namespace CaeModel
{
    [Serializable]
    public enum JointDefinitionEnum
    {
        [StandardValue("Master surface", "Master surface", Description =
                       "Compute the value from the master surface: its centroid for the joint centre and, for a cylindrical " +
                       "surface, the direction perpendicular to all its face normals for the joint axis.")]
        MasterSurface,
        [StandardValue("User defined", "User defined", Description = "Enter the value.")]
        UserDefined
    }
}
