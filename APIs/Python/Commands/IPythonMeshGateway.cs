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
using System.Collections.Generic;
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
        // Returns null when the item can be honoured, and the reason it cannot otherwise. The dialog asks
        // the same question before it applies, so a script is refused where the user interface refuses
        string IsMeshSetupItemProperlyDefined(MeshSetupItem meshSetupItem);
        void AddMeshSetupItem(MeshSetupItem meshSetupItem);
        void ReplaceMeshSetupItem(string oldMeshSetupItemName, MeshSetupItem meshSetupItem);
        void DuplicateMeshSetupItems(string[] meshSetupItemNames);
        void RemoveMeshSetupItems(string[] meshSetupItemNames);
        void RenameMeshSetupItem(NamedClass item, string newName);
        // Returns the reason every named part was not meshed, and an empty reason for one that was.
        // Meshing several parts can succeed for some of them, and the errors of the rest went only to
        // the output window, where the caller that asked for them cannot read them
        Dictionary<string, string> CreateMeshes(string[] partNames);
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
        void TranslateGeometryParts(string[] partNames, double[] translateVector, int numberOfCopies);
        void RotateGeometryParts(string[] partNames, double[] rotateCenter, double[] rotateAxis, double rotateAngle,
                                 int numberOfCopies);
        void MirrorGeometryParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection, bool copy);
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
        // Returns what it did to every part it was asked about, zero included, for the same reason
        // Defeature does: the modelling kernel answers whether or not it changed anything
        Dictionary<string, int> FlipFaceOrientations(GeometrySelection geometrySelection);
        // Returns the number of faces every part of the region gained, zero included, because a tool that
        // misses a part leaves it as it was without refusing the operation
        Dictionary<string, int> SplitGeometry(GeometryFeature geometryFeature);
        void RenameSplitSubPart(string compoundPartName, string oldSubPartName, string newSubPartName);
        // Returns the sub-part the cells were put under, which the application names when no name is given
        string GroupSplitCells(string compoundPartName, string[] cellNames, string subPartName);
        // Returns the cell the cells became, or the part when they were all the cells of the compound
        string FuseSplitCells(string compoundPartName, string[] cellNames);
        // Returns the number of faces each selected part lost, zero included, because a defeaturing that
        // removed nothing reports success just as loudly as one that worked
        Dictionary<string, int> Defeature(GeometrySelection geometrySelection);
        // Returns the names of the shell parts the extraction created, because the caller cannot
        // know them: the name is derived from the part the surfaces were taken from
        string[] ExtractSurface(GeometrySelection geometrySelection, double offset);
        // Stl parts
        void FlipStlPartSurfacesNormal(string[] partNames);
        Dictionary<string, int> DeleteStlPartFaces(GeometrySelection geometrySelection);
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
