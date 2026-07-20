// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace APIs.Python
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PythonAPIAttribute : Attribute
    {
        // Properties
        public string Description { get; private set; }
        public string Arguments { get; set; }
        public string Returns { get; set; }
        public string Example { get; set; }


        // Constructors
        public PythonAPIAttribute(string description)
        {
            Description = description;
            Arguments = null;
            Returns = null;
            Example = null;
        }
    }
}
