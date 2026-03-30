// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace CaeResults
{
    [Serializable]
    public enum ComplexResultTypeEnum
    {
        [StandardValue("Real", DisplayName = "Real", Description = "Real")]
        Real,
        [StandardValue("Imaginary", DisplayName = "Imaginary", Description = "Imaginary")]
        Imaginary,
        [StandardValue("Magnitude", DisplayName = "Magnitude", Description = "Magnitude")]
        Magnitude,
        [StandardValue("Phase", DisplayName = "Phase", Description = "Phase")]
        Phase,
        [StandardValue("Angle", DisplayName = "Angle", Description = "Angle")]
        RealAtAngle,
        [StandardValue("Max", DisplayName = "Max", Description = "Max")]
        Max,
        [StandardValue("AngleAtMax", DisplayName = "Angle at max", Description = "Angle at max")]
        AngleAtMax,
        [StandardValue("Min", DisplayName = "Min", Description = "Min")]
        Min,
        [StandardValue("AngleAtMin", DisplayName = "Angle at min", Description = "Angle at min")]
        AngleAtMin
    }
   
}
