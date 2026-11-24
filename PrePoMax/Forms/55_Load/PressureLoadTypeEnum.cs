// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    public enum PressureLoadTypeEnum
    {
        [StandardValue("Standard", "Standard", Description = "The pressure is given by its magnitude and an optional " +
                                                             "distribution.")]
        Standard,
        [StandardValue("Hydrostatic", "Hydrostatic", Description = "The pressure changes linearly between two points " +
                                                                   "in the given direction.")]
        Hydrostatic,
        [StandardValue("Imported", "Imported", Description = "The pressure is imported from an OpenFOAM results file.")]
        Imported
    }
}
