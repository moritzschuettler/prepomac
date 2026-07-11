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
    internal class ElementInfo
    {
        public readonly int Id;
        public readonly string TypeName;
        public readonly int Dimension;
        public readonly int[] NodeIds;

        public ElementInfo(int id, string typeName, int dimension, int[] nodeIds)
        {
            Id = id;
            TypeName = typeName;
            Dimension = dimension;
            NodeIds = nodeIds;
        }
    }
}
