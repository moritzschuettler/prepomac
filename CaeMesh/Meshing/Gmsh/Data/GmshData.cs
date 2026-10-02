// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;

namespace CaeMesh
{
    [Serializable]
    public struct GmshIdLocation
    {
        public int Id;
        public double Size;
        public double[] Location;
    }

    [Serializable]
    public class GmshData
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        public string GeometryFileName;
        public string MeshFileName;
        public string InpFileName;
        public MeshingParameters PartMeshingParameters;
        public MeshSetupItem[] GmshSetupItems;
        public bool Preview;
        public bool PreviewSweepSourceMesh;
        // Topology
        public Dictionary<int, string> PartIdPartName;
        public Dictionary<int, FeNode> VertexNodes;
        public Dictionary<int[], List<GmshIdLocation>> EdgeVertexNodeIdsEdgeId;
        public Dictionary<int[], List<GmshIdLocation>> FaceVertexNodeIdsFaceId;
        // Mesh size
        public Dictionary<int, double> VertexNodeIdMeshSize;
        public Dictionary<int, int> EdgeIdNumElements;
        // The edges whose number of elements the user set as a number; the others carry the number an element size
        // gives them, which a structured grid may change. Null where every number is the user's
        public HashSet<int> FixedCountEdgeIds;
        // Transfinite: the structured faces by the one definition all meshers follow - faces and edges by their gmsh
        // topology ids, corners by their vertex node ids
        public TransfiniteFace[] TransfiniteFaces;
        // Sweep
        public HashSet<int>[] EdgeIdsBySweepLayer;
        public string QuadWildDirectory;
        public string NetgenMesherFileName;
        public string NetgenMeshParametersFileName;
        // Normals
        public Dictionary<int, FeNode[]> FaceIdNodes;
        public Dictionary<int, List<Vec3D>> NodeIdNormals;
        // Element quality
        public string ElementQualityMetric;
        public Dictionary<int, double> ElementQuality;
        // Defeature
        public int[] SurfaceIds;
        // Parameterization
        public double[][][][] ParameterizationLinesU;
        public double[][][][] ParameterizationLinesV;
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
