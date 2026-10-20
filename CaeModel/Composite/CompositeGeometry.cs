// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using CaeGlobals;
using CaeMesh;

namespace CaeModel
{
    internal static class CompositeGeometry
    {
        internal static Vec3D ComputeElementNormal(FeElement element, Dictionary<int, FeNode> nodes)
        {
            // For both 2D shell and 3D solid elements: cross(N1-N0, N2-N0) gives the
            // face normal (through-thickness direction for layered solid meshes).
            FeNode n0 = nodes[element.NodeIds[0]];
            FeNode n1 = nodes[element.NodeIds[1]];
            FeNode n2 = nodes[element.NodeIds[2]];
            Vec3D v1 = new Vec3D(n1.X - n0.X, n1.Y - n0.Y, n1.Z - n0.Z);
            Vec3D v2 = new Vec3D(n2.X - n0.X, n2.Y - n0.Y, n2.Z - n0.Z);
            Vec3D normal = Vec3D.CrossProduct(v1, v2);
            double len = normal.Normalize();
            if (len < 1e-6) return new Vec3D(0, 0, 1);
            return normal;
        }
    }
}
