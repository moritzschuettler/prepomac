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
    public class LinearBeamElement : FeElement1D
    {
        // Variables
        private static int vtkCellTypeInt = (int)vtkCellType.VTK_LINE;
        // Line integration - 2 point Gauss on [-1, 1] (exact up to cubic integrands so that
        // the future axisymmetric r-weighted integrand is captured accurately)
        private static readonly double _gp = 1.0 / Math.Sqrt(3.0);
        private static readonly double[] _xi = new double[] { -_gp, _gp };
        private static readonly double[] _w = new double[] { 1.0, 1.0 };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public LinearBeamElement(int id, int[] nodeIds)
            : base(id, nodeIds)
        {
        }
        public LinearBeamElement(int id, int partId, int[] nodeIds)
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
            return vtkCellTypeInt;
        }
        public override FeFaceName GetFaceNameFromSortedNodeIds(int[] nodeIds)
        {
            throw new NotImplementedException();
        }
        public override int[] GetNodeIdsFromFaceName(FeFaceName faceName)
        {
            throw new NotImplementedException();
        }
        public override int[] GetVtkCellFromFaceName(FeFaceName faceName)
        {
            throw new NotImplementedException();
        }
        public override int[][] GetAllVtkCells()
        {
            return new int[][] { NodeIds.ToArray() };
        }
        public override Dictionary<FeFaceName, double> GetFaceNamesAndAreasFromNodeSet(HashSet<int> nodeSet,
                                                                                       Dictionary<int, FeNode> nodes,
                                                                                       bool edgeFaces)
        {
            throw new NotImplementedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName)
        {
            throw new NotImplementedException();
        }
        public override double[] GetEquivalentForcesFromFaceName(FeFaceName faceName, double[] nodalValues)
        {
            throw new NotImplementedException();
        }
        public override double GetArea(FeFaceName faceName, Dictionary<int, FeNode> nodes)
        {
            throw new NotImplementedException();
        }
        public override double[] GetFaceCG(FeFaceName faceName, Dictionary<int, FeNode> nodes, out double area)
        {
            throw new NotImplementedException();
        }
        public override double[] GetCG(Dictionary<int, FeNode> nodes, out double length)
        {
            return GeometryTools.BeamCG(nodes[NodeIds[0]], nodes[NodeIds[1]], out length);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]], IntegrationScale.None);
        }
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues,
                                         IntegrationScale scale)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]], scale);
        }
        private static double Integrate(FeNode n1, FeNode n2, double u1, double u2, IntegrationScale scale)
        {
            double integral = 0;
            //
            for (int gp = 0; gp < _w.Length; gp++)
            {
                double g = _xi[gp];
                //
                double u = Interpolate(u1, u2, g);
                //
                double jNorm = JNorm(n1, n2);
                //
                double weight = scale.IsUnity ? 1.0 : scale.GetWeight(Interpolate(n1.X, n2.X, g));
                //
                integral += _w[gp] * u * jNorm * weight;
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double g)
        {
            return 0.5 * (1 - g) * u1 +
                   0.5 * (1 + g) * u2;
        }
        private static double JNorm(FeNode n1, FeNode n2)
        {
            // |dx/dg| for a linear beam is constant and equal to 0.5 * length
            double xg = 0.5 * (n2.X - n1.X);
            double yg = 0.5 * (n2.Y - n1.Y);
            double zg = 0.5 * (n2.Z - n1.Z);
            //
            return Math.Sqrt(xg * xg + yg * yg + zg * zg);
        }
        //
        public override void Mirror()
        { }
        //
        public override FeElement DeepCopy()
        {
            return new LinearBeamElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
