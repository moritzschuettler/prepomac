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
    public enum JointStiffnessDefinitionEnum
    {
        [StandardValue("From model", "From model", Description =
                       "Compute the stiffness of a single spring as 100 * E * sqrt(A) / n, where E is the largest Young's " +
                       "modulus of the elements carrying the two regions, A is the average area of the two regions and n " +
                       "is the average number of their nodes. The springs of one region therefore carry 100 * E * sqrt(A) " +
                       "together, whatever the mesh density, which keeps the joint much stiffer than the regions it joins.")]
        FromModel,
        [StandardValue("User defined", "User defined", Description = "Enter the stiffness of each spring.")]
        UserDefined
    }
}
