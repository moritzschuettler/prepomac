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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public class ParabolicHexaElement : FeElement3D
    {
        // Variables                                                                                                                
        private static int _vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_HEXAHEDRON;
        private static double _a = 1.0 / 3.0;
        private static double _b = -_a / 4;
        private static double[] _g = new double[] { -Math.Sqrt(3.0 / 5.0), 0.0, Math.Sqrt(3.0 / 5.0) };
        private static double[] _w = new double[] { 5.0 / 9.0, 8.0 / 9.0, 5.0 / 9.0 };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ParabolicHexaElement(int id, int[] nodeIds)
          : base(id, nodeIds)
        {
        }
        public ParabolicHexaElement(int id, int partId, int[] nodeIds)
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
            int[] nodeIds = new int[NodeIds.Length];
            Array.Copy(NodeIds, nodeIds, 8);
            //
            nodeIds[8] = NodeIds[8];
            nodeIds[9] = NodeIds[11];
            nodeIds[10] = NodeIds[16];
            nodeIds[11] = NodeIds[9];
            //
            nodeIds[12] = NodeIds[17];
            nodeIds[13] = NodeIds[10];
            nodeIds[14] = NodeIds[18];
            nodeIds[15] = NodeIds[19];
            //
            nodeIds[16] = NodeIds[12];
            nodeIds[17] = NodeIds[15];
            nodeIds[18] = NodeIds[13];
            nodeIds[19] = NodeIds[14];
            //
            return nodeIds;
        }
        public override int GetVtkCellType()
        {
            return _vtkCellTypeInt;
        }
        public override FeFaceName GetFaceNameFromSortedNodeIds(int[] nodeIds)
        {
            // the node ids are sorted 
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
            // S1 = 1-2-3-4-9-10-11-12  . 0-1-2-3-8-9-10-11
            // S2 = 5-8-7-6-16-15-14-13 . 4-7-6-5-15-14-13-12
            // S3 = 1-5-6-2-17-13-18-9  . 0-4-5-1-16-12-17-8
            // S4 = 2-6-7-3-18-14-19-10 . 1-5-6-2-17-13-18-9
            // S5 = 3-7-8-4-19-15-20-11 . 2-6-7-3-18-14-19-10
            // S6 = 4-8-5-1-20-16-17-12 . 3-7-4-0-19-15-16-11
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3],
                                       NodeIds[8], NodeIds[9], NodeIds[10], NodeIds[11] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[4], NodeIds[7], NodeIds[6], NodeIds[5],
                                       NodeIds[15], NodeIds[14], NodeIds[13], NodeIds[12] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[4], NodeIds[5], NodeIds[1],
                                       NodeIds[16], NodeIds[12], NodeIds[17], NodeIds[8] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[5], NodeIds[6], NodeIds[2],
                                       NodeIds[17], NodeIds[13], NodeIds[18], NodeIds[9] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[6], NodeIds[7], NodeIds[3],
                                       NodeIds[18], NodeIds[14], NodeIds[19], NodeIds[10] };
                case FeFaceName.S6:
                    return new int[] { NodeIds[3], NodeIds[7], NodeIds[4], NodeIds[0],
                                       NodeIds[19], NodeIds[15], NodeIds[16], NodeIds[11] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // invert the surface normal to point outwards
            // S1 = 1-2-3-4-9-10-11-12  . 0-1-2-3-8-9-10-11   . 0-3-2-1-11-10-9-8
            // S2 = 5-8-7-6-16-15-14-13 . 4-7-6-5-15-14-13-12 . 4-5-6-7-12-13-14-15
            // S3 = 1-5-6-2-17-13-18-9  . 0-4-5-1-16-12-17-8  . 0-1-5-4-8-17-12-16
            // S4 = 2-6-7-3-18-14-19-10 . 1-5-6-2-17-13-18-9  . 1-2-6-5-9-18-13-17
            // S5 = 3-7-8-4-19-15-20-11 . 2-6-7-3-18-14-19-10 . 2-3-7-6-10-19-14-18
            // S6 = 4-8-5-1-20-16-17-12 . 3-7-4-0-19-15-16-11 . 3-0-4-7-11-16-15-19
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                       NodeIds[11], NodeIds[10], NodeIds[9], NodeIds[8] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7],
                                       NodeIds[12], NodeIds[13], NodeIds[14], NodeIds[15] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[5], NodeIds[4],
                                       NodeIds[8], NodeIds[17], NodeIds[12], NodeIds[16] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[6], NodeIds[5],
                                       NodeIds[9], NodeIds[18], NodeIds[13], NodeIds[17] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[2], NodeIds[3], NodeIds[7], NodeIds[6],
                                       NodeIds[10], NodeIds[19], NodeIds[14], NodeIds[18] };
                case FeFaceName.S6:
                    return new int[] { NodeIds[3], NodeIds[0], NodeIds[4], NodeIds[7],
                                       NodeIds[11], NodeIds[16], NodeIds[15], NodeIds[19] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetVtkCellIdFromCell(int[] cell)
        {
            if (cell[0] == NodeIds[0] && cell[1] == NodeIds[3] && cell[2] == NodeIds[2] && cell[3] == NodeIds[1] &&
                cell[4] == NodeIds[11] && cell[5] == NodeIds[10] && cell[6] == NodeIds[9] && cell[7] == NodeIds[8]) return 0;
            else if (cell[0] == NodeIds[4] && cell[1] == NodeIds[5] && cell[2] == NodeIds[6] && cell[3] == NodeIds[7] &&
                     cell[4] == NodeIds[12] && cell[5] == NodeIds[13] && cell[6] == NodeIds[14] && cell[7] == NodeIds[15]) return 1;
            else if (cell[0] == NodeIds[0] && cell[1] == NodeIds[1] && cell[2] == NodeIds[5] && cell[3] == NodeIds[4] &&
                     cell[4] == NodeIds[8] && cell[5] == NodeIds[17] && cell[6] == NodeIds[12] && cell[7] == NodeIds[16]) return 2;
            else if (cell[0] == NodeIds[1] && cell[1] == NodeIds[2] && cell[2] == NodeIds[6] && cell[3] == NodeIds[5] &&
                     cell[4] == NodeIds[9] && cell[5] == NodeIds[18] && cell[6] == NodeIds[13] && cell[7] == NodeIds[17]) return 3;
            else if (cell[0] == NodeIds[2] && cell[1] == NodeIds[3] && cell[2] == NodeIds[7] && cell[3] == NodeIds[6] &&
                     cell[4] == NodeIds[10] && cell[5] == NodeIds[19] && cell[6] == NodeIds[14] && cell[7] == NodeIds[18]) return 4;
            else if (cell[0] == NodeIds[3] && cell[1] == NodeIds[0] && cell[2] == NodeIds[4] && cell[3] == NodeIds[7] &&
                     cell[4] == NodeIds[11] && cell[5] == NodeIds[16] && cell[6] == NodeIds[15] && cell[7] == NodeIds[19]) return 5;
            return -1;
        }
        public override int[][] GetAllVtkCells()
        {
            // use Method: GetVtkCellFromFaceName(FeFaceName faceName)
            int[][] cells = new int[6][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                   NodeIds[11], NodeIds[10], NodeIds[9], NodeIds[8] };
            cells[1] = new int[] { NodeIds[4], NodeIds[5], NodeIds[6], NodeIds[7],
                                   NodeIds[12], NodeIds[13], NodeIds[14], NodeIds[15] };
            cells[2] = new int[] { NodeIds[0], NodeIds[1], NodeIds[5], NodeIds[4],
                                   NodeIds[8], NodeIds[17], NodeIds[12], NodeIds[16] };
            cells[3] = new int[] { NodeIds[1], NodeIds[2], NodeIds[6], NodeIds[5],
                                   NodeIds[9], NodeIds[18], NodeIds[13], NodeIds[17] };
            cells[4] = new int[] { NodeIds[2], NodeIds[3], NodeIds[7], NodeIds[6],
                                   NodeIds[10], NodeIds[19], NodeIds[14], NodeIds[18] };
            cells[5] = new int[] { NodeIds[3], NodeIds[0], NodeIds[4], NodeIds[7],
                                   NodeIds[11], NodeIds[16], NodeIds[15], NodeIds[19] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            // Check only first 8 nodes (as in linear element)
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
            if (faceNodeIds[0] && faceNodeIds[1] && faceNodeIds[2] && faceNodeIds[3])
                faces.Add(FeFaceName.S1, GetArea(FeFaceName.S1, nodes));
            if (faceNodeIds[4] && faceNodeIds[7] && faceNodeIds[6] && faceNodeIds[5])
                faces.Add(FeFaceName.S2, GetArea(FeFaceName.S2, nodes));
            if (faceNodeIds[0] && faceNodeIds[4] && faceNodeIds[5] && faceNodeIds[1])
                faces.Add(FeFaceName.S3, GetArea(FeFaceName.S3, nodes));
            if (faceNodeIds[1] && faceNodeIds[5] && faceNodeIds[6] && faceNodeIds[2])
                faces.Add(FeFaceName.S4, GetArea(FeFaceName.S4, nodes));
            if (faceNodeIds[2] && faceNodeIds[6] && faceNodeIds[7] && faceNodeIds[3])
                faces.Add(FeFaceName.S5, GetArea(FeFaceName.S5, nodes));
            if (faceNodeIds[3] && faceNodeIds[7] && faceNodeIds[4] && faceNodeIds[0])
                faces.Add(FeFaceName.S6, GetArea(FeFaceName.S6, nodes));
            //
            return faces;
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            return new double[] { _b, _b, _b, _b, _a, _a, _a, _a };
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            return GetEquivalentForces(typeof(ParabolicQuadrilateralElement), nodalValues);
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            return GeometryTools.RectangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                               nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]]);
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            double[] cg = GeometryTools.QuadrilateralCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                                    nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]], out area);
            return cg;
        }
        public override double GetVolume(Dictionary<int, FeNode> nodes)
        {
            return GeometryTools.HexahedronVolume(nodes[NodeIds[0]], nodes[NodeIds[1]],
                                                  nodes[NodeIds[2]], nodes[NodeIds[3]],
                                                  nodes[NodeIds[4]], nodes[NodeIds[5]],
                                                  nodes[NodeIds[6]], nodes[NodeIds[7]],
                                                  nodes[NodeIds[8]], nodes[NodeIds[9]],
                                                  nodes[NodeIds[10]], nodes[NodeIds[11]],
                                                  nodes[NodeIds[12]], nodes[NodeIds[13]],
                                                  nodes[NodeIds[14]], nodes[NodeIds[15]],
                                                  nodes[NodeIds[16]], nodes[NodeIds[17]],
                                                  nodes[NodeIds[18]], nodes[NodeIds[19]]);
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double volume)
        {
            return GeometryTools.HexahedronCG(nodes[NodeIds[0]], nodes[NodeIds[1]],
                                              nodes[NodeIds[2]], nodes[NodeIds[3]],
                                              nodes[NodeIds[4]], nodes[NodeIds[5]],
                                              nodes[NodeIds[6]], nodes[NodeIds[7]],
                                              nodes[NodeIds[8]], nodes[NodeIds[9]],
                                              nodes[NodeIds[10]], nodes[NodeIds[11]],
                                              nodes[NodeIds[12]], nodes[NodeIds[13]],
                                              nodes[NodeIds[14]], nodes[NodeIds[15]],
                                              nodes[NodeIds[16]], nodes[NodeIds[17]],
                                              nodes[NodeIds[18]], nodes[NodeIds[19]], out volume);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodes[NodeIds[2]], nodes[NodeIds[3]],
                             nodes[NodeIds[4]], nodes[NodeIds[5]],
                             nodes[NodeIds[6]], nodes[NodeIds[7]],
                             nodes[NodeIds[8]], nodes[NodeIds[9]],
                             nodes[NodeIds[10]], nodes[NodeIds[11]],
                             nodes[NodeIds[12]], nodes[NodeIds[13]],
                             nodes[NodeIds[14]], nodes[NodeIds[15]],
                             nodes[NodeIds[16]], nodes[NodeIds[17]],
                             nodes[NodeIds[18]], nodes[NodeIds[19]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]],
                             nodalValues[NodeIds[4]], nodalValues[NodeIds[5]],
                             nodalValues[NodeIds[6]], nodalValues[NodeIds[7]],
                             nodalValues[NodeIds[8]], nodalValues[NodeIds[9]],
                             nodalValues[NodeIds[10]], nodalValues[NodeIds[11]],
                             nodalValues[NodeIds[12]], nodalValues[NodeIds[13]],
                             nodalValues[NodeIds[14]], nodalValues[NodeIds[15]],
                             nodalValues[NodeIds[16]], nodalValues[NodeIds[17]],
                             nodalValues[NodeIds[18]], nodalValues[NodeIds[19]]);
        }
        private static double Integrate(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                        FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                        FeNode n9, FeNode n10, FeNode n11, FeNode n12,
                                        FeNode n13, FeNode n14, FeNode n15, FeNode n16,
                                        FeNode n17, FeNode n18, FeNode n19, FeNode n20,
                                        double u1, double u2, double u3, double u4,
                                        double u5, double u6, double u7, double u8,
                                        double u9, double u10, double u11, double u12,
                                        double u13, double u14, double u15, double u16,
                                        double u17, double u18, double u19, double u20)
        {
            double integral = 0;
            //
            for (int i = 0; i < _g.Length; i++)
            {
                for (int j = 0; j < _g.Length; j++)
                {
                    for (int k = 0; k < _g.Length; k++)
                    {
                        double xi = _g[i];
                        double eta = _g[j];
                        double zeta = _g[k];
                        //
                        double u = Interpolate(u1, u2, u3, u4, u5, u6, u7, u8, u9, u10,
                                               u11, u12, u13, u14, u15, u16, u17, u18,
                                               u19, u20, xi, eta, zeta);
                        //
                        double jNorm = JNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10,
                                             n11, n12, n13, n14, n15, n16, n17, n18,
                                             n19, n20, xi, eta, zeta);
                        //
                        integral += _w[i] * _w[j] * _w[k] * u * jNorm;
                    }
                }
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3, double u4,
                                          double u5, double u6, double u7, double u8,
                                          double u9, double u10, double u11, double u12,
                                          double u13, double u14, double u15, double u16,
                                          double u17, double u18, double u19, double u20,
                                          double g, double h, double r)
        {
            return -0.125 * (1 - g) * (1 - h) * (1 - r) * (2 + g + h + r) * u1 -
                   0.125 * (1 + g) * (1 - h) * (1 - r) * (2 - g + h + r) * u2 -
                   0.125 * (1 + g) * (1 + h) * (1 - r) * (2 - g - h + r) * u3 -
                   0.125 * (1 - g) * (1 + h) * (1 - r) * (2 + g - h + r) * u4 -
                   0.125 * (1 - g) * (1 - h) * (1 + r) * (2 + g + h - r) * u5 -
                   0.125 * (1 + g) * (1 - h) * (1 + r) * (2 - g + h - r) * u6 -
                   0.125 * (1 + g) * (1 + h) * (1 + r) * (2 - g - h - r) * u7 -
                   0.125 * (1 - g) * (1 + h) * (1 + r) * (2 + g - h - r) * u8 +
                    0.25 * (1 - g) * (1 + g) * (1 - h) * (1 - r) * u9 +
                    0.25 * (1 - h) * (1 + h) * (1 + g) * (1 - r) * u10 +
                    0.25 * (1 - g) * (1 + g) * (1 + h) * (1 - r) * u11 +
                    0.25 * (1 - h) * (1 + h) * (1 - g) * (1 - r) * u12 +
                    0.25 * (1 - g) * (1 + g) * (1 - h) * (1 + r) * u13 +
                    0.25 * (1 - h) * (1 + h) * (1 + g) * (1 + r) * u14 +
                    0.25 * (1 - g) * (1 + g) * (1 + h) * (1 + r) * u15 +
                    0.25 * (1 - h) * (1 + h) * (1 - g) * (1 + r) * u16 +
                    0.25 * (1 - r) * (1 + r) * (1 - g) * (1 - h) * u17 +
                    0.25 * (1 - r) * (1 + r) * (1 + g) * (1 - h) * u18 +
                    0.25 * (1 - r) * (1 + r) * (1 + g) * (1 + h) * u19 +
                    0.25 * (1 - r) * (1 + r) * (1 - g) * (1 + h) * u20;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                    FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                    FeNode n9, FeNode n10, FeNode n11, FeNode n12,
                                    FeNode n13, FeNode n14, FeNode n15, FeNode n16,
                                    FeNode n17, FeNode n18, FeNode n19, FeNode n20,
                                    double g, double h, double r)
        {
            FeNode[] n = new FeNode[] { n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15, n16, n17, n18, n19, n20 };
            //
            double[] ng = new double[20];
            double[] nh = new double[20];
            double[] nr = new double[20];
            //
            int[] sg = new int[] { -1, 1, 1, -1, -1, 1, 1, -1 };
            int[] sh = new int[] { -1, -1, 1, 1, -1, -1, 1, 1 };
            int[] sr = new int[] { -1, -1, -1, -1, 1, 1, 1, 1 };
            //
            for (int a = 0; a < 8; a++)
            {
                double s = sg[a] * g + sh[a] * h + sr[a] * r;
                ng[a] = 0.125 * sg[a] * (1 + sh[a] * h) * (1 + sr[a] * r) * (s - 2 + 1 + sg[a] * g);
                nh[a] = 0.125 * sh[a] * (1 + sg[a] * g) * (1 + sr[a] * r) * (s - 2 + 1 + sh[a] * h);
                nr[a] = 0.125 * sr[a] * (1 + sg[a] * g) * (1 + sh[a] * h) * (s - 2 + 1 + sr[a] * r);
            }
            //
            ng[8] = -0.5 * g * (1 - h) * (1 - r);
            nh[8] = -0.25 * (1 - g * g) * (1 - r);
            nr[8] = -0.25 * (1 - g * g) * (1 - h);
            //
            ng[9] = 0.25 * (1 - h * h) * (1 - r);
            nh[9] = -0.5 * h * (1 + g) * (1 - r);
            nr[9] = -0.25 * (1 - h * h) * (1 + g);
            //
            ng[10] = -0.5 * g * (1 + h) * (1 - r);
            nh[10] = 0.25 * (1 - g * g) * (1 - r);
            nr[10] = -0.25 * (1 - g * g) * (1 + h);
            //
            ng[11] = -0.25 * (1 - h * h) * (1 - r);
            nh[11] = -0.5 * h * (1 - g) * (1 - r);
            nr[11] = -0.25 * (1 - h * h) * (1 - g);
            //
            ng[12] = -0.5 * g * (1 - h) * (1 + r);
            nh[12] = -0.25 * (1 - g * g) * (1 + r);
            nr[12] = 0.25 * (1 - g * g) * (1 - h);
            //
            ng[13] = 0.25 * (1 - h * h) * (1 + r);
            nh[13] = -0.5 * h * (1 + g) * (1 + r);
            nr[13] = 0.25 * (1 - h * h) * (1 + g);
            //
            ng[14] = -0.5 * g * (1 + h) * (1 + r);
            nh[14] = 0.25 * (1 - g * g) * (1 + r);
            nr[14] = 0.25 * (1 - g * g) * (1 + h);
            //
            ng[15] = -0.25 * (1 - h * h) * (1 + r);
            nh[15] = -0.5 * h * (1 - g) * (1 + r);
            nr[15] = 0.25 * (1 - h * h) * (1 - g);
            //
            ng[16] = -0.25 * (1 - r * r) * (1 - h);
            nh[16] = -0.25 * (1 - r * r) * (1 - g);
            nr[16] = -0.5 * r * (1 - g) * (1 - h);
            //
            ng[17] = 0.25 * (1 - r * r) * (1 - h);
            nh[17] = -0.25 * (1 - r * r) * (1 + g);
            nr[17] = -0.5 * r * (1 + g) * (1 - h);
            //
            ng[18] = 0.25 * (1 - r * r) * (1 + h);
            nh[18] = 0.25 * (1 - r * r) * (1 + g);
            nr[18] = -0.5 * r * (1 + g) * (1 + h);
            //
            ng[19] = -0.25 * (1 - r * r) * (1 + h);
            nh[19] = 0.25 * (1 - r * r) * (1 - g);
            nr[19] = -0.5 * r * (1 - g) * (1 + h);
            //
            double xg = 0, yg = 0, zg = 0;
            double xh = 0, yh = 0, zh = 0;
            double xr = 0, yr = 0, zr = 0;
            //
            for (int a = 0; a < 20; a++)
            {
                xg += ng[a] * n[a].X;
                yg += ng[a] * n[a].Y;
                zg += ng[a] * n[a].Z;
                //
                xh += nh[a] * n[a].X;
                yh += nh[a] * n[a].Y;
                zh += nh[a] * n[a].Z;
                //
                xr += nr[a] * n[a].X;
                yr += nr[a] * n[a].Y;
                zr += nr[a] * n[a].Z;
            }
            //
            return Math.Abs(xg * (yh * zr - zh * yr) -
                            xh * (yg * zr - zg * yr) +
                            xr * (yg * zh - zg * yh));
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3 - 4-5-6-7 - 8-9-10-11 - 12-13-14-15 - 16-17-18-19 ->
            // 0-3-2-1 - 4-7-6-5 - 11-10-9-8 - 15-14-13-12 - 16-19-18-17
            NodeIds = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                  NodeIds[4], NodeIds[7], NodeIds[6], NodeIds[5],
                                  NodeIds[11], NodeIds[10], NodeIds[9], NodeIds[8],
                                  NodeIds[15], NodeIds[14], NodeIds[13], NodeIds[12],
                                  NodeIds[16], NodeIds[19], NodeIds[18], NodeIds[17] };
        }
        //
        public override FeElement DeepCopy()
        {
            return new ParabolicHexaElement(Id, PartId, NodeIds.ToArray());
        }
    }
}

