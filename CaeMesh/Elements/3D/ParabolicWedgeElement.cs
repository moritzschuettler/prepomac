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
    public class ParabolicWedgeElement : FeElement3D
    {
        // Variables                                                                                                                
        private static int _vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_WEDGE;
        private static double _a = 1.0 / 3.0;
        private static double _b = -_a / 4;
        private static double[] _xi = new double[]
        {
            0.091576213509771,
            0.816847572980459,
            0.091576213509771,
            0.445948490915965,
            0.108103018168070,
            0.445948490915965
        };
        private static double[] _eta = new double[]
        {
            0.091576213509771,
            0.091576213509771,
            0.816847572980459,
            0.445948490915965,
            0.445948490915965,
            0.108103018168070
        };
        private static double[] _zeta = new double[]
        {
            -Math.Sqrt(3.0 / 5.0),
             0.0,
             Math.Sqrt(3.0 / 5.0)
        };
        private static double[] _tw = new double[]
        {
            0.109951743655322 * 0.5,
            0.109951743655322 * 0.5,
            0.109951743655322 * 0.5,
            0.223381589678011 * 0.5,
            0.223381589678011 * 0.5,
            0.223381589678011 * 0.5
        };
        private static double[] _lw = new double[]
        {
            5.0 / 9.0,
            8.0 / 9.0,
            5.0 / 9.0
        };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ParabolicWedgeElement(int id, int[] nodeIds)
          : base(id, nodeIds)
        {
        }
        public ParabolicWedgeElement(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        public override int[] GetVtkNodeIds()
        {
            // return a copy -> ToArray
            return new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[3], NodeIds[5], NodeIds[4],
                               NodeIds[8], NodeIds[7], NodeIds[6], NodeIds[11], NodeIds[10], NodeIds[9],
                               NodeIds[12], NodeIds[14], NodeIds[13]};
        }
        public override int[] GetGmshNodeIds()
        {
            int[] nodeIds = new int[NodeIds.Length];
            Array.Copy(NodeIds, nodeIds, 6);
            //
            nodeIds[6] = NodeIds[6];
            nodeIds[7] = NodeIds[8];
            nodeIds[8] = NodeIds[12];
            //
            nodeIds[9] = NodeIds[7];
            nodeIds[10] = NodeIds[13];
            nodeIds[11] = NodeIds[14];
            //
            nodeIds[12] = NodeIds[9];
            nodeIds[13] = NodeIds[11];
            nodeIds[14] = NodeIds[10];
            //
            return nodeIds;
        }
        public override int GetVtkCellType()
        {
            return _vtkCellTypeInt;
        }
        public override FeFaceName GetFaceNameFromSortedNodeIds(int[] nodeIds)
        {
            // the parameter node ids is sorted 
            // only first three/four nodes are important for face determination
            // S1 = 1-2-3-7-8-9        . 0-1-2   . 0-1-2  
            // S2 = 4-5-6-10-11-12     . 3-4-5   . 3-4-5  
            // S3 = 1-2-5-4-7-14-10-13 . 0-1-4-3 . 0-1-3-4
            // S4 = 2-3-6-5-8-15-11-14 . 1-2-5-4 . 1-2-4-5
            // S5 = 3-1-4-6-9-13-12-15 . 2-0-3-5 . 0-2-3-5

            if (nodeIds[2] == 2) return FeFaceName.S1;
            else if (nodeIds[0] == 3) return FeFaceName.S2;
            else if (nodeIds[1] == 1) return FeFaceName.S3;
            else if (nodeIds[0] == 1) return FeFaceName.S4;
            else if (nodeIds[0] == 0) return FeFaceName.S5;
            else throw new NotSupportedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // S1 = 1-2-3-7-8-9        . 0-1-2-6-7-8       
            // S2 = 4-5-6-10-11-12     . 3-4-5-9-10-11     
            // S3 = 1-2-5-4-7-14-10-13 . 0-1-4-3-6-13-9-12 
            // S4 = 2-3-6-5-8-15-11-14 . 1-2-5-4-7-14-10-13
            // S5 = 3-1-4-6-9-13-12-15 . 2-0-3-5-8-12-11-14
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[6], NodeIds[7], NodeIds[8] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[3], NodeIds[4], NodeIds[5], NodeIds[9], NodeIds[10], NodeIds[11] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4], NodeIds[3], NodeIds[6], NodeIds[13],
                                       NodeIds[9], NodeIds[12] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[5], NodeIds[4], NodeIds[7], NodeIds[14],
                                       NodeIds[10], NodeIds[13] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[0], NodeIds[3], NodeIds[5], NodeIds[8], NodeIds[12],
                                       NodeIds[11], NodeIds[14] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // invert the surface normal to point outwards
            // S1 = 1-2-3-7-8-9        . 0-1-2-6-7-8        . 0-2-1-8-7-6                             
            // S2 = 4-5-6-10-11-12     . 3-4-5-9-10-11      . 3-4-5-9-10-11      - is already outwards
            // S3 = 1-2-5-4-7-14-10-13 . 0-1-4-3-6-13-9-12  . 0-1-4-3-6-13-9-12  - is already outwards
            // S4 = 2-3-6-5-8-15-11-14 . 1-2-5-4-7-14-10-13 . 1-2-5-4-7-14-10-13 - is already outwards
            // S5 = 3-1-4-6-9-13-12-15 . 2-0-3-5-8-12-11-14 . 2-0-3-5-8-12-11-14 - is already outwards
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[8], NodeIds[7], NodeIds[6] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[3], NodeIds[4], NodeIds[5], NodeIds[9], NodeIds[10], NodeIds[11] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4], NodeIds[3], NodeIds[6], NodeIds[13],
                                       NodeIds[9], NodeIds[12] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[5], NodeIds[4], NodeIds[7], NodeIds[14],
                                       NodeIds[10], NodeIds[13] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[0], NodeIds[3], NodeIds[5], NodeIds[8], NodeIds[12],
                                       NodeIds[11], NodeIds[14] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetVtkCellIdFromCell(int[] cell)
        {
            if (cell[0] == NodeIds[0] && cell[1] == NodeIds[2] && cell[2] == NodeIds[1] &&
                cell[3] == NodeIds[8] && cell[4] == NodeIds[7] && cell[5] == NodeIds[6]) return 0;
            else if (cell[0] == NodeIds[3] && cell[1] == NodeIds[4] && cell[2] == NodeIds[5] &&
                     cell[3] == NodeIds[9] && cell[4] == NodeIds[10] && cell[5] == NodeIds[11]) return 1;
            else if (cell[0] == NodeIds[0] && cell[1] == NodeIds[1] && cell[2] == NodeIds[4] && cell[3] == NodeIds[3] &&
                     cell[4] == NodeIds[6] && cell[5] == NodeIds[13] && cell[6] == NodeIds[9] && cell[7] == NodeIds[12]) return 2;
            else if (cell[0] == NodeIds[1] && cell[1] == NodeIds[2] && cell[2] == NodeIds[5] && cell[3] == NodeIds[4] &&
                     cell[4] == NodeIds[7] && cell[5] == NodeIds[14] && cell[6] == NodeIds[10] && cell[7] == NodeIds[13]) return 3;
            else if (cell[0] == NodeIds[2] && cell[1] == NodeIds[0] && cell[2] == NodeIds[3] && cell[3] == NodeIds[5] &&
                     cell[4] == NodeIds[8] && cell[5] == NodeIds[12] && cell[6] == NodeIds[11] && cell[7] == NodeIds[14]) return 4;
            return -1;
        }
        public override int[][] GetAllVtkCells()
        {
            // use Method: GetVtkCellFromFaceName(FeFaceName faceName)
            int[][] cells = new int[5][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[8], NodeIds[7], NodeIds[6] };
            cells[1] = new int[] { NodeIds[3], NodeIds[4], NodeIds[5], NodeIds[9], NodeIds[10], NodeIds[11] };
            cells[2] = new int[] { NodeIds[0], NodeIds[1], NodeIds[4], NodeIds[3], NodeIds[6], NodeIds[13],
                                   NodeIds[9], NodeIds[12] };
            cells[3] = new int[] { NodeIds[1], NodeIds[2], NodeIds[5], NodeIds[4], NodeIds[7], NodeIds[14],
                                   NodeIds[10], NodeIds[13] };
            cells[4] = new int[] { NodeIds[2], NodeIds[0], NodeIds[3], NodeIds[5], NodeIds[8], NodeIds[12],
                                   NodeIds[11], NodeIds[14] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            // Check only first 6 nodes (as in linear element)
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
                if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[4] && faceNodeIds[3])
                    faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
                if (faceNodeIds[1] && faceNodeIds[2] && faceNodeIds[5] && faceNodeIds[4])
                    faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
                if (faceNodeIds[2] && faceNodeIds[0] && faceNodeIds[3] && faceNodeIds[5])
                    faces.Add(FeFaceName.S5, GetArea(FeFaceName.S5, nodes));
            }
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return new double[] { 0, 0, 0, _a, _a, _a };
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                return new double[] { _b, _b, _b, _b, _a, _a, _a, _a };
            else throw new NotSupportedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            if (faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return GetEquivalentForces(typeof(ParabolicTriangleElement), nodalValues);
            else if (faceName == FeFaceName.S3 || faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                return GetEquivalentForces(typeof(ParabolicQuadrilateralElement), nodalValues);
            else throw new NotSupportedException();
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 6)   //(faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                return GeometryTools.TriangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                                  nodes[cell[3]], nodes[cell[4]], nodes[cell[5]]);
            else
                return GeometryTools.RectangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                                   nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]]);
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            double[] cg;
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 6)   //(faceName == FeFaceName.S1 || faceName == FeFaceName.S2)
                cg = GeometryTools.TriangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                              nodes[cell[3]], nodes[cell[4]], nodes[cell[5]], out area);
            else
                cg = GeometryTools.QuadrilateralCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                               nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]], out area);
            return cg;
        }
        public override double GetVolume(Dictionary<int, FeNode> nodes)
        {
            return  GeometryTools.WedgeVolume(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                                              nodes[NodeIds[3]], nodes[NodeIds[4]], nodes[NodeIds[5]], 
                                              nodes[NodeIds[6]], nodes[NodeIds[7]], nodes[NodeIds[8]],
                                              nodes[NodeIds[9]], nodes[NodeIds[10]], nodes[NodeIds[11]],
                                              nodes[NodeIds[12]], nodes[NodeIds[13]], nodes[NodeIds[14]]);
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double volume)
        {
            return GeometryTools.WedgeCG(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                                         nodes[NodeIds[3]], nodes[NodeIds[4]], nodes[NodeIds[5]],
                                         nodes[NodeIds[6]], nodes[NodeIds[7]], nodes[NodeIds[8]],
                                         nodes[NodeIds[9]], nodes[NodeIds[10]], nodes[NodeIds[11]],
                                         nodes[NodeIds[12]], nodes[NodeIds[13]], nodes[NodeIds[14]], out volume);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                             nodes[NodeIds[3]], nodes[NodeIds[4]], nodes[NodeIds[5]],
                             nodes[NodeIds[6]], nodes[NodeIds[7]], nodes[NodeIds[8]],
                             nodes[NodeIds[9]], nodes[NodeIds[10]], nodes[NodeIds[11]],
                             nodes[NodeIds[12]], nodes[NodeIds[13]], nodes[NodeIds[14]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]], nodalValues[NodeIds[2]],
                             nodalValues[NodeIds[3]], nodalValues[NodeIds[4]], nodalValues[NodeIds[5]],
                             nodalValues[NodeIds[6]], nodalValues[NodeIds[7]], nodalValues[NodeIds[8]],
                             nodalValues[NodeIds[9]], nodalValues[NodeIds[10]], nodalValues[NodeIds[11]],
                             nodalValues[NodeIds[12]], nodalValues[NodeIds[13]], nodalValues[NodeIds[14]]);
        }
        private static double Integrate(FeNode n1, FeNode n2, FeNode n3,
                                        FeNode n4, FeNode n5, FeNode n6,
                                        FeNode n7, FeNode n8, FeNode n9,
                                        FeNode n10, FeNode n11, FeNode n12,
                                        FeNode n13, FeNode n14, FeNode n15,
                                        double u1, double u2, double u3,
                                        double u4, double u5, double u6,
                                        double u7, double u8, double u9,
                                        double u10, double u11, double u12,
                                        double u13, double u14, double u15)
        {
            double integral = 0;
            //
            for (int i = 0; i < _tw.Length; i++)
            {
                for (int j = 0; j < _lw.Length; j++)
                {
                    double xi = _xi[i];
                    double eta = _eta[i];
                    double zeta = _zeta[j];
                    //
                    double u = Interpolate(u1, u2, u3, u4, u5, u6,
                                           u7, u8, u9, u10, u11, u12,
                                           u13, u14, u15, xi, eta, zeta);
                    //
                    double jNorm = JNorm(n1, n2, n3, n4, n5, n6,
                                         n7, n8, n9, n10, n11, n12,
                                         n13, n14, n15, xi, eta, zeta);
                    //
                    integral += _tw[i] * _lw[j] * u * jNorm;
                }
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3,
                                          double u4, double u5, double u6,
                                          double u7, double u8, double u9,
                                          double u10, double u11, double u12,
                                          double u13, double u14, double u15,
                                          double g, double h, double r)
        {
            double gh1 = 1 - g - h;
            //
            return 0.5 * (gh1 * (2 * gh1 - 1) * (1 - r) - gh1 * (1 - r * r)) * u1 +
                   0.5 * (g * (2 * g - 1) * (1 - r) - g * (1 - r * r)) * u2 +
                   0.5 * (h * (2 * h - 1) * (1 - r) - h * (1 - r * r)) * u3 +
                   0.5 * (gh1 * (2 * gh1 - 1) * (1 + r) - gh1 * (1 - r * r)) * u4 +
                   0.5 * (g * (2 * g - 1) * (1 + r) - g * (1 - r * r)) * u5 +
                   0.5 * (h * (2 * h - 1) * (1 + r) - h * (1 - r * r)) * u6 +
                   2 * gh1 * g * (1 - r) * u7 +
                   2 * g * h * (1 - r) * u8 +
                   2 * h * gh1 * (1 - r) * u9 +
                   2 * gh1 * g * (1 + r) * u10 +
                   2 * g * h * (1 + r) * u11 +
                   2 * h * gh1 * (1 + r) * u12 +
                   gh1 * (1 - r * r) * u13 +
                   g * (1 - r * r) * u14 +
                   h * (1 - r * r) * u15;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3,
                                    FeNode n4, FeNode n5, FeNode n6,
                                    FeNode n7, FeNode n8, FeNode n9,
                                    FeNode n10, FeNode n11, FeNode n12,
                                    FeNode n13, FeNode n14, FeNode n15,
                                    double g, double h, double r)
        {
            double gh1 = 1 - g - h;
            double rr1 = 1 - r * r;
            // Derivatives by g
            double n1g = 0.5 * ((-4 * gh1 + 1) * (1 - r) + rr1);
            double n2g = 0.5 * ((4 * g - 1) * (1 - r) - rr1);
            double n3g = 0.0;
            double n4g = 0.5 * ((-4 * gh1 + 1) * (1 + r) + rr1);
            double n5g = 0.5 * ((4 * g - 1) * (1 + r) - rr1);
            double n6g = 0.0;
            double n7g = 2 * (gh1 - g) * (1 - r);
            double n8g = 2 * h * (1 - r);
            double n9g = -2 * h * (1 - r);
            double n10g = 2 * (gh1 - g) * (1 + r);
            double n11g = 2 * h * (1 + r);
            double n12g = -2 * h * (1 + r);
            double n13g = -rr1;
            double n14g = rr1;
            double n15g = 0.0;
            // Derivatives by h
            double n1h = 0.5 * ((-4 * gh1 + 1) * (1 - r) + rr1);
            double n2h = 0.0;
            double n3h = 0.5 * ((4 * h - 1) * (1 - r) - rr1);
            double n4h = 0.5 * ((-4 * gh1 + 1) * (1 + r) + rr1);
            double n5h = 0.0;
            double n6h = 0.5 * ((4 * h - 1) * (1 + r) - rr1);
            double n7h = -2 * g * (1 - r);
            double n8h = 2 * g * (1 - r);
            double n9h = 2 * (gh1 - h) * (1 - r);
            double n10h = -2 * g * (1 + r);
            double n11h = 2 * g * (1 + r);
            double n12h = 2 * (gh1 - h) * (1 + r);
            double n13h = -rr1;
            double n14h = 0.0;
            double n15h = rr1;
            // Derivatives by r
            double n1r = 0.5 * (-gh1 * (2 * gh1 - 1) + 2 * r * gh1);
            double n2r = 0.5 * (-g * (2 * g - 1) + 2 * r * g);
            double n3r = 0.5 * (-h * (2 * h - 1) + 2 * r * h);
            double n4r = 0.5 * (gh1 * (2 * gh1 - 1) + 2 * r * gh1);
            double n5r = 0.5 * (g * (2 * g - 1) + 2 * r * g);
            double n6r = 0.5 * (h * (2 * h - 1) + 2 * r * h);
            double n7r = -2 * gh1 * g;
            double n8r = -2 * g * h;
            double n9r = -2 * h * gh1;
            double n10r = 2 * gh1 * g;
            double n11r = 2 * g * h;
            double n12r = 2 * h * gh1;
            double n13r = -2 * r * gh1;
            double n14r = -2 * r * g;
            double n15r = -2 * r * h;
            // dx/dg
            double xg = n1g * n1.X + n2g * n2.X + n3g * n3.X + n4g * n4.X + n5g * n5.X +
                        n6g * n6.X + n7g * n7.X + n8g * n8.X + n9g * n9.X + n10g * n10.X +
                        n11g * n11.X + n12g * n12.X + n13g * n13.X + n14g * n14.X + n15g * n15.X;
            double yg = n1g * n1.Y + n2g * n2.Y + n3g * n3.Y + n4g * n4.Y + n5g * n5.Y +
                        n6g * n6.Y + n7g * n7.Y + n8g * n8.Y + n9g * n9.Y + n10g * n10.Y +
                        n11g * n11.Y + n12g * n12.Y + n13g * n13.Y + n14g * n14.Y + n15g * n15.Y;
            double zg = n1g * n1.Z + n2g * n2.Z + n3g * n3.Z + n4g * n4.Z + n5g * n5.Z +
                        n6g * n6.Z + n7g * n7.Z + n8g * n8.Z + n9g * n9.Z + n10g * n10.Z +
                        n11g * n11.Z + n12g * n12.Z + n13g * n13.Z + n14g * n14.Z + n15g * n15.Z;
            // dx/dh
            double xh = n1h * n1.X + n2h * n2.X + n3h * n3.X + n4h * n4.X + n5h * n5.X +
                        n6h * n6.X + n7h * n7.X + n8h * n8.X + n9h * n9.X + n10h * n10.X +
                        n11h * n11.X + n12h * n12.X + n13h * n13.X + n14h * n14.X + n15h * n15.X;
            double yh = n1h * n1.Y + n2h * n2.Y + n3h * n3.Y + n4h * n4.Y + n5h * n5.Y +
                        n6h * n6.Y + n7h * n7.Y + n8h * n8.Y + n9h * n9.Y + n10h * n10.Y +
                        n11h * n11.Y + n12h * n12.Y + n13h * n13.Y + n14h * n14.Y + n15h * n15.Y;
            double zh = n1h * n1.Z + n2h * n2.Z + n3h * n3.Z + n4h * n4.Z + n5h * n5.Z +
                        n6h * n6.Z + n7h * n7.Z + n8h * n8.Z + n9h * n9.Z + n10h * n10.Z +
                        n11h * n11.Z + n12h * n12.Z + n13h * n13.Z + n14h * n14.Z + n15h * n15.Z;
            // dx/dr
            double xr = n1r * n1.X + n2r * n2.X + n3r * n3.X + n4r * n4.X + n5r * n5.X +
                        n6r * n6.X + n7r * n7.X + n8r * n8.X + n9r * n9.X + n10r * n10.X +
                        n11r * n11.X + n12r * n12.X + n13r * n13.X + n14r * n14.X + n15r * n15.X;
            double yr = n1r * n1.Y + n2r * n2.Y + n3r * n3.Y + n4r * n4.Y + n5r * n5.Y +
                        n6r * n6.Y + n7r * n7.Y + n8r * n8.Y + n9r * n9.Y + n10r * n10.Y +
                        n11r * n11.Y + n12r * n12.Y + n13r * n13.Y + n14r * n14.Y + n15r * n15.Y;
            double zr = n1r * n1.Z + n2r * n2.Z + n3r * n3.Z + n4r * n4.Z + n5r * n5.Z +
                        n6r * n6.Z + n7r * n7.Z + n8r * n8.Z + n9r * n9.Z + n10r * n10.Z +
                        n11r * n11.Z + n12r * n12.Z + n13r * n13.Z + n14r * n14.Z + n15r * n15.Z;
            // det(dx/dg, dx/dh, dx/dr)
            return Math.Abs(xg * (yh * zr - zh * yr) -
                            xh * (yg * zr - zg * yr) +
                            xr * (yg * zh - zg * yh));
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3-4-5-6-7-8-9-10-11-12-13-14 -> 0-2-1-3-5-4-8-7-6-11-10-9-12-14-13
            NodeIds = new int[] { NodeIds[0], NodeIds[2], NodeIds[1],
                                  NodeIds[3], NodeIds[5], NodeIds[4],
                                  NodeIds[8], NodeIds[7], NodeIds[6],
                                  NodeIds[11],NodeIds[10],NodeIds[9],
                                  NodeIds[12],NodeIds[14],NodeIds[13]};
        }
        //
        public override FeElement DeepCopy()
        {
            return new ParabolicWedgeElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
