// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Where a node of a QuadWild source mesh lies: on a vertex, as the k-th node inside an edge in the order of the
    /// edge's parameter, or inside a surface as the k-th interior node of the mesh. The tags the nodes get in gmsh
    /// are found from this when the mesh is added, since the curve nodes are meshed again between the run and then.
    /// </summary>
    public struct SweepQuadWildNode
    {
        public int Dimension;
        public int Tag;
        public int Index;
        //
        public SweepQuadWildNode(int dimension, int tag, int index)
        {
            Dimension = dimension;
            Tag = tag;
            Index = index;
        }
    }
}
