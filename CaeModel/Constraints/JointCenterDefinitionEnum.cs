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
    public enum JointCenterDefinitionEnum
    {
        [StandardValue("Master surface", "Master surface", Description =
                       "Compute the joint centre from the master surface: its centroid.")]
        MasterSurface,
        [StandardValue("Coordinates", "Coordinates", Description = "Enter the joint centre coordinates.")]
        Coordinates,
        [StandardValue("On point", "On point", Description = "Select a node lying at the joint centre.")]
        OnPoint,
        [StandardValue("Between two points", "Between two points", Description =
                       "Select two nodes; the joint centre is the point midway between them.")]
        BetweenTwoPoints,
        [StandardValue("Circle center by 3 points", "Circle center by 3 points", Description =
                       "Select three nodes lying on a circle; the joint centre is the centre of that circle.")]
        CircleCenter
    }
}
