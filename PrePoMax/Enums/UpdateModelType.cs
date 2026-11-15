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

namespace PrePoMax
{
    [PmxType(10300)]
    [Flags]
    public enum UpdateType
    {
        None = 0,
        Check = 1,
        DrawModel = 2,
        DrawResults = 4,
        ResetCamera = 8,
        RedrawSymbols = 16
    }
}
