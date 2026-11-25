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
    [PmxType(2101)]
    public class GmshVolume : IComparable<GmshVolume>
    {
        [PmxKey(0)] public int Id;
        [PmxKey(1)] public HashSet<int> SurfaceIds;
        [PmxKey(2)] public List<int> TriSurfIds;
        [PmxKey(3)] public List<int> QuadSurfIds;
        [PmxKey(4)] public bool Transfinite;
        //
        public int NumTriSurfaces { get { return TriSurfIds.Count(); } }
        public int NumQuadSurfaces { get { return QuadSurfIds.Count(); } }
        //
        public GmshVolume(int id, int surfaceId)
        {
            Id = id;
            SurfaceIds = new HashSet<int> { surfaceId };
            TriSurfIds = new List<int>();
            QuadSurfIds = new List<int>();
            Transfinite = false;
        }
        //
        public int CompareTo(GmshVolume other)
        {
            if (Id < other.Id) return 1;
            else if (Id > other.Id) return -1;
            else return 0;
        }
    }
}
