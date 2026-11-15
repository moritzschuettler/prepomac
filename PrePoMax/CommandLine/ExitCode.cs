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

namespace PrePoMax
{
    // The process exit code contract for unattended runs. These values are part of the command line
    // interface and must stay stable.
    [PmxType(10017)]
    public enum ExitCodeEnum
    {
        Ok = 0,
        OptionError = 1,
        ModelError = 2,
        ScriptError = 3,
        TimedOut = 4,
        PromptRefused = 5,
        LauncherError = 6,
        Cancelled = 7
    }
}
