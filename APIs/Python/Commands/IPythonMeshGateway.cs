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
using System.Drawing;

namespace APIs.Python
{
    // Geometry and mesh operations. The model parts live here too, because they are mesh parts.
    public interface IPythonMeshGateway
    {
        // Meshing
        MeshingParameters GetDefaultMeshingParameters(string name);
        MeshingParameters GetPartDefaultMeshingParameters(string name, int[] creationIds);
        ShellMmg GetPartDefaultShellMmg(string name, int[] creationIds);
        void AddMeshSetupItem(MeshSetupItem meshSetupItem);
        void ReplaceMeshSetupItem(string oldMeshSetupItemName, MeshSetupItem meshSetupItem);
        void DuplicateMeshSetupItems(string[] meshSetupItemNames);
        void RemoveMeshSetupItems(string[] meshSetupItemNames);
        void RenameMeshSetupItem(NamedClass item, string newName);
        void CreateMeshes(string[] partNames);
        // Mesh tools
        void CreateBoundaryLayer(GeometrySelection geometrySelection, double thickness);
        void RemeshElements(RemeshingParameters remeshingParameters);
        void ThickenShellMesh(string[] partNames, double thickness, int numberOfLayers, double offset,
                              bool keepModelEdges);
        void SplitPartMeshUsingSurface(SplitPartMeshData splitPartMeshData);
        void UpdateNodalCoordinatesFromFile(string fileName);
        // Nodes and elements
        void RenumberNodes(int startId);
        void RenumberElements(int startId);
        void MergeCoincidentNodes(MergeCoincidentNodes mergeCoincidentNodes);
        void CreateElementSetFromElementQuality(string name, string elementQualityMetric, string[] partNames,
                                                bool largerThan, double limit);
        // Part element types
        PartProperties GetModelPartProperties(string partName);
        PartProperties GetGeometryPartProperties(string partName);
        // Geometry parts
        void ReplaceGeometryPartProperties(string oldPartName, PartProperties newPartProperties);
        void ScaleGeometryParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy);
        void HideGeometryParts(string[] partNames);
        void ShowGeometryParts(string[] partNames);
        void SetColorForGeometryParts(string[] partNames, Color color);
        void ResetColorForGeometryParts(string[] partNames);
        void SetTransparencyForGeometryParts(string[] partNames, byte alpha);
        void RemoveGeometryParts(string[] partNames);
        void CreateAndImportCompoundPart(string[] partNames);
        void RegenerateCompoundParts(string[] compoundPartNames);
        void SwapPartGeometries(string partName1, string partName2);
        void ConvertShellToSolid(string[] partNames);
        void ConvertSolidToShell(string[] partNames);
        void FindEdgesByAngleForGeometryParts(string[] partNames, double edgeAngle);
        void FlipFaceOrientations(GeometrySelection geometrySelection);
        void SplitFaceUsingTwoPoints(GeometrySelection surfaceSelection, GeometrySelection verticesSelection);
        void Defeature(GeometrySelection geometrySelection);
        // Stl parts
        void FlipStlPartSurfacesNormal(string[] partNames);
        void DeleteStlPartFaces(GeometrySelection geometrySelection);
        // Model parts
        void ReplaceModelPartProperties(string oldPartName, PartProperties newPartProperties);
        void HideModelParts(string[] partNames);
        void ShowModelParts(string[] partNames);
        void SetColorForModelParts(string[] partNames, Color color);
        void ResetColorForModelParts(string[] partNames);
        void SetTransparencyForModelParts(string[] partNames, byte alpha);
        void TranslateModelParts(string[] partNames, double[] translateVector, int numberOfCopies);
        void RotateModelParts(string[] partNames, double[] rotateCenter, double[] rotateAxis, double rotateAngle,
                              int numberOfCopies);
        void MirrorModelParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection, bool copy);
        void ScaleModelParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy);
        void MergeModelParts(string[] partNames);
        void RemoveModelParts(string[] partNames);
        void FindEdgesByAngleForModelParts(string[] partNames, double edgeAngle);
    }
}
