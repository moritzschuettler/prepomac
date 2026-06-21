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
    public class LinearWedgeElement : FeElement3D
    {
        // Variables                                                                                                                
        private static int _vtkCellTypeInt = (int)vtkCellType.VTK_WEDGE;
        private static double _a = 1.0 / 3.0;
        private static double _b = 0.25;
        private static double[] _xi = new double[]
        {
            1.0 / 6.0,
            2.0 / 3.0,
            1.0 / 6.0,
            1.0 / 6.0,
            2.0 / 3.0,
            1.0 / 6.0
        };
        private static double[] _eta = new double[]
        {
            1.0 / 6.0,
            1.0 / 6.0,
            2.0 / 3.0,
            1.0 / 6.0,
            1.0 / 6.0,
            2.0 / 3.0
        };
        private static double[] _zeta = new double[]
        {
            -1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
            -1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3),
             1.0 / Math.Sqrt(3)
        };
        private static double[] _w = new double[]
        {
            1.0 / 6.0,
            1.0 / 6.0,
            1.0 / 6.0,
            1.0 / 6.0,
            1.0 / 6.0,
            1.0 / 6.0
        };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public LinearWedgeElement(int id, int[] nodeIds)
           : base(id, nodeIds)
        {
            //if (nodeIds.Length != 6) throw new NotSupportedException();
        }
        public LinearWedgeElement(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
            //if (nodeIds.Length != 6) throw new NotSupportedException();
        }


        // Methods                                                                                                                  
        public override int[] GetVtkNodeIds()
        {
            // return a copy
            return new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[3], NodeIds[5], NodeIds[4] };
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
            // S1 = 1-2-3   . 0-1-2   . 0-1-2  
            // S2 = 4-5-6   . 3-4-5   . 3-4-5  
            // S3 = 1-2-5-4 . 0-1-4-3 . 0-1-3-4
            // S4 = 2-3-6-5 . 1-2-5-4 . 1-2-4-5
            // S5 = 3-1-4-6 . 2-0-3-5 . 0-2-3-5

            if (nodeIds[2] == 2) return FeFaceName.S1;
            else if (nodeIds[0] == 3) return FeFaceName.S2;
            else if (nodeIds[1] == 1) return FeFaceName.S3;
            else if (nodeIds[0] == 1) return FeFaceName.S4;
            else if (nodeIds[0] == 0) return FeFaceName.S5;
            else throw new NotSupportedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // S1 = 1-2-3   . 0-1-2  
            // S2 = 4-5-6   . 3-4-5  
            // S3 = 1-2-5-4 . 0-1-4-3
            // S4 = 2-3-6-5 . 1-2-5-4
            // S5 = 3-1-4-6 . 2-0-3-5
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[3], NodeIds[4], NodeIds[5] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4], NodeIds[3] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[5], NodeIds[4] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[0], NodeIds[3], NodeIds[5] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // Invert the surface normal to point outwards
            // S1 = 1-2-3   . 0-1-2   . 0-2-1  
            // S2 = 4-5-6   . 3-4-5   . 3-4-5   - is already outwards
            // S3 = 1-2-5-4 . 0-1-4-3 . 0-1-4-3 - is already outwards
            // S4 = 2-3-6-5 . 1-2-5-4 . 1-2-5-4 - is already outwards
            // S5 = 3-1-4-6 . 2-0-3-5 . 2-0-3-5 - is already outwards
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[2], NodeIds[1] } ;
                case FeFaceName.S2:
                    return new int[] { NodeIds[3], NodeIds[4], NodeIds[5] };
                case FeFaceName.S3:
                    //return new int[] { NodeIDs[0], NodeIDs[1], NodeIDs[4], NodeIDs[3] };
                    return new int[] { NodeIds[1], NodeIds[4], NodeIds[3], NodeIds[0] };
                case FeFaceName.S4:
                    //return new int[] { NodeIDs[1], NodeIDs[2], NodeIDs[5], NodeIDs[4] };
                    return new int[] { NodeIds[2], NodeIds[5], NodeIds[4], NodeIds[1] };
                case FeFaceName.S5:
                    //return new int[] { NodeIDs[2], NodeIDs[0], NodeIDs[3], NodeIDs[5] };
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[5], NodeIds[2] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetVtkCellIdFromCell(int[] cell)
        {
            if (cell[0] == NodeIds[0] && cell[1] == NodeIds[2] && cell[2] == NodeIds[1]) return 0;
            else if (cell[0] == NodeIds[3] && cell[1] == NodeIds[4] && cell[2] == NodeIds[5]) return 1;
            else if (cell[0] == NodeIds[1] && cell[1] == NodeIds[4] && cell[2] == NodeIds[3] && cell[3] == NodeIds[0]) return 2;
            else if (cell[0] == NodeIds[2] && cell[1] == NodeIds[5] && cell[2] == NodeIds[4] && cell[3] == NodeIds[1]) return 3;
            else if (cell[0] == NodeIds[0] && cell[1] == NodeIds[3] && cell[2] == NodeIds[5] && cell[3] == NodeIds[2]) return 4;
            return -1;
        }
        public override int[][] GetAllVtkCells()
        {
            // Use Method: GetVtkCellFromFaceName(FeFaceName faceName)
            int[][] cells = new int[5][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[2], NodeIds[1] };
            cells[1] = new int[] { NodeIds[3], NodeIds[4], NodeIds[5] };
            cells[2] = new int[] { NodeIds[1], NodeIds[4], NodeIds[3], NodeIds[0] };
            cells[3] = new int[] { NodeIds[2], NodeIds[5], NodeIds[4], NodeIds[1] };
            cells[4] = new int[] { NodeIds[0], NodeIds[3], NodeIds[5], NodeIds[2] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            int significantNodes = 6;
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
                // If four or more nodes were missed: break
                if (i + 1 - count >= 4) break;
            }
            // S1 = 1-2-3   . 0-1-2  
            // S2 = 4-5-6   . 3-4-5  
            // S3 = 1-2-5-4 . 0-1-4-3
            // S4 = 2-3-6-5 . 1-2-5-4
            // S5 = 3-1-4-6 . 2-0-3-5
            Dictionary<FeFaceName, double> faces = new Dictionary<FeFaceName, double>();
            //
            if (count >= 3)
            {
                if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[2]) faces.Add(FeFaceName.S1, GetArea(FeFaceName.S1, nodes));
                if (faceNodeIds[3] && faceNodeIds[4] && faceNodeIds[5]) faces.Add(FeFaceName.S2, GetArea(FeFaceName.S2, nodes));
                if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[4] && faceNodeIds[3]) faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
                if (faceNodeIds[1] && faceNodeIds[2] && faceNodeIds[5] && faceNodeIds[4]) faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
                if (faceNodeIds[2] && faceNodeIds[0] && faceNodeIds[3] && faceNodeIds[5]) faces.Add(FeFaceName.S5, GetArea(FeFaceName.S5, nodes));
            }
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2) 
                return new double[] { _a, _a, _a };
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                return new double[] { _b, _b, _b, _b };
            else throw new NotSupportedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return GetEquivalentForces(typeof(LinearTriangleElement), nodalValues);
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                return GetEquivalentForces(typeof(LinearQuadrilateralElement), nodalValues);
            else throw new NotSupportedException();
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 3)   //(faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return GeometryTools.TriangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]]);
            else
                return GeometryTools.RectangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]]);
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            double[] cg;
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 3)   //(faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                cg = GeometryTools.TriangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], out area);
            else
                cg = GeometryTools.QuadrilateralCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]], out area);
            return cg;
        }
        public override double GetVolume(Dictionary<int, FeNode> nodes)
        {
            return GeometryTools.WedgeVolume(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                                             nodes[NodeIds[3]], nodes[NodeIds[4]], nodes[NodeIds[5]]);
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double volume)
        {
            return GeometryTools.WedgeCG(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                                         nodes[NodeIds[3]], nodes[NodeIds[4]], nodes[NodeIds[5]], out volume);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodes[NodeIds[2]], nodes[NodeIds[3]],
                             nodes[NodeIds[4]], nodes[NodeIds[5]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]],
                             nodalValues[NodeIds[4]], nodalValues[NodeIds[5]]);
        }
        private static double Integrate(FeNode n1, FeNode n2, FeNode n3,
                                       FeNode n4, FeNode n5, FeNode n6,
                                       double u1, double u2, double u3,
                                       double u4, double u5, double u6)
        {
            double integral = 0;
            //
            for (int gp = 0; gp < _w.Length; gp++)
            {
                double xi = _xi[gp];
                double eta = _eta[gp];
                double zeta = _zeta[gp];
                //
                double u = Interpolate(u1, u2, u3, u4, u5, u6, xi, eta, zeta);
                //
                double jNorm = JNorm(n1, n2, n3, n4, n5, n6, xi, eta, zeta);
                //
                integral += _w[gp] * u * jNorm;
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3,
                                          double u4, double u5, double u6,
                                          double g, double h, double r)
        {
            double gh1 = 1 - g - h;
            //
            return 0.5 * gh1 * (1 - r) * u1 +
                   0.5 * g * (1 - r) * u2 +
                   0.5 * h * (1 - r) * u3 +
                   0.5 * gh1 * (1 + r) * u4 +
                   0.5 * g * (1 + r) * u5 +
                   0.5 * h * (1 + r) * u6;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3,
                                    FeNode n4, FeNode n5, FeNode n6,
                                    double g, double h, double r)
        {
            double gh1 = 1 - g - h;
            // Derivatives by g
            double n1g = -0.5 * (1 - r);
            double n2g = 0.5 * (1 - r);
            double n3g = 0.0;
            double n4g = -0.5 * (1 + r);
            double n5g = 0.5 * (1 + r);
            double n6g = 0.0;
            // Derivatives by h
            double n1h = -0.5 * (1 - r);
            double n2h = 0.0;
            double n3h = 0.5 * (1 - r);
            double n4h = -0.5 * (1 + r);
            double n5h = 0.0;
            double n6h = 0.5 * (1 + r);
            // Derivatives by r
            double n1r = -0.5 * gh1;
            double n2r = -0.5 * g;
            double n3r = -0.5 * h;
            double n4r = 0.5 * gh1;
            double n5r = 0.5 * g;
            double n6r = 0.5 * h;
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
            // dx/dr
            double xr = n1r * n1.X + n2r * n2.X + n3r * n3.X +
                        n4r * n4.X + n5r * n5.X + n6r * n6.X;
            double yr = n1r * n1.Y + n2r * n2.Y + n3r * n3.Y +
                        n4r * n4.Y + n5r * n5.Y + n6r * n6.Y;
            double zr = n1r * n1.Z + n2r * n2.Z + n3r * n3.Z +
                        n4r * n4.Z + n5r * n5.Z + n6r * n6.Z;
            // det(dx/dg, dx/dh, dx/dr)
            return Math.Abs(xg * (yh * zr - zh * yr) -
                            xh * (yg * zr - zg * yr) +
                            xr * (yg * zh - zg * yh));
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3-4-5 -> 0-2-1-3-5-4
            NodeIds = new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[3], NodeIds[5], NodeIds[4] };
        }
        //
        public override FeElement DeepCopy()
        {
            return new LinearWedgeElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
