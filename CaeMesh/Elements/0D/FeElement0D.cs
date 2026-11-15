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

namespace CaeMesh
{
    [PmxType(2005)]
    public abstract class FeElement0D : FeElement
    {
        // Properties                                                                                                               
        public override int Dimension { get { return 0; } }


        // Constructors                                                                                                             
        public FeElement0D(int id, int[] nodeIds)
            : base (id, nodeIds)
        {
        }
        public FeElement0D(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            throw new NotSupportedException();
        }
    }
}
