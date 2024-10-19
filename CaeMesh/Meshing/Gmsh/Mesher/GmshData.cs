using CaeGlobals;
using CaeMesh.Meshing;
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
        public string MeshFileName;
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
        // Element quality
        public string ElementQualityMetric;
        public Dictionary<int, double> ElementQuality;
        // Defeature
        public int[] SurfaceIds;
        // Parameterization
        public double[][][] Coor;
        //
        public double StlFeatureAngleDeg;


        // Constructors                                                                                                             
        public GmshData()
        {
        }
        //
        public void WriteToFile(string fileName)
        {
            this.DumpToFile(fileName);
        }
    }
}
