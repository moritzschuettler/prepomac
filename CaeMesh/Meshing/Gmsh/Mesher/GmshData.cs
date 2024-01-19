using CaeGlobals;
using System;
using System.Collections.Generic;

namespace CaeMesh
{
    [Serializable]
    public struct GmshIdLocation
    {
        public int Id;
        public double[] Location;
    }
    
    [Serializable]
    public class GmshData
    {
        // Variables                                                                                                                
        public string GeometryFileName;
        public string InpFileName;
        public MeshingParameters PartMeshingParameters;
        public MeshSetupItem[] GmshSetupItems;
        public bool Preview;
        // Topology
        public Dictionary<int, FeNode> VertexNodes;
        public Dictionary<int[], List<GmshIdLocation>> EdgeVertexNodeIdsEdgeId;
        public Dictionary<int[], List<GmshIdLocation>> FaceVertexNodeIdsFaceId;
        // Mesh size
        public Dictionary<int, double> VertexNodeIdMeshSize;
        public Dictionary<int, int> EdgeIdNumElements;
        // Normals
        public Dictionary<int, FeNode[]> FaceIdNodes;
        public Dictionary<int, List<Vec3D>> NodeIdNormals;
        //
        public double StlFeatureAngleDeg;


        // Constructors                                                                                                             
        public GmshData(string geometryFileName)
        {
            GeometryFileName = geometryFileName;
        }
        //
        public void WriteToFile(string fileName)
        {
            this.DumpToFile(fileName);
        }
    }
}
