// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;

namespace PrePoMax
{
    internal interface IMesherRunner
    {
        MesherTypeEnum MesherType { get; }
        bool CanCreateMeshFromBrep(GeometryPart part);
        bool CanCreateMeshFromStl(GeometryPart part);
        GeneratedMeshData CreateMeshFromBrep(GeometryPart part, MeshSetupItem[] mesherSetupItems, string workDirectory);
        GeneratedMeshData CreateMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems, string workDirectory);
        bool PreviewEdgeMeshFromBrep(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                     OrderedDictionary<string, MeshSetupItem> meshSetupItems);
        bool PreviewEdgeMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                    OrderedDictionary<string, MeshSetupItem> meshSetupItems);
    }
}
