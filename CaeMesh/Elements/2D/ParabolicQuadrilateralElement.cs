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
    public class ParabolicQuadrilateralElement : FeElement2D
    {
        // Variables                                                                                                                
        private static readonly int _vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_QUAD;
        private static readonly double _a = 1.0 / 3.0;
        private static readonly double _b = -1.0 / 12.0;
        private static readonly double _c = 1.0 / 6.0;
        private static readonly double _d = 4.0 / 6.0;
        static double[] _xi = new double[] { -1.0 / Math.Sqrt(3), 1.0 / Math.Sqrt(3), 1.0 / Math.Sqrt(3), -1.0 / Math.Sqrt(3) };
        static double[] _eta = new double[] { -1.0 / Math.Sqrt(3), -1.0 / Math.Sqrt(3), 1.0 / Math.Sqrt(3), 1.0 / Math.Sqrt(3) };
        static double[] _w = new double[] { 1.0, 1.0, 1.0, 1.0 };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ParabolicQuadrilateralElement(int id, int[] nodeIds)
         : base(id, nodeIds)
        {
        }
        public ParabolicQuadrilateralElement(int id, int partId, int[] nodeIds)
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
            // NEG S1 = 1-4-3-2-8-7-6-5 . 0-3-2-1-7-6-5-4
            // POS S2 = 1-2-3-4-5-6-7-8 . 0-1-2-3-4-5-6-7
            //     S3 = 1-2-5 . 0-1-4
            //     S4 = 2-3-6 . 1-2-5
            //     S5 = 3-4-7 . 2-3-6
            //     S6 = 4-1-8 . 3-0-7
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                       NodeIds[7], NodeIds[6], NodeIds[5], NodeIds[4] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3],
                                       NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[5] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[3], NodeIds[6] };
                case FeFaceName.S6:
                    return new int[] { NodeIds[3], NodeIds[0], NodeIds[7] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // NEG S1 = 1-4-3-2-8-7-6-5 . 0-3-2-1-7-6-5-4
            // POS S2 = 1-2-3-4-5-6-7-8 . 0-1-2-3-4-5-6-7
            //     S3 = 1-2-5 . 0-1-4
            //     S4 = 2-3-6 . 1-2-5
            //     S5 = 3-4-7 . 2-3-6
            //     S6 = 4-1-8 . 3-0-7
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                       NodeIds[7], NodeIds[6], NodeIds[5], NodeIds[4] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3],
                                       NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[5] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[3], NodeIds[6] };
                case FeFaceName.S6:
                    return new int[] { NodeIds[3], NodeIds[0], NodeIds[7] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[][] GetAllVtkCells()
        {
            int[][] cells = new int[6][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                       NodeIds[7], NodeIds[6], NodeIds[5], NodeIds[4] };
            cells[1] = new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3],
                                       NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7] };
            cells[2] = new int[] { NodeIds[0], NodeIds[1], NodeIds[4] };
            cells[3] = new int[] { NodeIds[1], NodeIds[2], NodeIds[5] };
            cells[4] = new int[] { NodeIds[2], NodeIds[3], NodeIds[6] };
            cells[5] = new int[] { NodeIds[3], NodeIds[0], NodeIds[7] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            // Check only first 4 nodes (as in linear element)
            int significantNodes = 4;
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
                // If three or more nodes were missed: break
                if (i + 1 - count >= 3) break;
            }
            //
            Dictionary<FeFaceName, double> faces = new Dictionary<FeFaceName, double>();
            //
            if (edgeFaces)
            {
                if (count >= 2)
                {
                    // S3 = 1-2-5 . 0-1-4
                    // S4 = 2-3-6 . 1-2-5
                    // S5 = 3-4-7 . 2-3-6
                    // S6 = 4-1-8 . 3-0-7
                    if (faceNodeIds[0] && faceNodeIds[1]) faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
                    if (faceNodeIds[1] && faceNodeIds[2]) faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
                    if (faceNodeIds[2] && faceNodeIds[3]) faces.Add(FeFaceName.S5, GetArea(FeFaceName.S5, nodes));
                    if (faceNodeIds[3] && faceNodeIds[0]) faces.Add(FeFaceName.S6, GetArea(FeFaceName.S6, nodes));
                }
            }
            else if (count == 4)
            {
                // POS S2 = 1-2-3 . 0-1-2
                if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[2] && faceNodeIds[3])
                    faces.Add(FeFaceName.S2, GetArea(FeFaceName.S2, nodes));
            }
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return new double[] { _b, _b, _b, _b, _a, _a, _a, _a };
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 ||
                     faceName == FeFaceName.S5 || faceName == FeFaceName.S6)
                return new double[] { _c, _c, _d };
            else throw new NotSupportedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return GetEquivalentForces(typeof(ParabolicQuadrilateralElement), nodalValues);
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 ||
                     faceName == FeFaceName.S5 || faceName == FeFaceName.S6)
                return GetEquivalentForces(typeof(ParabolicBeamElement), nodalValues);
            else throw new NotSupportedException();
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 8)
                return GeometryTools.RectangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                                   nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]]);
            else if (cell.Length == 3)
                return GeometryTools.BeamLength(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]]);
            else throw new NotSupportedException();
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 8)
                return GeometryTools.QuadrilateralCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                                 nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]], out area);
            else if (cell.Length == 3)
                return GeometryTools.BeamCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], out area);
            else throw new NotSupportedException();
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double area)
        {
            return GeometryTools.QuadrilateralCG(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                                                 nodes[NodeIds[3]], nodes[NodeIds[4]], nodes[NodeIds[5]],
                                                 nodes[NodeIds[6]], nodes[NodeIds[7]], out area);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodes[NodeIds[2]], nodes[NodeIds[3]],
                             nodes[NodeIds[4]], nodes[NodeIds[5]],
                             nodes[NodeIds[6]], nodes[NodeIds[7]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]],
                             nodalValues[NodeIds[4]], nodalValues[NodeIds[5]],
                             nodalValues[NodeIds[6]], nodalValues[NodeIds[7]]);
        }
        private static double Integrate(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                       FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                       double u1, double u2, double u3, double u4,
                                       double u5, double u6, double u7, double u8)
        {
            double integral = 0;
            //
            for (int gp = 0; gp < _w.Length; gp++)
            {
                double xi = _xi[gp];
                double eta = _eta[gp];
                //
                double u = Interpolate(u1, u2, u3, u4, u5, u6, u7, u8, xi, eta);
                //
                double jNorm = JNorm(n1, n2, n3, n4, n5, n6, n7, n8, xi, eta);
                //
                integral += _w[gp] * u * jNorm;
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3, double u4,
                                          double u5, double u6, double u7, double u8,
                                          double g, double h)
        {
            return -0.25 * (1 - g) * (1 - h) * (1 + g + h) * u1 -
                   0.25 * (1 + g) * (1 - h) * (1 - g + h) * u2 -
                   0.25 * (1 + g) * (1 + h) * (1 - g - h) * u3 -
                   0.25 * (1 - g) * (1 + h) * (1 + g - h) * u4 +
                    0.5 * (1 - g) * (1 + g) * (1 - h) * u5 +
                    0.5 * (1 - h) * (1 + h) * (1 + g) * u6 +
                    0.5 * (1 - g) * (1 + g) * (1 + h) * u7 +
                    0.5 * (1 - h) * (1 + h) * (1 - g) * u8;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                    FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                    double g, double h)
        {
            // dN/dg
            double n1g = 0.25 * (1 - h) * (2 * g + h);
            double n2g = 0.25 * (1 - h) * (2 * g - h);
            double n3g = 0.25 * (1 + h) * (2 * g + h);
            double n4g = 0.25 * (1 + h) * (2 * g - h);
            double n5g = -g * (1 - h);
            double n6g = 0.5 * (1 - h * h);
            double n7g = -g * (1 + h);
            double n8g = -0.5 * (1 - h * h);
            // dN/dh
            double n1h = 0.25 * (1 - g) * (g + 2 * h);
            double n2h = 0.25 * (1 + g) * (-g + 2 * h);
            double n3h = 0.25 * (1 + g) * (g + 2 * h);
            double n4h = 0.25 * (1 - g) * (-g + 2 * h);
            double n5h = -0.5 * (1 - g * g);
            double n6h = -h * (1 + g);
            double n7h = 0.5 * (1 - g * g);
            double n8h = -h * (1 - g);
            // dx/dg
            double xg = n1g * n1.X + n2g * n2.X + n3g * n3.X + n4g * n4.X +
                        n5g * n5.X + n6g * n6.X + n7g * n7.X + n8g * n8.X;
            double yg = n1g * n1.Y + n2g * n2.Y + n3g * n3.Y + n4g * n4.Y +
                        n5g * n5.Y + n6g * n6.Y + n7g * n7.Y + n8g * n8.Y;
            double zg = n1g * n1.Z + n2g * n2.Z + n3g * n3.Z + n4g * n4.Z +
                        n5g * n5.Z + n6g * n6.Z + n7g * n7.Z + n8g * n8.Z;
            // dx/dh
            double xh = n1h * n1.X + n2h * n2.X + n3h * n3.X + n4h * n4.X +
                        n5h * n5.X + n6h * n6.X + n7h * n7.X + n8h * n8.X;
            double yh = n1h * n1.Y + n2h * n2.Y + n3h * n3.Y + n4h * n4.Y +
                        n5h * n5.Y + n6h * n6.Y + n7h * n7.Y + n8h * n8.Y;
            double zh = n1h * n1.Z + n2h * n2.Z + n3h * n3.Z + n4h * n4.Z +
                        n5h * n5.Z + n6h * n6.Z + n7h * n7.Z + n8h * n8.Z;
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
            // 0-1-2-3-4-5-6-7-8 -> 0-3-2-1-7-6-5-4
            NodeIds = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                  NodeIds[7], NodeIds[6], NodeIds[5], NodeIds[4] };
        }
        //
        public override FeElement DeepCopy()
        {
            return new ParabolicQuadrilateralElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
