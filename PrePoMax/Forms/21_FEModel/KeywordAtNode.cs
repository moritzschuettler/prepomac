// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using FileInOut.Output.Calculix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    [PmxType(10340)]
    class KeywordAtNode
    {
        [PmxKey(0)] public CalculixKeyword Keyword;
        [PmxKey(1)] public string Data;
        [PmxKey(2)] public int NumOfLines;
        [PmxKey(3)] public int[] GeneratedIndices;
        [PmxKey(4)] public string OriginalSignature;
        [PmxKey(5)] public CalculixKeywordOverride KeywordOverride;
    }
}
