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
    public class ParabolicPyramidElement : FeElement3D
    {
        // Variables                                                                                                                
        private static int _vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_PYRAMID;
        private static double _a = 1.0 / 3.0;
        private static double _b = -_a / 4;
        static double[] _g = new double[] { -Math.Sqrt(3.0 / 5.0), 0.0, Math.Sqrt(3.0 / 5.0) };
        static double[] _w = new double[] { 5.0 / 9.0, 8.0 / 9.0, 5.0 / 9.0 };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ParabolicPyramidElement(int id, int[] nodeIds)
          : base(id, nodeIds)
        {
        }
        public ParabolicPyramidElement(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        public override int[] GetVtkNodeIds()
        {
            // Return a copy -> ToArray
            return NodeIds.ToArray();
        }
        public override int[] GetGmshNodeIds()
        {
            int[] nodeIds = new int[NodeIds.Length];
            Array.Copy(NodeIds, nodeIds, 6);
            //
            nodeIds[6] = NodeIds[8];
            nodeIds[7] = NodeIds[9];
            nodeIds[8] = NodeIds[6];
            //
            nodeIds[9] = NodeIds[10];
            nodeIds[10] = NodeIds[7];
            nodeIds[11] = NodeIds[11];
            nodeIds[12] = NodeIds[12];
            //
            return nodeIds;
        }
        public override int GetVtkCellType()
        {
            return _vtkCellTypeInt;
        }
        public override FeFaceName GetFaceNameFromSortedNodeIds(int[] nodeIds)
        {
            // The parameter node ids is sorted 
            // Only first three/four nodes are important for face determination
            // S1 = 1-2-3-4-6-7-8-9 . 0-1-2-3-5-6-7-8 . 0-1-2-3-5-6-7-8 
            // S2 = 1-5-2-10-11-6   . 0-4-1-9-10-5    . 0-1-4-5-9-10    
            // S3 = 2-5-3-11-12-7   . 1-4-2-10-11-6   . 1-2-4-6-10-11   
            // S4 = 3-5-4-12-13-8   . 2-4-3-11-12-7   . 2-3-4-7-11-12   
            // S5 = 4-5-1-13-10-9   . 3-4-0-12-9-8    . 0-3-4-8-9-12    

            if (nodeIds[2] == 2) return FeFaceName.S1;
            else if (nodeIds[1] == 1) return FeFaceName.S2;
            else if (nodeIds[0] == 1) return FeFaceName.S3;
            else if (nodeIds[0] == 2) return FeFaceName.S4;
            else if (nodeIds[0] == 0) return FeFaceName.S5;
            else throw new NotSupportedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // S1 = 1-2-3-4-6-7-8-9 . 0-1-2-3-5-6-7-8
            // S2 = 1-5-2-10-11-6   . 0-4-1-9-10-5   
            // S3 = 2-5-3-11-12-7   . 1-4-2-10-11-6  
            // S4 = 3-5-4-12-13-8   . 2-4-3-11-12-7  
            // S5 = 4-5-1-13-10-9   . 3-4-0-12-9-8   
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[2], NodeIds[3], NodeIds[5], NodeIds[6],
                                       NodeIds[7], NodeIds[8] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[4], NodeIds[1], NodeIds[9], NodeIds[10], NodeIds[5] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[1], NodeIds[4], NodeIds[2], NodeIds[10], NodeIds[11], NodeIds[6] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[2], NodeIds[4], NodeIds[3], NodeIds[11], NodeIds[12], NodeIds[7] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[3], NodeIds[4], NodeIds[0], NodeIds[12], NodeIds[9], NodeIds[8] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // Invert the surface normal to point outwards
            // S1 = 1-2-3-4-6-7-8-9 . 0-1-2-3-5-6-7-8 . 0-3-2-1-8-7-6-5
            // S2 = 1-5-2-10-11-6   . 0-4-1-9-10-5    . 0-1-4-5-10-9   
            // S3 = 2-5-3-11-12-7   . 1-4-2-10-11-6   . 1-2-4-6-11-10  
            // S4 = 3-5-4-12-13-8   . 2-4-3-11-12-7   . 2-3-4-7-12-11  
            // S5 = 4-5-1-13-10-9   . 3-4-0-12-9-8    . 3-0-4-8-9-12   
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1], NodeIds[8], NodeIds[7],
                                       NodeIds[6], NodeIds[5] };
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[4], NodeIds[5], NodeIds[10], NodeIds[9] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[4], NodeIds[6], NodeIds[11], NodeIds[10] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[2], NodeIds[3], NodeIds[4], NodeIds[7], NodeIds[12], NodeIds[11] };
                case FeFaceName.S5:
                    return new int[] { NodeIds[3], NodeIds[0], NodeIds[4], NodeIds[8], NodeIds[9], NodeIds[12] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetVtkCellIdFromCell(int[] cell)
        {
            if (cell[0] == NodeIds[0] && cell[1] == NodeIds[3] && cell[2] == NodeIds[2] && cell[3] == NodeIds[1] &&
                cell[4] == NodeIds[8] && cell[5] == NodeIds[7] && cell[6] == NodeIds[6] && cell[7] == NodeIds[5]) return 0;
            else if (cell[0] == NodeIds[0] && cell[1] == NodeIds[1] && cell[2] == NodeIds[4] &&
                     cell[3] == NodeIds[5] && cell[4] == NodeIds[10] && cell[5] == NodeIds[9]) return 1;
            else if (cell[0] == NodeIds[1] && cell[1] == NodeIds[2] && cell[2] == NodeIds[4] &&
                     cell[3] == NodeIds[6] && cell[4] == NodeIds[11] && cell[5] == NodeIds[10]) return 2;
            else if (cell[0] == NodeIds[2] && cell[1] == NodeIds[3] && cell[2] == NodeIds[4] &&
                     cell[3] == NodeIds[7] && cell[4] == NodeIds[12] && cell[5] == NodeIds[11]) return 3;
            else if (cell[0] == NodeIds[3] && cell[1] == NodeIds[0] && cell[2] == NodeIds[4] &&
                     cell[3] == NodeIds[8] && cell[4] == NodeIds[9] && cell[5] == NodeIds[12]) return 4;
            return -1;
        }
        public override int[][] GetAllVtkCells()
        {
            // Use Method: GetVtkCellFromFaceName(FeFaceName faceName)
            int[][] cells = new int[5][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1],
                                   NodeIds[8], NodeIds[7], NodeIds[6], NodeIds[5] };
            cells[1] = new int[] { NodeIds[0], NodeIds[1], NodeIds[4], NodeIds[5], NodeIds[10], NodeIds[9] };
            cells[2] = new int[] { NodeIds[1], NodeIds[2], NodeIds[4], NodeIds[6], NodeIds[11], NodeIds[10] };
            cells[3] = new int[] { NodeIds[2], NodeIds[3], NodeIds[4], NodeIds[7], NodeIds[12], NodeIds[11] };
            cells[4] = new int[] { NodeIds[3], NodeIds[0], NodeIds[4], NodeIds[8], NodeIds[9], NodeIds[12] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            // Check only first 5 nodes (as in linear element)
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
            // S1 = 1-2-3-4-6-7-8-9 . 0-1-2-3-5-6-7-8
            // S2 = 1-5-2-10-11-6   . 0-4-1-9-10-5   
            // S3 = 2-5-3-11-12-7   . 1-4-2-10-11-6  
            // S4 = 3-5-4-12-13-8   . 2-4-3-11-12-7  
            // S5 = 4-5-1-13-10-9   . 3-4-0-12-9-8   
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
            if (faceName == FeFaceName.S1 )
                return new double[] { _b, _b, _b, _b, _a, _a, _a, _a };
            else if (faceName == FeFaceName.S2 ||faceName == FeFaceName.S3 ||
                     faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                //return new double[] { 0, 0, 0, a, a, a };
                throw new NotImplementedException();
            else throw new NotSupportedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            if (faceName == FeFaceName.S1)
                return GetEquivalentForces(typeof(ParabolicQuadrilateralElement), nodalValues);
            else if (faceName == FeFaceName.S2 ||faceName == FeFaceName.S3 ||
                     faceName == FeFaceName.S4 || faceName == FeFaceName.S5)
                //return GetEquivalentForces(typeof(ParabolicTriangleElement), nodalValues);
                throw new NotImplementedException();
            else throw new NotSupportedException();
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 8)   // faceName == FeFaceName.S1
                return GeometryTools.RectangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                                   nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]]);
            else
                return GeometryTools.TriangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                                  nodes[cell[3]], nodes[cell[4]], nodes[cell[5]]);
            
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            double[] cg;
            int[] cell = GetVtkCellFromFaceName(faceName);
            if (cell.Length == 8)   // faceName == FeFaceName.S1
                cg = GeometryTools.QuadrilateralCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                               nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]], out area);
            else
                cg = GeometryTools.TriangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                              nodes[cell[3]], nodes[cell[4]], nodes[cell[5]], out area);
            return cg;
        }
        public override double GetVolume(Dictionary<int, FeNode> nodes)
        {
            return GeometryTools.PyramidVolume(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]], nodes[NodeIds[3]],
                                               nodes[NodeIds[4]], nodes[NodeIds[5]], nodes[NodeIds[6]], nodes[NodeIds[7]],
                                               nodes[NodeIds[8]], nodes[NodeIds[9]], nodes[NodeIds[10]], nodes[NodeIds[11]],
                                               nodes[NodeIds[12]]);
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double volume)
        {
            return GeometryTools.PyramidCG(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]], nodes[NodeIds[3]],
                                           nodes[NodeIds[4]], nodes[NodeIds[5]], nodes[NodeIds[6]], nodes[NodeIds[7]],
                                           nodes[NodeIds[8]], nodes[NodeIds[9]], nodes[NodeIds[10]], nodes[NodeIds[11]],
                                           nodes[NodeIds[12]], out volume);
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
                             nodes[NodeIds[12]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]],
                             nodalValues[NodeIds[4]], nodalValues[NodeIds[5]],
                             nodalValues[NodeIds[6]], nodalValues[NodeIds[7]],
                             nodalValues[NodeIds[8]], nodalValues[NodeIds[9]],
                             nodalValues[NodeIds[10]], nodalValues[NodeIds[11]],
                             nodalValues[NodeIds[12]]);
        }
        static public double Integrate(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5,
                                       FeNode n6, FeNode n7, FeNode n8, FeNode n9,
                                       FeNode n10, FeNode n11, FeNode n12, FeNode n13,
                                       double u1, double u2, double u3, double u4, double u5,
                                       double u6, double u7, double u8, double u9,
                                       double u10, double u11, double u12, double u13)
        {
            double integral = 0;
            //
            for (int i = 0; i < _g.Length; i++)
            {
                for (int j = 0; j < _g.Length; j++)
                {
                    for (int k = 0; k < _g.Length; k++)
                    {
                        double g = _g[i];
                        double h = _g[j];
                        double r = _g[k];
                        //
                        double u = Interpolate(u1, u2, u3, u4, u5, u6, u7, u8, u9, u10, u11, u12, u13, g, h, r);
                        //
                        double jNorm = JNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, g, h, r);
                        //
                        integral += _w[i] * _w[j] * _w[k] * u * jNorm;
                    }
                }
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3, double u4, double u5,
                                          double u6, double u7, double u8, double u9,
                                          double u10, double u11, double u12, double u13,
                                          double g, double h, double r)
        {
            double g2 = g * g;
            double h2 = h * h;
            double r2 = r * r;
            //
            return
                -(1 - g) * (1 - h) * (1 - r) * (4 + 3 * g + 3 * h + 2 * g * h + 2 * r + g * r + h * r + 2 * g * h * r) / 16.0 * u1
                -
                (1 + g) * (1 - h) * (1 - r) * (4 - 3 * g + 3 * h - 2 * g * h + 2 * r - g * r + h * r - 2 * g * h * r) / 16.0 * u2
                -
                (1 + g) * (1 + h) * (1 - r) * (4 - 3 * g - 3 * h + 2 * g * h + 2 * r - g * r - h * r + 2 * g * h * r) / 16.0 * u3
                -
                (1 - g) * (1 + h) * (1 - r) * (4 + 3 * g - 3 * h - 2 * g * h + 2 * r + g * r - h * r - 2 * g * h * r) / 16.0 * u4
                +
                r * (1 + r) / 2.0 * u5
                +
                (1 - g2) * (1 - h) * (1 - r) * (2 + h + h * r) / 8.0 * u6
                +
                (1 + g) * (1 - h2) * (1 - r) * (2 - g - g * r) / 8.0 * u7
                +
                (1 - g2) * (1 + h) * (1 - r) * (2 - h - h * r) / 8.0 * u8
                +
                (1 - g) * (1 - h2) * (1 - r) * (2 + g + g * r) / 8.0 * u9
                +
                (1 - g) * (1 - h) * (1 - r2) / 4.0 * u10
                +
                (1 + g) * (1 - h) * (1 - r2) / 4.0 * u11
                +
                (1 + g) * (1 + h) * (1 - r2) / 4.0 * u12
                +
                (1 - g) * (1 + h) * (1 - r2) / 4.0 * u13;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5,
                                    FeNode n6, FeNode n7, FeNode n8, FeNode n9,
                                    FeNode n10, FeNode n11, FeNode n12, FeNode n13,
                                    double g, double h, double r)
        {
            // Derivatives by g
            double n1g = (1.0 / 16.0) * (h - 1) * (r - 1) * (4 * g * h * r + 4 * g * h + 2 * g * r + 6 * g - h * r + h + r + 1);
            double n2g = (1.0 / 16.0) * (h - 1) * (r - 1) * (4 * g * h * r + 4 * g * h + 2 * g * r + 6 * g + h * r - h - r - 1);
            double n3g = (1.0 / 16.0) * (h + 1) * (r - 1) * (4 * g * h * r + 4 * g * h - 2 * g * r - 6 * g + h * r - h + r + 1);
            double n4g = (1.0 / 16.0) * (h + 1) * (r - 1) * (4 * g * h * r + 4 * g * h - 2 * g * r - 6 * g - h * r + h - r - 1);
            double n5g = 0.0;
            double n6g = -0.25 * g * (h - 1) * (r - 1) * (h * r + h + 2);
            double n7g = -0.125 * (h - 1) * (h + 1) * (r - 1) * (2 * g * r + 2 * g + r - 1);
            double n8g = -0.25 * g * (h + 1) * (r - 1) * (h * r + h - 2);
            double n9g = -0.125 * (h - 1) * (h + 1) * (r - 1) * (2 * g * r + 2 * g - r + 1);
            double n10g = -0.25 * (h - 1) * (r - 1) * (r + 1);
            double n11g = 0.25 * (h - 1) * (r - 1) * (r + 1);
            double n12g = -0.25 * (h + 1) * (r - 1) * (r + 1);
            double n13g = 0.25 * (h + 1) * (r - 1) * (r + 1);
            // Derivatives by h
            double n1h = (1.0 / 16.0) * (g - 1) * (r - 1) * (4 * g * h * r + 4 * g * h - g * r + g + 2 * h * r + 6 * h + r + 1);
            double n2h = (1.0 / 16.0) * (g + 1) * (r - 1) * (4 * g * h * r + 4 * g * h - g * r + g - 2 * h * r - 6 * h - r - 1);
            double n3h = (1.0 / 16.0) * (g + 1) * (r - 1) * (4 * g * h * r + 4 * g * h + g * r - g - 2 * h * r - 6 * h + r + 1);
            double n4h = (1.0 / 16.0) * (g - 1) * (r - 1) * (4 * g * h * r + 4 * g * h + g * r - g + 2 * h * r + 6 * h - r - 1);
            double n5h = 0.0;
            double n6h = -0.125 * (g - 1) * (g + 1) * (r - 1) * (2 * h * r + 2 * h - r + 1);
            double n7h = -0.25 * h * (g + 1) * (r - 1) * (g * r + g - 2);
            double n8h = -0.125 * (g - 1) * (g + 1) * (r - 1) * (2 * h * r + 2 * h + r - 1);
            double n9h = -0.25 * h * (g - 1) * (r - 1) * (g * r + g + 2);
            double n10h = -0.25 * (g - 1) * (r - 1) * (r + 1);
            double n11h = 0.25 * (g + 1) * (r - 1) * (r + 1);
            double n12h = -0.25 * (g + 1) * (r - 1) * (r + 1);
            double n13h = 0.25 * (g - 1) * (r - 1) * (r + 1);
            // Derivatives by r
            double n1r = 0.125 * (g - 1) * (h - 1) * (2 * g * h * r + g * r + g + h * r + h + 2 * r + 1);
            double n2r = 0.125 * (g + 1) * (h - 1) * (2 * g * h * r + g * r + g - h * r - h - 2 * r - 1);
            double n3r = 0.125 * (g + 1) * (h + 1) * (2 * g * h * r - g * r - g - h * r - h + 2 * r + 1);
            double n4r = 0.125 * (g - 1) * (h + 1) * (2 * g * h * r - g * r - g + h * r + h - 2 * r - 1);
            double n5r = 0.5 * (2 * r + 1);
            double n6r = -0.25 * (g - 1) * (g + 1) * (h - 1) * (h * r + 1);
            double n7r = -0.25 * (g + 1) * (h - 1) * (h + 1) * (g * r - 1);
            double n8r = -0.25 * (g - 1) * (g + 1) * (h + 1) * (h * r - 1);
            double n9r = -0.25 * (g - 1) * (h - 1) * (h + 1) * (g * r + 1);
            double n10r = -0.5 * r * (g - 1) * (h - 1);
            double n11r = 0.5 * r * (g + 1) * (h - 1);
            double n12r = -0.5 * r * (g + 1) * (h + 1);
            double n13r = 0.5 * r * (g - 1) * (h + 1);
            //
            double xg = n1g * n1.X + n2g * n2.X + n3g * n3.X + n4g * n4.X + n5g * n5.X +
                        n6g * n6.X + n7g * n7.X + n8g * n8.X + n9g * n9.X +
                        n10g * n10.X + n11g * n11.X + n12g * n12.X + n13g * n13.X;
            double yg = n1g * n1.Y + n2g * n2.Y + n3g * n3.Y + n4g * n4.Y + n5g * n5.Y +
                        n6g * n6.Y + n7g * n7.Y + n8g * n8.Y + n9g * n9.Y +
                        n10g * n10.Y + n11g * n11.Y + n12g * n12.Y + n13g * n13.Y;
            double zg = n1g * n1.Z + n2g * n2.Z + n3g * n3.Z + n4g * n4.Z + n5g * n5.Z +
                        n6g * n6.Z + n7g * n7.Z + n8g * n8.Z + n9g * n9.Z +
                        n10g * n10.Z + n11g * n11.Z + n12g * n12.Z + n13g * n13.Z;
            //
            double xh = n1h * n1.X + n2h * n2.X + n3h * n3.X + n4h * n4.X + n5h * n5.X +
                        n6h * n6.X + n7h * n7.X + n8h * n8.X + n9h * n9.X +
                        n10h * n10.X + n11h * n11.X + n12h * n12.X + n13h * n13.X;
            double yh = n1h * n1.Y + n2h * n2.Y + n3h * n3.Y + n4h * n4.Y + n5h * n5.Y +
                        n6h * n6.Y + n7h * n7.Y + n8h * n8.Y + n9h * n9.Y +
                        n10h * n10.Y + n11h * n11.Y + n12h * n12.Y + n13h * n13.Y;
            double zh = n1h * n1.Z + n2h * n2.Z + n3h * n3.Z + n4h * n4.Z + n5h * n5.Z +
                        n6h * n6.Z + n7h * n7.Z + n8h * n8.Z + n9h * n9.Z +
                        n10h * n10.Z + n11h * n11.Z + n12h * n12.Z + n13h * n13.Z;
            //
            double xr = n1r * n1.X + n2r * n2.X + n3r * n3.X + n4r * n4.X + n5r * n5.X +
                        n6r * n6.X + n7r * n7.X + n8r * n8.X + n9r * n9.X +
                        n10r * n10.X + n11r * n11.X + n12r * n12.X + n13r * n13.X;
            double yr = n1r * n1.Y + n2r * n2.Y + n3r * n3.Y + n4r * n4.Y + n5r * n5.Y +
                        n6r * n6.Y + n7r * n7.Y + n8r * n8.Y + n9r * n9.Y +
                        n10r * n10.Y + n11r * n11.Y + n12r * n12.Y + n13r * n13.Y;
            double zr = n1r * n1.Z + n2r * n2.Z + n3r * n3.Z + n4r * n4.Z + n5r * n5.Z +
                        n6r * n6.Z + n7r * n7.Z + n8r * n8.Z + n9r * n9.Z +
                        n10r * n10.Z + n11r * n11.Z + n12r * n12.Z + n13r * n13.Z;
            //
            return Math.Abs(xg * (yh * zr - zh * yr) -
                            xh * (yg * zr - zg * yr) +
                            xr * (yg * zh - zg * yh));
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3-4-5-6-7-8-9-10-11-12 -> 0-3-2-1-4-8-7-6-5-9-12-11-10
            NodeIds = new int[] { NodeIds[0], NodeIds[3], NodeIds[2], NodeIds[1], NodeIds[4],
                                  NodeIds[8], NodeIds[7], NodeIds[6], NodeIds[5],
                                  NodeIds[9], NodeIds[12],NodeIds[11],NodeIds[10]};
        }
        //
        public override FeElement DeepCopy()
        {
            return new ParabolicPyramidElement(Id, PartId, NodeIds.ToArray());
        }
        //
        public ParabolicWedgeElement ConvertToWedge()
        {
            int[] nodeIds = new int[15];
            nodeIds[0] = NodeIds[0];
            nodeIds[1] = NodeIds[4];
            nodeIds[2] = NodeIds[1];
            //
            nodeIds[3] = NodeIds[3];
            nodeIds[4] = NodeIds[4];
            nodeIds[5] = NodeIds[2];
            //
            nodeIds[6] = NodeIds[9];
            nodeIds[7] = NodeIds[10];
            nodeIds[8] = NodeIds[5];
            //
            nodeIds[9] = NodeIds[12];
            nodeIds[10] = NodeIds[11];
            nodeIds[11] = NodeIds[7];
            //
            nodeIds[12] = NodeIds[8];
            nodeIds[13] = NodeIds[4];
            nodeIds[14] = NodeIds[6];
            //
            return new ParabolicWedgeElement(Id, nodeIds);
        }
        public ParabolicHexaElement ConvertToHex()
        {
            int[] nodeIds = new int[20];
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
            nodeIds[8] = NodeIds[5];
            nodeIds[9] = NodeIds[6];
            nodeIds[10] = NodeIds[7];
            nodeIds[11] = NodeIds[8];
            //
            nodeIds[12] = NodeIds[4];
            nodeIds[13] = NodeIds[4];
            nodeIds[14] = NodeIds[4];
            nodeIds[15] = NodeIds[4];
            //
            nodeIds[16] = NodeIds[9];
            nodeIds[17] = NodeIds[10];
            nodeIds[18] = NodeIds[11];
            nodeIds[19] = NodeIds[12];
            //
            return new ParabolicHexaElement(Id, nodeIds);
        }
    }
}

