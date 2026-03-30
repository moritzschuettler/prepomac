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

namespace FileInOut.Input
{
    [Serializable]
    [Flags]
    public enum ElementsToImport
    {
        // Must start from 1 othervise the 0 value has no effect
        Beam = 1,
        Shell = 2,
        Solid = 4,
        All = Beam | Shell | Solid
    }
}
