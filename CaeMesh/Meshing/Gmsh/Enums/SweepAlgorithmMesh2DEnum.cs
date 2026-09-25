// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using DynamicTypeDescriptor;

namespace CaeMesh
{
    /// <summary>
    /// The 2D meshing algorithms of a sweep: the gmsh ones, with their gmsh numbers, and QuadWild, which meshes the
    /// source surfaces while gmsh meshes the rest.
    /// </summary>
    [Serializable]
    public enum SweepAlgorithmMesh2DEnum
    {
        [StandardValue("MeshAdapt", DisplayName = "MeshAdapt")]
        MeshAdapt = GmshAlgorithmMesh2DEnum.MeshAdapt,
        [StandardValue("Automatic", DisplayName = "Automatic")]
        Automatic = GmshAlgorithmMesh2DEnum.Automatic,
        [StandardValue("InitialMeshOnly", DisplayName = "Initial mesh only")]
        InitialMeshOnly = GmshAlgorithmMesh2DEnum.InitialMeshOnly,
        [StandardValue("Delaunay", DisplayName = "Delaunay")]
        Delaunay = GmshAlgorithmMesh2DEnum.Delaunay,
        [StandardValue("FrontalDelaunay", DisplayName = "Frontal-Delaunay")]
        FrontalDelaunay = GmshAlgorithmMesh2DEnum.FrontalDelaunay,
        [StandardValue("FrontalDelaunayQuads", DisplayName = "Frontal-Delaunay for quads")]
        FrontalDelaunayQuads = GmshAlgorithmMesh2DEnum.FrontalDelaunayQuads,
        [StandardValue("PackingOfParallelograms", DisplayName = "Packing of parallelograms")]
        PackingOfParallelograms = GmshAlgorithmMesh2DEnum.PackingOfParallelograms,
        [StandardValue("QuasiStructuredQuad", DisplayName = "Quasi-structured quad")]
        QuasiStructuredQuad = GmshAlgorithmMesh2DEnum.QuasiStructuredQuad,
        [StandardValue("QuadWild", DisplayName = "QuadWild")]
        QuadWild = 100
    }
}
