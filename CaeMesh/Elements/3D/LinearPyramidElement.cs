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
    public class LinearPyramidElement : FeElement3D
    {
        // Variables                                                                                                                
        private static int _vtkCellTypeInt = (int)vtkCellType.VTK_PYRAMID;
        private static double _a = 1.0 / 3.0;
        private static double _b = 0.25;
        private static double[] _xi = new double[]
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
        private static double[] _eta = new double[]
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
        private static double[] _zeta = new double[]
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
        private static double[] _w = new double[]
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
        public LinearPyramidElement(int id, int[] nodeIds)
           : base(id, nodeIds)
        {
        }
        public LinearPyramidElement(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        public override int[] GetVtkNodeIds()
        {
            // return a copy
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
            // S2 = 1-5-2   . 0-4-1   . 0-1-4  
            // S3 = 2-5-3   . 1-4-2   . 1-2-4  
            // S4 = 3-5-4   . 2-4-3   . 2-3-4  
            // S5 = 4-5-1   . 3-4-0   . 0-3-4  
            if (nodeIds[2] == 2) return FeFaceName.S1;
            else if (nodeIds[1] == 1) return FeFaceName.S2;
            else if (nodeIds[1] == 2) return FeFaceName.S3;
            else if (nodeIds[0] == 2) return FeFaceName.S4;
            else if (nodeIds[0] == 0) return FeFaceName.S5;
            else throw new NotSupportedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // S1 = 1-2-3-4 . 0-1-2-3
            // S2 = 1-5-2   . 0-4-1  
            // S3 = 2-5-3   . 1-4-2  
            // S4 = 3-5-4   . 2-4-3  
            // S5 = 4-5-1   . 3-4-0  
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[4], NodeIds[1] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[1], NodeIds[4], NodeIds[2] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[2], NodeIds[4], NodeIds[3] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[3], NodeIds[4], NodeIds[0] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // Invert the surface normal to point outwards
            // S1 = 1-2-3-4 . 0-1-2-3 . 0-3-2-1
            // S2 = 1-5-2   . 0-4-1   . 0-1-4  
            // S3 = 2-5-3   . 1-4-2   . 1-2-4  
            // S4 = 3-5-4   . 2-4-3   . 2-3-4  
            // S5 = 4-5-1   . 3-4-0   . 3-0-4  
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[4] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[2], NodeIds[3], NodeIds[4] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[3], NodeIds[0], NodeIds[4] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetVtkCellIdFromCell(int[] cell)
        {
            if (cell[0] == NodeIds[0] && cell[1] == NodeIds[3] && cell[2] == NodeIds[2] && cell[3] == NodeIds[1]) return 0;
            else if (cell[0] == NodeIds[0] && cell[1] == NodeIds[1] && cell[2] == NodeIds[4]) return 1;
            else if (cell[0] == NodeIds[1] && cell[1] == NodeIds[2] && cell[2] == NodeIds[4]) return 2;
            else if (cell[0] == NodeIds[2] && cell[1] == NodeIds[3] && cell[2] == NodeIds[4]) return 3;
            else if (cell[0] == NodeIds[3] && cell[1] == NodeIds[0] && cell[2] == NodeIds[4]) return 4;
            return -1;
        }
        public override int[][] GetAllVtkCells()
        {
            // Use Method: GetVtkCellFromFaceName(FeFaceName faceName)
            int[][] cells = new int[5][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1] };
            cells[1] = new int[] { NodeIds[0], NodeIds[1], NodeIds[4] };
            cells[2] = new int[] { NodeIds[1], NodeIds[2], NodeIds[4] };
            cells[3] = new int[] { NodeIds[2], NodeIds[3], NodeIds[4] };
            cells[4] = new int[] { NodeIds[3], NodeIds[0], NodeIds[4] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            int significantNodes = 5;
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
            // S1 = 1-2-3-4 . 0-1-2-3
            // S2 = 1-5-2   . 0-4-1  
            // S3 = 2-5-3   . 1-4-2  
            // S4 = 3-5-4   . 2-4-3  
            // S5 = 4-5-1   . 3-4-0  
            Dictionary<FeFaceName, double> faces = new Dictionary<FeFaceName, double>();
            //
            if (count >= 3)
            {
                if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[2] && faceNodeIds[3])
                    faces.Add(FeFaceName.S1, GetArea(FeFaceName.S1, nodes));
                if (faceNodeIds[0] && faceNodeIds[4] && faceNodeIds[1])
                    faces.Add(FeFaceName.S2, GetArea(FeFaceName.S2, nodes));
                if (faceNodeIds[1] && faceNodeIds[4] && faceNodeIds[2])
                    faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
                if (faceNodeIds[2] && faceNodeIds[4] && faceNodeIds[3])
                    faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
                if (faceNodeIds[3] && faceNodeIds[4] && faceNodeIds[0])
                    faces.Add(FeFaceName.S5, GetArea(FeFaceName.S5, nodes));
            }
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            if (faceName == FeFaceName.S1)
                return new double[] { _b, _b, _b, _b };
            else if (faceName == FeFaceName.S2 || faceName == FeFaceName.S3 ||
                     faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                //return new double[] { a, a, a };
                throw new NotImplementedException();
            else throw new NotSupportedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            if (faceName == FeFaceName.S1)
                return GetEquivalentForces(typeof(LinearQuadrilateralElement), nodalValues);
            else if (faceName == FeFaceName.S2 || faceName == FeFaceName.S3 ||
                     faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                //return GetEquivalentForces(typeof(LinearTriangleElement), nodalValues);
                throw new NotImplementedException();
            else throw new NotSupportedException();
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 4)   // faceName == FeFaceName.S1
                return GeometryTools.RectangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]]);
            else
                return GeometryTools.TriangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]]);
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            double[] cg;
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 4)   // faceName == FeFaceName.S1
                cg = GeometryTools.QuadrilateralCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]], out area);
            else
                cg = GeometryTools.TriangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], out area);
            return cg;
        }
        public override double GetVolume(Dictionary<int, FeNode> nodes)
        {
            return GeometryTools.PyramidVolume(nodes[NodeIds[0]], nodes[NodeIds[1]],
                                               nodes[NodeIds[2]], nodes[NodeIds[3]], nodes[NodeIds[4]]);
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double volume)
        {
            return GeometryTools.PyramidCG(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                                           nodes[NodeIds[3]], nodes[NodeIds[4]], out volume);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodes[NodeIds[2]], nodes[NodeIds[3]],
                             nodes[NodeIds[4]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]],
                             nodalValues[NodeIds[4]]);
        }
        static public double Integrate(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5,
                                       double u1, double u2, double u3, double u4, double u5)
        {
            double integral = 0;
            //
            for (int gp = 0; gp < _w.Length; gp++)
            {
                double xi = _xi[gp];
                double eta = _eta[gp];
                double zeta = _zeta[gp];
                //
                double u = Interpolate(u1, u2, u3, u4, u5, xi, eta, zeta);
                //
                double jNorm = JNorm(n1, n2, n3, n4, n5, xi, eta, zeta);
                //
                integral += _w[gp] * u * jNorm;
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3, double u4, double u5,
                                          double g, double h, double r)
        {
            return 0.125 * (1 - g) * (1 - h) * (1 - r) * u1 +
                   0.125 * (1 + g) * (1 - h) * (1 - r) * u2 +
                   0.125 * (1 + g) * (1 + h) * (1 - r) * u3 +
                   0.125 * (1 - g) * (1 + h) * (1 - r) * u4 +
                   0.5 * (1 + r) * u5;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5,
                                    double g, double h, double r)
        {
            // Derivatives by g
            double n1g = -0.125 * (1 - h) * (1 - r);
            double n2g = 0.125 * (1 - h) * (1 - r);
            double n3g = 0.125 * (1 + h) * (1 - r);
            double n4g = -0.125 * (1 + h) * (1 - r);
            double n5g = 0.0;
            // Derivatives by h
            double n1h = -0.125 * (1 - g) * (1 - r);
            double n2h = -0.125 * (1 + g) * (1 - r);
            double n3h = 0.125 * (1 + g) * (1 - r);
            double n4h = 0.125 * (1 - g) * (1 - r);
            double n5h = 0.0;
            // Derivatives by r
            double n1r = -0.125 * (1 - g) * (1 - h);
            double n2r = -0.125 * (1 + g) * (1 - h);
            double n3r = -0.125 * (1 + g) * (1 + h);
            double n4r = -0.125 * (1 - g) * (1 + h);
            double n5r = 0.5;
            // dx/dg
            double xg = n1g * n1.X + n2g * n2.X + n3g * n3.X + n4g * n4.X + n5g * n5.X;
            double yg = n1g * n1.Y + n2g * n2.Y + n3g * n3.Y + n4g * n4.Y + n5g * n5.Y;
            double zg = n1g * n1.Z + n2g * n2.Z + n3g * n3.Z + n4g * n4.Z + n5g * n5.Z;
            // dx/dh
            double xh = n1h * n1.X + n2h * n2.X + n3h * n3.X + n4h * n4.X + n5h * n5.X;
            double yh = n1h * n1.Y + n2h * n2.Y + n3h * n3.Y + n4h * n4.Y + n5h * n5.Y;
            double zh = n1h * n1.Z + n2h * n2.Z + n3h * n3.Z + n4h * n4.Z + n5h * n5.Z;
            // dx/dr
            double xr = n1r * n1.X + n2r * n2.X + n3r * n3.X + n4r * n4.X + n5r * n5.X;
            double yr = n1r * n1.Y + n2r * n2.Y + n3r * n3.Y + n4r * n4.Y + n5r * n5.Y;
            double zr = n1r * n1.Z + n2r * n2.Z + n3r * n3.Z + n4r * n4.Z + n5r * n5.Z;
            // det(dx/dg, dx/dh, dx/dr)
            return Math.Abs(xg * (yh * zr - zh * yr) -
                            xh * (yg * zr - zg * yr) +
                            xr * (yg * zh - zg * yh));
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3-4 -> 0-3-2-1-4
            NodeIds = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1], NodeIds[4] };
        }
        //
        public override FeElement DeepCopy()
        {
            return new LinearPyramidElement(Id, PartId, NodeIds.ToArray());
        }
        //
        public LinearWedgeElement ConvertToWedge()
        {
            int[] nodeIds = new int[6];
            nodeIds[0] = NodeIds[0];
            nodeIds[1] = NodeIds[4];
            nodeIds[2] = NodeIds[1];
            //
            nodeIds[3] = NodeIds[3];
            nodeIds[4] = NodeIds[4];
            nodeIds[5] = NodeIds[2];
            //
            return new LinearWedgeElement(Id, nodeIds);
        }
        public LinearHexaElement ConvertToHex()
        {
            int[] nodeIds = new int[8];
            nodeIds[0] = NodeIds[0];
            nodeIds[1] = NodeIds[1];
            nodeIds[2] = NodeIds[2];
            nodeIds[3] = NodeIds[3];
            //
            nodeIds[4] = NodeIds[4];
            nodeIds[5] = NodeIds[4];
            nodeIds[6] = NodeIds[4];
            nodeIds[7] = NodeIds[4];
            //
            return new LinearHexaElement(Id, nodeIds);
        }
        /// <summary>
        /// The pyramid a wedge made by ConvertToWedge or a hexahedron made by ConvertToHex stands for, or null for any
        /// other element.
        /// </summary>
        public static LinearPyramidElement FromCollapsedElement(FeElement element)
        {
            int[] n = element.NodeIds;
            if (element is LinearHexaElement && n[4] == n[5] && n[4] == n[6] && n[4] == n[7])
                return new LinearPyramidElement(element.Id, element.PartId, new int[] { n[0], n[1], n[2], n[3], n[4] });
            else if (element is LinearWedgeElement && n[1] == n[4])
                return new LinearPyramidElement(element.Id, element.PartId, new int[] { n[0], n[2], n[5], n[3], n[1] });
            else return null;
        }
    }
}

