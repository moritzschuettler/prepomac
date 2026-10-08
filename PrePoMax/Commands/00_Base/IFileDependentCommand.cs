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
using CaeGlobals;

namespace PrePoMax.Commands
{
    // Command whose model item reads data from an external file (distribution, defined temperature, submodel...)
    public interface IFileDependentCommand
    {
        // Variables


        // Properties
        string FileName { get; set; }   // null if the model item does not use a file


        // Constructors


        // Methods
    }
}
