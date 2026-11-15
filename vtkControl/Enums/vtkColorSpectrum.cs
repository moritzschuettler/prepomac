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
using CaeGlobals;


namespace vtkControl
{
    [PmxType(6100)]
    public enum vtkColorSpectrumMode
    {
        Discrete,
        Smooth
    }

    [PmxType(6101)]
    public enum vtkColorSpectrumType
    {
        CoolWarm,
        Rainbow,
        RainbowDesaturated,
        Turbo,
        Jet,
        Warm,
        Cool,
        Cividis,
        Viridis,
        Plasma,
        BlackBody,
        Inferno,
        Kindlmann,
        Grayscale

    }
}
