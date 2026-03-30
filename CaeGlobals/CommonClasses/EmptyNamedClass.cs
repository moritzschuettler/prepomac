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

namespace CaeGlobals
{
    [Serializable]
    public class EmptyNamedClass : NamedClass
    {
        // Empty named class is used to trasfer the name only

        public EmptyNamedClass(string name)
            :base()
        {
            _checkName = false;     // the name may contain other cahracters - do not use constructor with name
            Name = name;
        }
    }
}
