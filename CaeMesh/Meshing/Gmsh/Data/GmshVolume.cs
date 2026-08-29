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

namespace CaeMesh
{
    [Serializable]
    public class GmshVolume : IComparable<GmshVolume>
    {
        public int Id;
        public HashSet<int> SurfaceIds;
        public List<int> TriSurfIds;
        public List<int> QuadSurfIds;
        public bool Transfinite;
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
