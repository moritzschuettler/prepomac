// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using CaeMesh;
using CaeResults;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax.API.Python
{
    internal class PartData
    {
        public readonly int[] NodeIds;
        public readonly int[] ElementIds;

        public PartData(int[] nodeIds, int[] elementIds)
        {
            NodeIds = nodeIds;
            ElementIds = elementIds;
        }
    }
}
