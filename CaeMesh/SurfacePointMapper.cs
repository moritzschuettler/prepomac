// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;

namespace CaeMesh
{
    /// <summary>
    /// Carries a point on the surface of parts from one set of node coordinates to another of the same mesh,
    /// such as from a deformed and exploded display to the undeformed geometry. The point is located on the
    /// closest surface cell in the first set and rebuilt from the same cell nodes, with the same weights, in the
    /// second, so it stays on the same material point however the nodes moved.
    /// </summary>
    public static class SurfacePointMapper
    {
        // Variables                                                                                                                
        private static readonly int[] _triangle = new int[] { 0, 1, 2 };
        private static readonly int[] _quadrilateral = new int[] { 0, 1, 2, 0, 2, 3 };
        private static readonly int[] _parabolicTriangle = new int[] { 0, 3, 5, 3, 1, 4, 5, 4, 2, 3, 4, 5 };
        private static readonly int[] _parabolicQuadrilateral = new int[] { 0, 4, 7, 4, 1, 5, 5, 2, 6, 6, 3, 7,
                                                                            4, 5, 7, 5, 6, 7 };


        // Methods                                                                                                                  
        public static bool TryMapPoint(double[] point, IEnumerable<BasePart> parts, Dictionary<int, FeNode> fromNodes,
                                       Dictionary<int, FeNode> toNodes, out double[] mappedPoint)
        {
            mappedPoint = null;
            if (point == null || parts == null || fromNodes == null || toNodes == null) return false;
            //
            int[] cell;
            int[] triangles;
            double distance2;
            double[] weights = new double[3];
            double minDistance2 = double.MaxValue;
            int[] closestNodeIds = null;
            double[] closestWeights = null;
            FeNode n1;
            FeNode n2;
            FeNode n3;
            foreach (BasePart part in parts)
            {
                if (part == null || part.Visualization == null || part.Visualization.Cells == null) continue;
                //
                for (int i = 0; i < part.Visualization.Cells.Length; i++)
                {
                    cell = part.Visualization.Cells[i];
                    if (cell == null || cell.Length < 3) continue;
                    //
                    triangles = GetTriangles(cell.Length);
                    for (int j = 0; j < triangles.Length; j += 3)
                    {
                        if (!fromNodes.TryGetValue(cell[triangles[j]], out n1) ||
                            !fromNodes.TryGetValue(cell[triangles[j + 1]], out n2) ||
                            !fromNodes.TryGetValue(cell[triangles[j + 2]], out n3)) continue;
                        //
                        distance2 = GetClosestPointWeights(point, n1, n2, n3, weights);
                        if (distance2 < minDistance2)
                        {
                            minDistance2 = distance2;
                            closestNodeIds = new int[] { n1.Id, n2.Id, n3.Id };
                            closestWeights = (double[])weights.Clone();
                        }
                    }
                }
            }
            if (closestNodeIds == null) return false;
            //
            mappedPoint = new double[3];
            FeNode node;
            for (int i = 0; i < 3; i++)
            {
                if (!toNodes.TryGetValue(closestNodeIds[i], out node))
                {
                    mappedPoint = null;
                    return false;
                }
                mappedPoint[0] += closestWeights[i] * node.X;
                mappedPoint[1] += closestWeights[i] * node.Y;
                mappedPoint[2] += closestWeights[i] * node.Z;
            }
            // The distance of the point from the surface is kept, as an offset along the same direction
            double[] closestPoint = new double[3];
            for (int i = 0; i < 3; i++)
            {
                node = fromNodes[closestNodeIds[i]];
                closestPoint[0] += closestWeights[i] * node.X;
                closestPoint[1] += closestWeights[i] * node.Y;
                closestPoint[2] += closestWeights[i] * node.Z;
            }
            mappedPoint[0] += point[0] - closestPoint[0];
            mappedPoint[1] += point[1] - closestPoint[1];
            mappedPoint[2] += point[2] - closestPoint[2];
            return true;
        }
        private static int[] GetTriangles(int numOfNodes)
        {
            if (numOfNodes == 3) return _triangle;
            if (numOfNodes == 4) return _quadrilateral;
            if (numOfNodes == 6) return _parabolicTriangle;
            if (numOfNodes == 8) return _parabolicQuadrilateral;
            //
            int[] fan = new int[3 * (numOfNodes - 2)];
            for (int i = 0; i < numOfNodes - 2; i++)
            {
                fan[3 * i] = 0;
                fan[3 * i + 1] = i + 1;
                fan[3 * i + 2] = i + 2;
            }
            return fan;
        }
        // The closest point of the triangle as the weights of its three nodes; returns the squared distance
        private static double GetClosestPointWeights(double[] p, FeNode a, FeNode b, FeNode c, double[] weights)
        {
            double abx = b.X - a.X, aby = b.Y - a.Y, abz = b.Z - a.Z;
            double acx = c.X - a.X, acy = c.Y - a.Y, acz = c.Z - a.Z;
            double apx = p[0] - a.X, apy = p[1] - a.Y, apz = p[2] - a.Z;
            double d1 = abx * apx + aby * apy + abz * apz;
            double d2 = acx * apx + acy * apy + acz * apz;
            double v;
            double w;
            //
            if (d1 <= 0 && d2 <= 0) return SetWeights(p, a, b, c, 1, 0, 0, weights);
            //
            double bpx = p[0] - b.X, bpy = p[1] - b.Y, bpz = p[2] - b.Z;
            double d3 = abx * bpx + aby * bpy + abz * bpz;
            double d4 = acx * bpx + acy * bpy + acz * bpz;
            if (d3 >= 0 && d4 <= d3) return SetWeights(p, a, b, c, 0, 1, 0, weights);
            //
            double vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                v = d1 / (d1 - d3);
                return SetWeights(p, a, b, c, 1 - v, v, 0, weights);
            }
            //
            double cpx = p[0] - c.X, cpy = p[1] - c.Y, cpz = p[2] - c.Z;
            double d5 = abx * cpx + aby * cpy + abz * cpz;
            double d6 = acx * cpx + acy * cpy + acz * cpz;
            if (d6 >= 0 && d5 <= d6) return SetWeights(p, a, b, c, 0, 0, 1, weights);
            //
            double vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                w = d2 / (d2 - d6);
                return SetWeights(p, a, b, c, 1 - w, 0, w, weights);
            }
            //
            double va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return SetWeights(p, a, b, c, 0, 1 - w, w, weights);
            }
            //
            double denominator = va + vb + vc;
            if (denominator == 0) return SetWeights(p, a, b, c, 1, 0, 0, weights);
            v = vb / denominator;
            w = vc / denominator;
            return SetWeights(p, a, b, c, 1 - v - w, v, w, weights);
        }
        private static double SetWeights(double[] p, FeNode a, FeNode b, FeNode c, double wa, double wb, double wc,
                                         double[] weights)
        {
            weights[0] = wa;
            weights[1] = wb;
            weights[2] = wc;
            double dx = wa * a.X + wb * b.X + wc * c.X - p[0];
            double dy = wa * a.Y + wb * b.Y + wc * c.Y - p[1];
            double dz = wa * a.Z + wb * b.Z + wc * c.Z - p[2];
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
