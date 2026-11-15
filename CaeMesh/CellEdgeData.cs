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
    [PmxType(2001)]
    public class CellEdgeData
    {
        [PmxKey(0)] public int[] Key;
        [PmxKey(1)] public int[] NodeIds;
        [PmxKey(2)] public List<int> CellIds;
    }
}
