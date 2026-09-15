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
    public enum JointFormulationEnum
    {
        [StandardValue("Rigid body", "Rigid body", Description =
                       "Each region becomes a rigid body whose reference node sits at the joint centre. The joint is exact, " +
                       "but a rigid body on shell elements does not converge in a geometrically nonlinear step.")]
        RigidBody,
        [StandardValue("Springs", "Springs", Description =
                       "Each region node is connected by axial springs to nodes on the joint axis, and the axis nodes of the " +
                       "two regions are tied by linear equations. The joint is as stiff as its springs; it needs no rigid " +
                       "body and solves on shell elements.")]
        Springs
    }
}
