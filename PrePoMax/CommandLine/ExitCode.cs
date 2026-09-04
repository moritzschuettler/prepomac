// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax
{
    // The process exit code contract for unattended runs. These values are part of the command line
    // interface and must stay stable.
    [Serializable]
    public enum ExitCodeEnum
    {
        Ok = 0,
        OptionError = 1,
        ModelError = 2,
        ScriptError = 3,
        TimedOut = 4,
        PromptRefused = 5,
        LauncherError = 6
    }
}
