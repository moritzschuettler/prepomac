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
        private static int vtkCellTypeInt = (int)vtkCellType.VTK_QUADRATIC_PYRAMID;
        private static double a = 1.0 / 3.0;
        private static double b = -a / 4;


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
            return vtkCellTypeInt;
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
                return new double[] { b, b, b, b, a, a, a, a };
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
                cg = GeometryTools.RectangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]], nodes[cell[3]],
                                               nodes[cell[4]], nodes[cell[5]], nodes[cell[6]], nodes[cell[7]], out area);
            else
                cg = GeometryTools.TriangleCG(nodes[cell[0]], nodes[cell[1]], nodes[cell[2]],
                                              nodes[cell[3]], nodes[cell[4]], nodes[cell[5]], out area);
            return cg;
        }
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

