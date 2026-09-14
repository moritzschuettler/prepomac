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
    public enum JointTypeEnum
    {
        [StandardValue("Hinge", "Hinge", Description =
                       "The two regions share the joint centre and rotate relative to each other only about the joint axis.")]
        Hinge,
        [StandardValue("Cylindrical", "Cylindrical", Description =
                       "The two regions rotate about and translate along the joint axis relative to each other.")]
        Cylindrical,
        [StandardValue("Slider", "Slider", Description =
                       "The two regions translate along the joint axis relative to each other and do not rotate.")]
        Slider,
        [StandardValue("Ball", "Ball", Description =
                       "The two regions share the joint centre and rotate freely relative to each other.")]
        Ball,
        [StandardValue("Weld", "Weld", Description =
                       "The two regions move as one rigid body.")]
        Weld
    }
}
