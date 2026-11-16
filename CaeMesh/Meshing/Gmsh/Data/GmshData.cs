// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;

namespace CaeMesh
{
    [PmxType(2097)]
    public struct GmshIdLocation
    {
        [PmxKey(0)] public int Id;
        [PmxKey(1)] public double Size;
        [PmxKey(2)] public double[] Location;
    }

    [PmxType(2098)]
    public class GmshData
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        [PmxKey(0)] public string GeometryFileName;
        [PmxKey(1)] public string MeshFileName;
        [PmxKey(2)] public string InpFileName;
        [PmxKey(3)] public MeshingParameters PartMeshingParameters;
        [PmxKey(4)] public MeshSetupItem[] GmshSetupItems;
        [PmxKey(5)] public bool Preview;
        [PmxKey(6)] public bool PreviewSweepSourceMesh;
        // Topology
        [PmxKey(7)] public Dictionary<int, string> PartIdPartName;
        [PmxKey(8)] public Dictionary<int, FeNode> VertexNodes;
        [PmxKey(9)] public Dictionary<int[], List<GmshIdLocation>> EdgeVertexNodeIdsEdgeId;
        [PmxKey(10)] public Dictionary<int[], List<GmshIdLocation>> FaceVertexNodeIdsFaceId;
        // Mesh size
        [PmxKey(11)] public Dictionary<int, double> VertexNodeIdMeshSize;
        [PmxKey(12)] public Dictionary<int, int> EdgeIdNumElements;
        // The edges whose number of elements the user set as a number; the others carry the number an element size
        // gives them, which a structured grid may change. Null where every number is the user's
        [PmxKey(13)] public HashSet<int> FixedCountEdgeIds;
        // Transfinite: the structured faces by the one definition all meshers follow - faces and edges by their gmsh
        // topology ids, corners by their vertex node ids
        [PmxKey(14)] public TransfiniteFace[] TransfiniteFaces;
        // Sweep
        [PmxKey(15)] public HashSet<int>[] EdgeIdsBySweepLayer;
        [PmxKey(16)] public string QuadWildDirectory;
        [PmxKey(17)] public string NetgenMesherFileName;
        [PmxKey(18)] public string NetgenMeshParametersFileName;
        // Normals
        [PmxKey(19)] public Dictionary<int, FeNode[]> FaceIdNodes;
        [PmxKey(20)] public Dictionary<int, List<Vec3D>> NodeIdNormals;
        // Element quality
        [PmxKey(21)] public string ElementQualityMetric;
        [PmxKey(22)] public Dictionary<int, double> ElementQuality;
        // Defeature
        [PmxKey(23)] public int[] SurfaceIds;
        // Parameterization
        [PmxKey(24)] public double[][][][] ParameterizationLinesU;
        [PmxKey(25)] public double[][][][] ParameterizationLinesV;
        //
        [PmxKey(26)] public double StlFeatureAngleDeg;
        // Submapping: the faces SubmapFaceDetector found on the shells - faces and edges by their gmsh topology ids,
        // vertices by their vertex node ids
        [PmxKey(27)] public SubmapFace[] SubmapFaces;
        // Ring mapping: the faces RingFaceDetector found, by the same ids
        [PmxKey(28)] public RingFace[] RingFaces;


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
