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
    public class ParabolicTetraElement : FeElement3D
    {
        // Variables                                                                                                                
        private static int _vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_TETRA; 
        private static double _a = 1.0 / 3.0;
        private static double[] _xi = new double[]
        {
            0.25,
            0.7272727272727273,
            0.0909090909090909,
            0.0909090909090909,
            0.0909090909090909,
            0.315701149778202,
            0.315701149778202,
            0.0531450498448169,
            0.315701149778202,
            0.0531450498448169,
            0.0531450498448169,
            0.1884185567365411,
            0.1884185567365411,
            0.6220084679281462,
            0.1884185567365411
        };
        private static double[] _eta = new double[]
        {
            0.25,
            0.0909090909090909,
            0.7272727272727273,
            0.0909090909090909,
            0.0909090909090909,
            0.315701149778202,
            0.0531450498448169,
            0.315701149778202,
            0.0531450498448169,
            0.315701149778202,
            0.0531450498448169,
            0.1884185567365411,
            0.6220084679281462,
            0.1884185567365411,
            0.1884185567365411
        };
        private static double[] _zeta = new double[]
        {
            0.25,
            0.0909090909090909,
            0.0909090909090909,
            0.7272727272727273,
            0.0909090909090909,
            0.0531450498448169,
            0.315701149778202,
            0.315701149778202,
            0.0531450498448169,
            0.0531450498448169,
            0.315701149778202,
            0.6220084679281462,
            0.1884185567365411,
            0.1884185567365411,
            0.1884185567365411
        };
        private static double[] _w = new double[]
        {
            0.0300328053555596,
            0.0059768592737158,
            0.0059768592737158,
            0.0059768592737158,
            0.0059768592737158,
            0.0115487787842829,
            0.0115487787842829,
            0.0115487787842829,
            0.0115487787842829,
            0.0115487787842829,
            0.0115487787842829,
            0.0108584378776366,
            0.0108584378776366,
            0.0108584378776366,
            0.0108584378776366
        };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ParabolicTetraElement(int id, int[] nodeIds)
          : base(id, nodeIds)
        {
        }
        public ParabolicTetraElement(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        public override int[] GetVtkNodeIds()
        {
            // return copy
            return NodeIds.ToArray();
        }
        public override int[] GetGmshNodeIds()
        {
            int[] nodeIds = new int[NodeIds.Length];
            Array.Copy(NodeIds, nodeIds, 4);
            //
            nodeIds[4] = NodeIds[4];
            nodeIds[5] = NodeIds[5];
            nodeIds[6] = NodeIds[6];
            //
            nodeIds[7] = NodeIds[7];
            nodeIds[8] = NodeIds[9];
            nodeIds[9] = NodeIds[8];
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
            // only first three nodes are important for face determination
            // S1 = 1-2-3-4-5-6 . 0-1-2
            // S2 = 1-4-2-7-8-4 . 0-1-3
            // S3 = 2-4-3-8-9-5 . 1-2-3
            // S4 = 3-4-1-9-7-6 . 0-2-3
            
            if (nodeIds[2] == 2) return FeFaceName.S1;
            else if (nodeIds[1] == 1) return FeFaceName.S2;
            else if (nodeIds[0] == 1) return FeFaceName.S3;
            else return FeFaceName.S4;
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // S1 = 1-2-3-5-6-7  . 0-1-2-4-5-6
            // S2 = 1-4-2-8-9-5  . 0-3-1-7-8-4
            // S3 = 2-4-3-9-10-6 . 1-3-2-8-9-5
            // S4 = 3-4-1-10-8-7 . 2-3-0-9-7-6
            if (faceName == FeFaceName.S1) return new int[] { NodeIds[0], NodeIds[1], NodeIds[2],
                                                              NodeIds[4], NodeIds[5], NodeIds[6] };
            if (faceName == FeFaceName.S2) return new int[] { NodeIds[0], NodeIds[3], NodeIds[1],
                                                              NodeIds[7], NodeIds[8], NodeIds[4] };
            if (faceName == FeFaceName.S3) return new int[] { NodeIds[1], NodeIds[3], NodeIds[2],
                                                              NodeIds[8], NodeIds[9], NodeIds[5] };
            if (faceName == FeFaceName.S4) return new int[] { NodeIds[2], NodeIds[3], NodeIds[0],
                                                              NodeIds[9], NodeIds[7], NodeIds[6] };
            else throw new NotSupportedException();
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // invert the surface normal . switch the second and third index
            // S1 = 1-2-3-5-6-7  . 0-2-1-6-5-4
            // S2 = 1-4-2-8-9-5  . 0-1-3-4-8-7
            // S3 = 2-4-3-9-10-6 . 1-2-3-5-9-8
            // S4 = 3-4-1-10-8-7 . 2-0-3-6-7-9
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[6], NodeIds[5], NodeIds[4] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[3], NodeIds[4], NodeIds[8], NodeIds[7] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[3], NodeIds[5], NodeIds[9], NodeIds[8] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[2], NodeIds[0], NodeIds[3], NodeIds[6], NodeIds[7], NodeIds[9] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetVtkCellIdFromCell(int[] cell)
        {
            if (cell[0] == NodeIds[0] && cell[1] == NodeIds[2] && cell[2] == NodeIds[1] &&
                cell[3] == NodeIds[6] && cell[4] == NodeIds[5] && cell[5] == NodeIds[4]) return 0;
            else if (cell[0] == NodeIds[0] && cell[1] == NodeIds[1] && cell[2] == NodeIds[3] &&
                     cell[3] == NodeIds[4] && cell[4] == NodeIds[8] && cell[5] == NodeIds[7]) return 1;
            else if (cell[0] == NodeIds[1] && cell[1] == NodeIds[2] && cell[2] == NodeIds[3] &&
                     cell[3] == NodeIds[5] && cell[4] == NodeIds[9] && cell[5] == NodeIds[8]) return 2;
            else if (cell[0] == NodeIds[2] && cell[1] == NodeIds[0] && cell[2] == NodeIds[3] &&
                     cell[3] == NodeIds[6] && cell[4] == NodeIds[7] && cell[5] == NodeIds[9]) return 3;
            return -1;
        }
        public override int[][] GetAllVtkCells()
        {
            // use Method: GetVtkCellFromFaceName(FeFaceName faceName)
            int[][] cells = new int[4][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[6], NodeIds[5], NodeIds[4] };
            cells[1] = new int[] { NodeIds[0], NodeIds[1], NodeIds[3], NodeIds[4], NodeIds[8], NodeIds[7] };
            cells[2] = new int[] { NodeIds[1], NodeIds[2], NodeIds[3], NodeIds[5], NodeIds[9], NodeIds[8] };
            cells[3] = new int[] { NodeIds[2], NodeIds[0], NodeIds[3], NodeIds[6], NodeIds[7], NodeIds[9] };
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
                // If two or more nodes were missed: break
                if (i + 1 - count >= 2) break;
            }
            // S1 = 1-2-3 . 0-1-2
            // S2 = 1-4-2 . 0-3-1
            // S3 = 2-4-3 . 1-3-2
            // S4 = 3-4-1 . 2-3-0
            Dictionary<FeFaceName, double> faces = new Dictionary<FeFaceName, double>();
            //
            if (count >= 3)
            {
                if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[2]) faces.Add(FeFaceName.S1, GetArea(FeFaceName.S1, nodes));
                if (faceNodeIds[0] && faceNodeIds[3] && faceNodeIds[1]) faces.Add(FeFaceName.S2, GetArea(FeFaceName.S2, nodes));
                if (faceNodeIds[1] && faceNodeIds[3] && faceNodeIds[2]) faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
                if (faceNodeIds[2] && faceNodeIds[3] && faceNodeIds[0]) faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
            }
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            return new double[] { 0, 0, 0, _a, _a, _a };
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            return GetEquivalentForces(typeof(ParabolicTriangleElement), nodalValues);
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            return GeometryTools.TriangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                              nodes[cell[3]], nodes[cell[4]], nodes[cell[5]]);
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            double[] cg = GeometryTools.TriangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                                   nodes[cell[3]], nodes[cell[4]], nodes[cell[5]], out area);
            return cg;
        }
        public override double GetVolume(Dictionary<int, FeNode> nodes)
        {
            return GeometryTools.TetrahedronVolume(nodes[NodeIds[0]], nodes[NodeIds[1]],
                                                   nodes[NodeIds[2]], nodes[NodeIds[3]],
                                                   nodes[NodeIds[4]], nodes[NodeIds[5]],
                                                   nodes[NodeIds[6]], nodes[NodeIds[7]],
                                                   nodes[NodeIds[8]], nodes[NodeIds[9]]);
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double volume)
        {
            return GeometryTools.TetrahedronCG(nodes[NodeIds[0]], nodes[NodeIds[1]],
                                               nodes[NodeIds[2]], nodes[NodeIds[3]],
                                               nodes[NodeIds[4]], nodes[NodeIds[5]],
                                               nodes[NodeIds[6]], nodes[NodeIds[7]],
                                               nodes[NodeIds[8]], nodes[NodeIds[9]], out volume);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodes[NodeIds[2]], nodes[NodeIds[3]],
                             nodes[NodeIds[4]], nodes[NodeIds[5]],
                             nodes[NodeIds[6]], nodes[NodeIds[7]],
                             nodes[NodeIds[8]], nodes[NodeIds[9]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]],
                             nodalValues[NodeIds[4]], nodalValues[NodeIds[5]],
                             nodalValues[NodeIds[6]], nodalValues[NodeIds[7]],
                             nodalValues[NodeIds[8]], nodalValues[NodeIds[9]]);
        }
        private static double Integrate(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                       FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                       FeNode n9, FeNode n10,
                                       double u1, double u2, double u3, double u4,
                                       double u5, double u6, double u7, double u8,
                                       double u9, double u10)
        {
            double integral = 0;
            //
            for (int gp = 0; gp < _w.Length; gp++)
            {
                double xi = _xi[gp];
                double eta = _eta[gp];
                double zeta = _zeta[gp];
                //
                double u = Interpolate(u1, u2, u3, u4, u5, u6, u7, u8, u9, u10, xi, eta, zeta);
                //
                double jNorm = JNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, xi, eta, zeta);
                //
                integral += _w[gp] * u * jNorm;
            }
            //
            return integral;
        }

        private static double Interpolate(double u1, double u2, double u3, double u4,
                                          double u5, double u6, double u7, double u8,
                                          double u9, double u10,
                                          double g, double h, double i)
        {
            double ghi1 = 1 - g - h - i;
            //
            return (2 * ghi1 - 1) * ghi1 * u1 +
                   (2 * g - 1) * g * u2 +
                   (2 * h - 1) * h * u3 +
                   (2 * i - 1) * i * u4 +
                   4 * ghi1 * g * u5 +
                   4 * g * h * u6 +
                   4 * h * ghi1 * u7 +
                   4 * ghi1 * i * u8 +
                   4 * g * i * u9 +
                   4 * h * i * u10;
        }

        private static double JNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                    FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                    FeNode n9, FeNode n10,
                                    double g, double h, double i)
        {
            double ghi1 = 1 - g - h - i;
            // Derivatives by g
            double n1g = -4 * ghi1 + 1;
            double n2g = 4 * g - 1;
            double n3g = 0;
            double n4g = 0;
            double n5g = 4 * (ghi1 - g);
            double n6g = 4 * h;
            double n7g = -4 * h;
            double n8g = -4 * i;
            double n9g = 4 * i;
            double n10g = 0;
            // Derivatives by h
            double n1h = -4 * ghi1 + 1;
            double n2h = 0;
            double n3h = 4 * h - 1;
            double n4h = 0;
            double n5h = -4 * g;
            double n6h = 4 * g;
            double n7h = 4 * (ghi1 - h);
            double n8h = -4 * i;
            double n9h = 0;
            double n10h = 4 * i;
            // Derivatives by i
            double n1i = -4 * ghi1 + 1;
            double n2i = 0;
            double n3i = 0;
            double n4i = 4 * i - 1;
            double n5i = -4 * g;
            double n6i = 0;
            double n7i = -4 * h;
            double n8i = 4 * (ghi1 - i);
            double n9i = 4 * g;
            double n10i = 4 * h;
            // dx/dg
            double xg = n1g * n1.X + n2g * n2.X + n3g * n3.X + n4g * n4.X +
                        n5g * n5.X + n6g * n6.X + n7g * n7.X + n8g * n8.X +
                        n9g * n9.X + n10g * n10.X;
            double yg = n1g * n1.Y + n2g * n2.Y + n3g * n3.Y + n4g * n4.Y +
                        n5g * n5.Y + n6g * n6.Y + n7g * n7.Y + n8g * n8.Y +
                        n9g * n9.Y + n10g * n10.Y;
            double zg = n1g * n1.Z + n2g * n2.Z + n3g * n3.Z + n4g * n4.Z +
                        n5g * n5.Z + n6g * n6.Z + n7g * n7.Z + n8g * n8.Z +
                        n9g * n9.Z + n10g * n10.Z;
            // dx/dh
            double xh = n1h * n1.X + n2h * n2.X + n3h * n3.X + n4h * n4.X +
                        n5h * n5.X + n6h * n6.X + n7h * n7.X + n8h * n8.X +
                        n9h * n9.X + n10h * n10.X;
            double yh = n1h * n1.Y + n2h * n2.Y + n3h * n3.Y + n4h * n4.Y +
                        n5h * n5.Y + n6h * n6.Y + n7h * n7.Y + n8h * n8.Y +
                        n9h * n9.Y + n10h * n10.Y;
            double zh = n1h * n1.Z + n2h * n2.Z + n3h * n3.Z + n4h * n4.Z +
                        n5h * n5.Z + n6h * n6.Z + n7h * n7.Z + n8h * n8.Z +
                        n9h * n9.Z + n10h * n10.Z;
            // dx/di
            double xi = n1i * n1.X + n2i * n2.X + n3i * n3.X + n4i * n4.X +
                        n5i * n5.X + n6i * n6.X + n7i * n7.X + n8i * n8.X +
                        n9i * n9.X + n10i * n10.X;
            double yi = n1i * n1.Y + n2i * n2.Y + n3i * n3.Y + n4i * n4.Y +
                        n5i * n5.Y + n6i * n6.Y + n7i * n7.Y + n8i * n8.Y +
                        n9i * n9.Y + n10i * n10.Y;
            double zi = n1i * n1.Z + n2i * n2.Z + n3i * n3.Z + n4i * n4.Z +
                        n5i * n5.Z + n6i * n6.Z + n7i * n7.Z + n8i * n8.Z +
                        n9i * n9.Z + n10i * n10.Z;
            // det(dx/dg, dx/dh, dx/di)
            return Math.Abs(xg * (yh * zi - zh * yi) -
                            xh * (yg * zi - zg * yi) +
                            xi * (yg * zh - zg * yh));
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3-4-5-6-7-8-9 -> 0-2-1-3-6-5-4-7-9-8
            NodeIds = new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[3],
                                  NodeIds[6], NodeIds[5], NodeIds[4],
                                  NodeIds[7], NodeIds[9], NodeIds[8] };
        }
        //
        public override FeElement DeepCopy()
        {
            return new ParabolicTetraElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
