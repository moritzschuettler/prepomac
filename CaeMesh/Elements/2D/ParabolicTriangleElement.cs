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
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public class ParabolicTriangleElement : FeElement2D
    {
        // Variables                                                                                                                
        private static readonly int _vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_TRIANGLE;
        private static readonly double _a = 1.0 / 3.0;
        private static readonly double _b = 1.0 / 6.0;
        private static readonly double _c = 4.0 / 6.0;
        private static readonly double[] _xi = new double[]
        {
            0.091576213509771,
            0.816847572980459,
            0.091576213509771,
            0.445948490915965,
            0.108103018168070,
            0.445948490915965
        };
        private static readonly double[] _eta = new double[]
        {
            0.091576213509771,
            0.091576213509771,
            0.816847572980459,
            0.445948490915965,
            0.445948490915965,
            0.108103018168070
        };
        private static readonly double[] _w = new double[]
        {
            0.109951743655322 * 0.5,
            0.109951743655322 * 0.5,
            0.109951743655322 * 0.5,
            0.223381589678011 * 0.5,
            0.223381589678011 * 0.5,
            0.223381589678011 * 0.5
        };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ParabolicTriangleElement(int id, int[] nodeIds)
         : base(id, nodeIds)
        {
        }
        public ParabolicTriangleElement(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        public override int[] GetVtkNodeIds()
        {
            // return a copy -> ToArray
            return NodeIds.ToArray();
        }
        public override int[] GetGmshNodeIds()
        {
            // return a copy -> ToArray
            return NodeIds.ToArray();
        }
        public override int GetVtkCellType()
        {
            return _vtkCellTypeInt;
        }
        public override FeFaceName GetFaceNameFromSortedNodeIds(int[] nodeIds)
        {
            throw new NotImplementedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // NEG S1 = 1-3-2-6-5-4  . 0-2-1-5-4-3
            // POS S2 = 1-2-3-4-5-6  . 0-1-2-3-4-5
            //     S3 = 1-2-4 . 0-1-3
            //     S4 = 2-3-5 . 1-2-4
            //     S5 = 3-1-6 . 2-0-5
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[5], NodeIds[4], NodeIds[3] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3], NodeIds[4], NodeIds[5] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[3] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[4] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[0], NodeIds[5] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // NEG S1 = 1-3-2-6-5-4  . 0-2-1-5-4-3
            // POS S2 = 1-2-3-4-5-6  . 0-1-2-3-4-5
            //     S3 = 1-2-4 . 0-1-3
            //     S4 = 2-3-5 . 1-2-4
            //     S5 = 3-1-6 . 2-0-5
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[5], NodeIds[4], NodeIds[3] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3], NodeIds[4], NodeIds[5] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[3] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[4] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[0], NodeIds[5] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[][] GetAllVtkCells()
        {
            int[][] cells = new int[5][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[5], NodeIds[4], NodeIds[3] };
            cells[1] = new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3], NodeIds[4], NodeIds[5] };
            cells[2] = new int[] { NodeIds[0], NodeIds[1], NodeIds[3] };
            cells[3] = new int[] { NodeIds[1], NodeIds[2], NodeIds[4] };
            cells[4] = new int[] { NodeIds[2], NodeIds[0], NodeIds[5] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            // Check only first 3 nodes (as in linear element)
            int significantNodes = 3;
            bool[] faceNodeIds = new bool[significantNodes];
            //
            int count = 0;
            for (int i = 0; i < significantNodes; i++)
            {
                if (nodeSet.Contains(NodeIds[i]))
                {
                    faceNodeIds[i] = true;
                    count++;
                }
                // If two or more nodes were missed: break
                if (i + 1 - count >= 2) break;
            }
            Dictionary<FeFaceName, double> faces = new Dictionary<FeFaceName, double>();
            //
            if (edgeFaces)
            {
                if (count >= 2)
                {
                    // S3 = 1-2-4 . 0-1-3
                    // S4 = 2-3-5 . 1-2-4
                    // S5 = 3-1-6 . 2-0-5
                    if (faceNodeIds[0] && faceNodeIds[1]) faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
                    if (faceNodeIds[1] && faceNodeIds[2]) faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
                    if (faceNodeIds[2] && faceNodeIds[0]) faces.Add(FeFaceName.S5, GetArea(FeFaceName.S5, nodes));
                }
            }
            else if (count == 3)
            {
                // POS S2 = 1-2-3 . 0-1-2
                if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[2]) faces.Add(FeFaceName.S2, GetArea(FeFaceName.S2, nodes));
            }
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return new double[] { 0, 0, 0, _a, _a, _a };
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                return new double[] { _b, _b, _c };
            else throw new NotSupportedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return GetEquivalentForces(typeof(ParabolicTriangleElement), nodalValues);
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                return GetEquivalentForces(typeof(ParabolicBeamElement), nodalValues);
            else throw new NotSupportedException();
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 6)
                return GeometryTools.TriangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                                  nodes[cell[3]], nodes[cell[4]], nodes[cell[5]]);
            else if (cell.Length == 3)
                return GeometryTools.BeamLength(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]]);
            else throw new NotSupportedException();
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 6)
                return GeometryTools.TriangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                                nodes[cell[3]], nodes[cell[4]], nodes[cell[5]], out area);
            else if (cell.Length == 3)
                return GeometryTools.BeamCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], out area);
            else throw new NotSupportedException();
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double area)
        {
            return GeometryTools.TriangleCG(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                                            nodes[NodeIds[3]], nodes[NodeIds[4]], nodes[NodeIds[5]], out area);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodes[NodeIds[2]], nodes[NodeIds[3]],
                             nodes[NodeIds[4]], nodes[NodeIds[5]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]],
                             nodalValues[NodeIds[4]], nodalValues[NodeIds[5]], IntegrationScale.None);
        }
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues,
                                         IntegrationScale scale)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodes[NodeIds[2]], nodes[NodeIds[3]],
                             nodes[NodeIds[4]], nodes[NodeIds[5]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]],
                             nodalValues[NodeIds[4]], nodalValues[NodeIds[5]], scale);
        }
        private static double Integrate(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, FeNode n6,
                                       double u1, double u2, double u3, double u4, double u5, double u6,
                                       IntegrationScale scale)
        {
            double integral = 0;
            //
            for (int gp = 0; gp < _w.Length; gp++)
            {
                double xi = _xi[gp];
                double eta = _eta[gp];
                //
                double u = Interpolate(u1, u2, u3, u4, u5, u6, xi, eta);
                //
                double jNorm = JNorm(n1, n2, n3, n4, n5, n6, xi, eta);
                //
                double weight = scale.IsUnity ? 1.0
                    : scale.GetWeight(Interpolate(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, xi, eta));
                //
                integral += _w[gp] * u * jNorm * weight;
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3, double u4, double u5, double u6, double g, double h)
        {
            double gh1 = 1 - g - h;
            //
            return 2 * gh1 * (gh1 - 0.5) * u1 +
                   2 * g * (g - 0.5) * u2 +
                   2 * h * (h - 0.5) * u3 +
                   4 * g * gh1 * u4 +
                   4 * g * h * u5 +
                   4 * h * gh1 * u6;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, FeNode n6, double g, double h)
        {
            double gh1 = 1 - g - h;
            // Derivatives by g
            double n1g = -4 * gh1 + 1;
            double n2g = 4 * g - 1;
            double n3g = 0;
            double n4g = 4 * (gh1 - g);
            double n5g = 4 * h;
            double n6g = -4 * h;
            // Derivatives by h
            double n1h = -4 * gh1 + 1;
            double n2h = 0;
            double n3h = 4 * h - 1;
            double n4h = -4 * g;
            double n5h = 4 * g;
            double n6h = 4 * (gh1 - h);
            // dx/dg
            double xg = n1g * n1.X + n2g * n2.X + n3g * n3.X +
                        n4g * n4.X + n5g * n5.X + n6g * n6.X;
            double yg = n1g * n1.Y + n2g * n2.Y + n3g * n3.Y +
                        n4g * n4.Y + n5g * n5.Y + n6g * n6.Y;
            double zg = n1g * n1.Z + n2g * n2.Z + n3g * n3.Z +
                        n4g * n4.Z + n5g * n5.Z + n6g * n6.Z;
            // dx/dh
            double xh = n1h * n1.X + n2h * n2.X + n3h * n3.X +
                        n4h * n4.X + n5h * n5.X + n6h * n6.X;
            double yh = n1h * n1.Y + n2h * n2.Y + n3h * n3.Y +
                        n4h * n4.Y + n5h * n5.Y + n6h * n6.Y;
            double zh = n1h * n1.Z + n2h * n2.Z + n3h * n3.Z +
                        n4h * n4.Z + n5h * n5.Z + n6h * n6.Z;
            // |dx/dg x dx/dh|
            double cx = yg * zh - zg * yh;
            double cy = zg * xh - xg * zh;
            double cz = xg * yh - yg * xh;
            //
            return Math.Sqrt(cx * cx + cy * cy + cz * cz);
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3-4-5 -> 0-2-1-5-4-3
            NodeIds = new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[5], NodeIds[4], NodeIds[3] };
        }
        //
        public override FeElement DeepCopy()
        {
            return new ParabolicTriangleElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
