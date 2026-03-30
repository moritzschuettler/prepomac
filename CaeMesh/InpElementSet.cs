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

namespace CaeMesh
{
    public class InpElementSet
    {
        // Properties                                                                                                               
        public string Name { get; set; }
        public HashSet<string> InpElementTypeNames { get; set; }
        public HashSet<int> ElementLabels { get; set; }


        // Constructors                                                                                                             
        public InpElementSet(string name, HashSet<string> inpElementTypeName, HashSet<int> elementLabels)
        {
            Name = name;
            InpElementTypeNames = inpElementTypeName;
            ElementLabels = elementLabels;
        }
    }
}
