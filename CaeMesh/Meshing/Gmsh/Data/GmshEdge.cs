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
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    [PmxType(2099)]
    public class GmshEdge : IComparable<GmshEdge>
    {
        [PmxKey(0)] public int Id;
        [PmxKey(1)] public int[] VertexIds;
        [PmxKey(2)] public HashSet<int> SurfaceIds;
        [PmxKey(3)] public double Length;
        //
        public GmshEdge(int id, int[] vertexIds, int surfaceId, double length)
        {
            Id = id;
            VertexIds = vertexIds;
            SurfaceIds = new HashSet<int> { surfaceId };
            Length = length;
        }
        //
        public int CompareTo(GmshEdge other)
        {
            if (Id < other.Id) return 1;
            else if (Id > other.Id) return -1;
            else return 0;
        }
    }
}
