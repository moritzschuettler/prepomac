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
    public class LinearTetraElement : FeElement3D
    {
        // Variables                                                                                                                
        private static int _vtkCellTypeInt = (int)vtkCellType.VTK_TETRA;
        private static double _a = 1.0 / 3.0;
        private static double[] _xi = new double[] { 0.25 };
        private static double[] _eta = new double[] { 0.25 };
        private static double[] _zeta = new double[] { 0.25 };
        private static double[] _w = new double[] { 1.0 / 6.0 };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public LinearTetraElement(int id, int[] nodeIds)
           : base(id, nodeIds)
        {
        }
        public LinearTetraElement(int id, int partId, int[] nodeIds)
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
            // the node ids are sorted 
            // S1 = 1-2-3 . 0-1-2 . 0-1-2
            // S2 = 1-4-2 . 0-3-1 . 0-1-3
            // S3 = 2-4-3 . 1-3-2 . 1-2-3
            // S4 = 3-4-1 . 2-3-0 . 0-2-3

            if (nodeIds[2] == 2) return FeFaceName.S1;
            else if (nodeIds[1] == 1) return FeFaceName.S2;
            else if (nodeIds[0] == 1) return FeFaceName.S3;
            else if (nodeIds[0] == 0) return FeFaceName.S4;
            else throw new NotSupportedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            // S1 = 1-2-3 . 0-1-2
            // S2 = 1-4-2 . 0-3-1
            // S3 = 2-4-3 . 1-3-2
            // S4 = 3-4-1 . 2-3-0
            if (faceName == FeFaceName.S1) return new int[] { NodeIds[0], NodeIds[1], NodeIds[2] };
            if (faceName == FeFaceName.S2) return new int[] { NodeIds[0], NodeIds[3], NodeIds[1] };
            if (faceName == FeFaceName.S3) return new int[] { NodeIds[1], NodeIds[3], NodeIds[2] };
            if (faceName == FeFaceName.S4) return new int[] { NodeIds[2], NodeIds[3], NodeIds[0] };
            else throw new NotSupportedException();
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            // Invert the surface normal . switch the second and third index
            // S1 = 1-2-3 . 0-1-2 . 0-2-1
            // S2 = 1-4-2 . 0-3-1 . 0-1-3
            // S3 = 2-4-3 . 1-3-2 . 1-2-3
            // S4 = 3-4-1 . 2-3-0 . 2-0-3
            switch (faceName)
            {
                case FeFaceName.S1:
                    return new int[] { NodeIds[0], NodeIds[2], NodeIds[1] } ;
                case FeFaceName.S2:
                    return new int[] { NodeIds[0], NodeIds[1], NodeIds[3] };
                case FeFaceName.S3:
                    return new int[] { NodeIds[1], NodeIds[2], NodeIds[3] };
                case FeFaceName.S4:
                    return new int[] { NodeIds[2], NodeIds[0], NodeIds[3] };
                default:
                    throw new NotSupportedException();
            }
        }
        public override int GetVtkCellIdFromCell(int[] cell)
        {
            if (cell[0] == NodeIds[0] && cell[1] == NodeIds[2] && cell[2] == NodeIds[1]) return 0;
            else if (cell[0] == NodeIds[0] && cell[1] == NodeIds[1] && cell[2] == NodeIds[3]) return 1;
            else if (cell[0] == NodeIds[1] && cell[1] == NodeIds[2] && cell[2] == NodeIds[3]) return 2;
            else if (cell[0] == NodeIds[2] && cell[1] == NodeIds[0] && cell[2] == NodeIds[3]) return 3;
            return -1;
        }
        public override int[][] GetAllVtkCells()
        {
            // use Method: GetVtkCellFromFaceName(FeFaceName faceName)
            int[][] cells = new int[4][];
            //
            cells[0] = new int[] { NodeIds[0], NodeIds[2], NodeIds[1] };
            cells[1] = new int[] { NodeIds[0], NodeIds[1], NodeIds[3] };
            cells[2] = new int[] { NodeIds[1], NodeIds[2], NodeIds[3] };
            cells[3] = new int[] { NodeIds[2], NodeIds[0], NodeIds[3] };
            //
            return cells;
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
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
            return new double[] { _a, _a, _a };
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            return GetEquivalentForces(typeof(LinearTriangleElement), nodalValues);
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            return GeometryTools.TriangleArea(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]]);
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            int[] cell = GetVtkCellFromFaceName(faceName);
            double[] cg = GeometryTools.TriangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], out area);
            return cg;
        }
        public override double GetVolume(Dictionary<int, FeNode> nodes)
        {
            return GeometryTools.TetrahedronVolume(nodes[NodeIds[0]], nodes[NodeIds[1]],
                                                   nodes[NodeIds[2]], nodes[NodeIds[3]]);
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double volume)
        {
            return GeometryTools.TetrahedronCG(nodes[NodeIds[0]], nodes[NodeIds[1]],
                                               nodes[NodeIds[2]], nodes[NodeIds[3]], out volume);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodes[NodeIds[2]], nodes[NodeIds[3]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]],
                             nodalValues[NodeIds[2]], nodalValues[NodeIds[3]]);
        }
        private static double Integrate(FeNode n1, FeNode n2, FeNode n3, FeNode n4, double u1, double u2, double u3, double u4)
        {
            double integral = 0;
            //
            for (int gp = 0; gp < _w.Length; gp++)
            {
                double xi = _xi[gp];
                double eta = _eta[gp];
                double zeta = _zeta[gp];
                //
                double u = Interpolate(u1, u2, u3, u4, xi, eta, zeta);
                //
                double jNorm = JNorm(n1, n2, n3, n4);
                //
                integral += _w[gp] * u * jNorm;
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3, double u4, double g, double h, double i)
        {
            return (1 - g - h - i) * u1 +
                    g * u2 +
                    h * u3 +
                    i * u4;
        }
        private static double JNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4)
        {
            // Derivatives by g
            double xg = n2.X - n1.X;
            double yg = n2.Y - n1.Y;
            double zg = n2.Z - n1.Z;
            // Derivatives by h
            double xh = n3.X - n1.X;
            double yh = n3.Y - n1.Y;
            double zh = n3.Z - n1.Z;
            // Derivatives by i
            double xi = n4.X - n1.X;
            double yi = n4.Y - n1.Y;
            double zi = n4.Z - n1.Z;
            // det(dx/dg, dx/dh, dx/di)
            return Math.Abs(xg * (yh * zi - zh * yi) - xh * (yg * zi - zg * yi) + xi * (yg * zh - zg * yh));
        }
        //
        public override void Mirror()
        {
            // 0-1-2-3 -> 0-2-1-3
            NodeIds = new int[] { NodeIds[0], NodeIds[2], NodeIds[1], NodeIds[3] };
        }
        //
        public override FeElement DeepCopy()
        {
            return new LinearTetraElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
