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
    [Serializable]
    [PmxType(2130)]
    public class CompareFeNodeByX : IComparer<FeNode>
    {
        public int Compare(FeNode n1, FeNode n2)
        {
            if (n1.X > n2.X) return 1;
            else if (n1.X < n2.X) return -1;
            else return 0;
        }
    }
}
