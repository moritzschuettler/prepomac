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
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    public class LinearHexaElement : FeElement3D
    {
        // Variables                                                                                                                
        private static int _vtkCellTypeInt = (int)vtkCellType.VTK_HEXAHEDRON;
        private static double _b = 0.25;
        static double[] _xi = new double[]
        {
            -1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3)
        };
        static double[] _eta = new double[]
        {
            -1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3)
        };
        static double[] _zeta = new double[]
        {
            -1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3)
        };
        static double[] _w = new double[]
        {
            1.0,
            1.0,
            1.0,
            1.0,
            1.0,
            1.0,
            1.0,
            1.0
        };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public LinearHexaElement(int id, int[] nodeIds)
           : base(id, nodeIds)
        {
        }
        public LinearHexaElement(int id, int partId, int[] nodeIds)
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
            // The node ids are sorted 
            // S1 = 1-2-3-4 . 0-1-2-3 . 0-1-2-3
            // S2 = 5-8-7-6 . 4-7-6-5 . 4-5-6-7
            // S3 = 1-5-6-2 . 0-4-5-1 . 0-1-4-5
            // S4 = 2-6-7-3 . 1-5-6-2 . 1-2-5-6
            // S5 = 3-7-8-4 . 2-6-7-3 . 2-3-6-7
            // S6 = 4-8-5-1 . 3-7-4-0 . 0-3-4-7

            if (nodeIds[2] == 2) return FeFaceName.S1;
            else if (nodeIds[0] == 4) return FeFaceName.S2;
            else if (nodeIds[1] == 1) return FeFaceName.S3;
            else if (nodeIds[0] == 1) return FeFaceName.S4;
            else if (nodeIds[0] == 2) return FeFaceName.S5;
            else if (nodeIds[0] == 0) return FeFaceName.S6;
            else throw new NotSupportedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // S1 = 1-2-3-4 . 0-1-2-3
            // S2 = 5-8-7-6 . 4-7-6-5
            // S3 = 1-5-6-2 . 0-4-5-1
            // S4 = 2-6-7-3 . 1-5-6-2
            // S5 = 3-7-8-4 . 2-6-7-3
            // S6 = 4-8-5-1 . 3-7-4-0
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[4], NodeIds[7], NodeIds[6], NodeIds[5] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[4], NodeIds[5], NodeIds[1] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[5], NodeIds[6], NodeIds[2] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[6], NodeIds[7], NodeIds[3] };
                case FeFaceName.S6:
                    return new int[] { NodeIds[3], NodeIds[7], NodeIds[4], NodeIds[0] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // Invert the surface normal to point outwards
            // S1 = 1-2-3-4 . 0-1-2-3 . 0-3-2-1
            // S2 = 5-8-7-6 . 4-7-6-5 . 4-5-6-7
            // S3 = 1-5-6-2 . 0-4-5-1 . 0-1-5-4
            // S4 = 2-6-7-3 . 1-5-6-2 . 1-2-6-5
            // S5 = 3-7-8-4 . 2-6-7-3 . 2-3-7-6
            // S6 = 4-8-5-1 . 3-7-4-0 . 3-0-4-7
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[5], NodeIds[4] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[6], NodeIds[5] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[3], NodeIds[7], NodeIds[6] };
                case FeFaceName.S6:
                    return new int[] { NodeIds[3], NodeIds[0], NodeIds[4], NodeIds[7] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetVtkCellIdFromCell(int[] cell)
        {
            if (cell[0] == NodeIds[0] && cell[1] == NodeIds[3] && cell[2] == NodeIds[2] && cell[3] == NodeIds[1]) return 0;
            else if (cell[0] == NodeIds[4] && cell[1] == NodeIds[5] && cell[2] == NodeIds[6] && cell[3] == NodeIds[7]) return 1;
            else if (cell[0] == NodeIds[0] && cell[1] == NodeIds[1] && cell[2] == NodeIds[5] && cell[3] == NodeIds[4]) return 2;
            else if (cell[0] == NodeIds[1] && cell[1] == NodeIds[2] && cell[2] == NodeIds[6] && cell[3] == NodeIds[5]) return 3;
            else if (cell[0] == NodeIds[2] && cell[1] == NodeIds[3] && cell[2] == NodeIds[7] && cell[3] == NodeIds[6]) return 4;
            else if (cell[0] == NodeIds[3] && cell[1] == NodeIds[0] && cell[2] == NodeIds[4] && cell[3] == NodeIds[7]) return 5;
            return -1;
        }
        public override int[][] GetAllVtkCells()
        {
            // use Method: GetVtkCellFromFaceName(FeFaceName faceName)
            int[][] cells = new int[6][];

            cells[0] = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1] };
            cells[1] = new int[] { NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7] };
            cells[2] = new int[] { NodeIds[0], NodeIds[1], NodeIds[5], NodeIds[4] };
            cells[3] = new int[] { NodeIds[1], NodeIds[2], NodeIds[6], NodeIds[5] };
            cells[4] = new int[] { NodeIds[2], NodeIds[3], NodeIds[7], NodeIds[6] };
            cells[5] = new int[] { NodeIds[3], NodeIds[0], NodeIds[4], NodeIds[7] };

            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            int significantNodes = 8;
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
                // If five or more nodes were missed: break
                if (i + 1 - count >= 5) break;
            }
            // S1 = 1-2-3-4 . 0-1-2-3
            // S2 = 5-8-7-6 . 4-7-6-5
            // S3 = 1-5-6-2 . 0-4-5-1
            // S4 = 2-6-7-3 . 1-5-6-2
            // S5 = 3-7-8-4 . 2-6-7-3
            // S6 = 4-8-5-1 . 3-7-4-0
            Dictionary<FeFaceName, double> faces = new Dictionary<FeFaceName, double>();
            //
            if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[2] && faceNodeIds[3]) faces.Add(FeFaceName.S1, GetArea(FeFaceName.S1, nodes));
            if (faceNodeIds[4] && faceNodeIds[7] && faceNodeIds[6] && faceNodeIds[5]) faces.Add(FeFaceName.S2, GetArea(FeFaceName.S2, nodes));
            if (faceNodeIds[0] && faceNodeIds[4] && faceNodeIds[5] && faceNodeIds[1]) faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
            if (faceNodeIds[1] && faceNodeIds[5] && faceNodeIds[6] && faceNodeIds[2]) faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
            if (faceNodeIds[2] && faceNodeIds[6] && faceNodeIds[7] && faceNodeIds[3]) faces.Add(FeFaceName.S5, GetArea(FeFaceName.S5, nodes));
            if (faceNodeIds[3] && faceNodeIds[7] && faceNodeIds[4] && faceNodeIds[0]) faces.Add(FeFaceName.S6, GetArea(FeFaceName.S6, nodes));
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            return new double[] { _b, _b, _b, _b };
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            return GetEquivalentForces(typeof(LinearQuadrilateralElement), nodalValues);
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            return GeometryTools.RectangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]]);
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            double[] cg = GeometryTools.QuadrilateralCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]], out area);
            return cg;
        }
        public override double GetVolume(Dictionary<int, FeNode> nodes)
        {
            return GeometryTools.HexahedronVolume(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]], nodes[NodeIds[3]],
                                                  nodes[NodeIds[4]], nodes[NodeIds[5]], nodes[NodeIds[6]], nodes[NodeIds[7]]);
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double volume)
        {
            return GeometryTools.HexahedronCG(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]], nodes[NodeIds[3]],
                                              nodes[NodeIds[4]], nodes[NodeIds[5]], nodes[NodeIds[6]], nodes[NodeIds[7]],
                                              out volume);
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
                double zeta = _zeta[gp];
                //
                double u = Interpolate(u1, u2, u3, u4, u5, u6, u7, u8, xi, eta, zeta);
                //
                double jNorm = JNorm(n1, n2, n3, n4, n5, n6, n7, n8, xi, eta, zeta);
                //
                integral += _w[gp] * u * jNorm;
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3, double u4,
                                          double u5, double u6, double u7, double u8,
                                          double g, double h, double r)
        {
            return 0.125 * (1 - g) * (1 - h) * (1 - r) * u1 +
                   0.125 * (1 + g) * (1 - h) * (1 - r) * u2 +
                   0.125 * (1 + g) * (1 + h) * (1 - r) * u3 +
                   0.125 * (1 - g) * (1 + h) * (1 - r) * u4 +
                   0.125 * (1 - g) * (1 - h) * (1 + r) * u5 +
                   0.125 * (1 + g) * (1 - h) * (1 + r) * u6 +
                   0.125 * (1 + g) * (1 + h) * (1 + r) * u7 +
                   0.125 * (1 - g) * (1 + h) * (1 + r) * u8;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                    FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                    double g, double h, double r)
        {
            // Derivatives by g
            double n1g = -0.125 * (1 - h) * (1 - r);
            double n2g = 0.125 * (1 - h) * (1 - r);
            double n3g = 0.125 * (1 + h) * (1 - r);
            double n4g = -0.125 * (1 + h) * (1 - r);
            double n5g = -0.125 * (1 - h) * (1 + r);
            double n6g = 0.125 * (1 - h) * (1 + r);
            double n7g = 0.125 * (1 + h) * (1 + r);
            double n8g = -0.125 * (1 + h) * (1 + r);
            // Derivatives by h
            double n1h = -0.125 * (1 - g) * (1 - r);
            double n2h = -0.125 * (1 + g) * (1 - r);
            double n3h = 0.125 * (1 + g) * (1 - r);
            double n4h = 0.125 * (1 - g) * (1 - r);
            double n5h = -0.125 * (1 - g) * (1 + r);
            double n6h = -0.125 * (1 + g) * (1 + r);
            double n7h = 0.125 * (1 + g) * (1 + r);
            double n8h = 0.125 * (1 - g) * (1 + r);
            // Derivatives by r
            double n1r = -0.125 * (1 - g) * (1 - h);
            double n2r = -0.125 * (1 + g) * (1 - h);
            double n3r = -0.125 * (1 + g) * (1 + h);
            double n4r = -0.125 * (1 - g) * (1 + h);
            double n5r = 0.125 * (1 - g) * (1 - h);
            double n6r = 0.125 * (1 + g) * (1 - h);
            double n7r = 0.125 * (1 + g) * (1 + h);
            double n8r = 0.125 * (1 - g) * (1 + h);
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
            // dx/dr
            double xr = n1r * n1.X + n2r * n2.X + n3r * n3.X + n4r * n4.X +
                        n5r * n5.X + n6r * n6.X + n7r * n7.X + n8r * n8.X;
            double yr = n1r * n1.Y + n2r * n2.Y + n3r * n3.Y + n4r * n4.Y +
                        n5r * n5.Y + n6r * n6.Y + n7r * n7.Y + n8r * n8.Y;
            double zr = n1r * n1.Z + n2r * n2.Z + n3r * n3.Z + n4r * n4.Z +
                        n5r * n5.Z + n6r * n6.Z + n7r * n7.Z + n8r * n8.Z;
            // det(dx/dg, dx/dh, dx/dr)
            return Math.Abs(xg * (yh * zr - zh * yr) -
                            xh * (yg * zr - zg * yr) +
                            xr * (yg * zh - zg * yh));
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3-4-5-6-7 -> 0-3-2-1-4-7-6-5
            NodeIds = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                  NodeIds[4], NodeIds[7], NodeIds[6], NodeIds[5] };
        }
        //
        public override FeElement DeepCopy()
        {
            return new LinearHexaElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
