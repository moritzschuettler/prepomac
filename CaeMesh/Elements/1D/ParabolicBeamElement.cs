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
    public class ParabolicBeamElement : FeElement1D
    {
        // Variables
        private static int vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_EDGE;
        // Line integration - 2 point Gauss on [-1, 1]
        private static readonly double _gp = 1.0 / Math.Sqrt(3.0);
        private static readonly double[] _xi = new double[] { -_gp, _gp };
        private static readonly double[] _w = new double[] { 1.0, 1.0 };


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ParabolicBeamElement(int id, int[] nodeIds)
           : base(id, nodeIds)
        {
        }
        public ParabolicBeamElement(int id, int partId, int[] nodeIds)
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
            return GeometryTools.BeamCG(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]], out length);
        }
        // Integration
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues)
        {
            // Node ordering: n1, n2 - end nodes; n3 - mid node (g: n1 = -1, n2 = +1, n3 = 0)
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]], nodalValues[NodeIds[2]],
                             IntegrationScale.None);
        }
        public override double Integrate(Dictionary<int, FeNode> nodes, Dictionary<int, double> nodalValues,
                                         IntegrationScale scale)
        {
            return Integrate(nodes[NodeIds[0]], nodes[NodeIds[1]], nodes[NodeIds[2]],
                             nodalValues[NodeIds[0]], nodalValues[NodeIds[1]], nodalValues[NodeIds[2]], scale);
        }
        private static double Integrate(FeNode n1, FeNode n2, FeNode n3, double u1, double u2, double u3,
                                        IntegrationScale scale)
        {
            double integral = 0;
            //
            for (int gp = 0; gp < _w.Length; gp++)
            {
                double g = _xi[gp];
                //
                double u = Interpolate(u1, u2, u3, g);
                //
                double jNorm = GeometryTools.BeamJNorm(n1, n2, n3, g);
                //
                double weight = scale.IsUnity ? 1.0 : scale.GetWeight(Interpolate(n1.X, n2.X, n3.X, g));
                //
                integral += _w[gp] * u * jNorm * weight;
            }
            //
            return integral;
        }
        private static double Interpolate(double u1, double u2, double u3, double g)
        {
            return 0.5 * g * (g - 1) * u1 +
                   0.5 * g * (g + 1) * u2 +
                   (1 - g * g) * u3;
        }
        //
        public override void Mirror()
        { }
        //
        public override FeElement DeepCopy()
        {
            return new ParabolicBeamElement(Id, PartId, NodeIds.ToArray());
        }
    }
}
