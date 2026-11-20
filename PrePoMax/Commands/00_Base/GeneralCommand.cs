// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [PmxType(10220)]
    public abstract class GeneralCommand : Command
    {
        // Constructor                                                                                                              
        public GeneralCommand(string name)
            : base(name)
        {
        }
    }
}
