// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeGlobals;
using CaeMesh;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PrePoMax.Python
{
    internal class ControllerPythonMeshGateway : IPythonMeshGateway
    {
        // Variables
        private readonly Controller _controller;


        // Constructors
        public ControllerPythonMeshGateway(Controller controller)
        {
            _controller = controller;
        }


        // Methods
        public MeshingParameters GetDefaultMeshingParameters(string name)
        {
            return _controller.GetDefaultMeshingParameters(name);
        }
        public MeshingParameters GetPartDefaultMeshingParameters(string name, int[] creationIds)
        {
            string[] partNames = GetPartNames(creationIds);
            if (partNames == null) return null;
            //
            MeshingParameters meshingParameters = _controller.GetPartDefaultMeshingParameters(partNames);
            if (meshingParameters == null) return null;
            //
            meshingParameters.Name = name;
            return meshingParameters;
        }
        public ShellMmg GetPartDefaultShellMmg(string name, int[] creationIds)
        {
            string[] partNames = GetPartNames(creationIds);
            if (partNames == null) return null;
            //
            ShellMmg shellMmg = _controller.GetPartDefaultShellMmg(partNames);
            if (shellMmg == null) return null;
            //
            shellMmg.Name = name;
            return shellMmg;
        }
        private string[] GetPartNames(int[] creationIds)
        {
            if (creationIds == null || creationIds.Length == 0) return null;
            //
            FeMesh geometry = _controller.Model?.Geometry;
            if (geometry == null) return null;
            // A script may name the parts or the geometry they cover, so both forms of id are resolved
            string[] partNames = geometry.GetPartNamesFromPartIds(creationIds);
            if (partNames.Length == 0)
                partNames = geometry.GetPartNamesFromPartIds(FeMesh.GetPartIdsFromGeometryIds(creationIds));
            if (partNames.Length == 0) return null;
            //
            return partNames;
        }
        public string IsMeshSetupItemProperlyDefined(MeshSetupItem meshSetupItem)
        {
            return _controller.IsMeshSetupItemProperlyDefined(meshSetupItem);
        }
        public void AddMeshSetupItem(MeshSetupItem meshSetupItem)
        {
            _controller.AddMeshSetupItemCommand(meshSetupItem);
        }
        public void ReplaceMeshSetupItem(string oldMeshSetupItemName, MeshSetupItem meshSetupItem)
        {
            _controller.ReplaceMeshSetupItemCommand(oldMeshSetupItemName, meshSetupItem);
        }
        public void DuplicateMeshSetupItems(string[] meshSetupItemNames)
        {
            _controller.DuplicateMeshSetupItemsCommand(meshSetupItemNames);
        }
        public void RemoveMeshSetupItems(string[] meshSetupItemNames)
        {
            _controller.RemoveMeshSetupItemsCommand(meshSetupItemNames);
        }
        public void RenameMeshSetupItem(NamedClass item, string newName)
        {
            // A mesh setup item lives in the geometry view
            _controller.RenameCommand(item, newName, null, ViewGeometryModelResults.Geometry);
        }
        // The controller already collects which parts meshed and why the others did not, and writes the
        // reasons to the output window. This hands them to the caller as well, because an agent has no
        // output window to read. A call that meshed nothing records no command, so it raises rather than
        // returning a report of an operation that left no trace
        public Dictionary<string, string> CreateMeshes(string[] partNames)
        {
            _controller.CreateMeshesCommand(partNames);
            //
            HashSet<string> meshed = new HashSet<string>(_controller.LastMeshedPartNames);
            string[] errors = _controller.LastMeshingErrors;
            Dictionary<string, string> report = new Dictionary<string, string>();
            foreach (string partName in partNames)
            {
                if (report.ContainsKey(partName)) continue;
                report.Add(partName, meshed.Contains(partName) ? "" : GetMeshingError(partName, errors));
            }
            if (meshed.Count == 0)
            {
                throw new PythonAPIException("No part was meshed. " +
                                             (errors.Length > 0 ? string.Join(" ", errors)
                                                                : "The mesher reported no reason."));
            }
            return report;
        }
        // An error the mesher attributed to a part opens with its name; one it could not attribute - a
        // cancelled run - belongs to every part that did not mesh
        private static string GetMeshingError(string partName, string[] errors)
        {
            string prefix = partName + ": ";
            List<string> unattributed = new List<string>();
            foreach (string error in errors)
            {
                if (error == null) continue;
                if (error.StartsWith(prefix, StringComparison.Ordinal)) return error.Substring(prefix.Length);
                // The mesher opens an attributed error with the part name and a colon
                if (error.IndexOf(": ", StringComparison.Ordinal) < 0) unattributed.Add(error);
            }
            if (unattributed.Count > 0) return string.Join(" ", unattributed.ToArray());
            return "The mesh was not created.";
        }
        // Mesh tools
        public void CreateBoundaryLayer(GeometrySelection geometrySelection, double thickness)
        {
            _controller.CreateBoundaryLayerCommand(geometrySelection, thickness);
        }
        public void RemeshElements(RemeshingParameters remeshingParameters)
        {
            _controller.RemeshElementsCommand(remeshingParameters);
        }
        public void ThickenShellMesh(string[] partNames, double thickness, int numberOfLayers, double offset,
                                     bool keepModelEdges)
        {
            _controller.ThickenShellMeshCommand(partNames, thickness, numberOfLayers, offset, keepModelEdges);
        }
        public void SplitPartMeshUsingSurface(SplitPartMeshData splitPartMeshData)
        {
            _controller.SplitPartMeshUsingSurfaceCommand(splitPartMeshData);
        }
        public void UpdateNodalCoordinatesFromFile(string fileName)
        {
            _controller.UpdateNodalCoordinatesFromFileCommand(fileName);
        }
        // Nodes and elements
        public void RenumberNodes(int startId)
        {
            _controller.RenumberNodesCommand(startId);
        }
        public void RenumberElements(int startId)
        {
            _controller.RenumberElementsCommand(startId);
        }
        public void MergeCoincidentNodes(MergeCoincidentNodes mergeCoincidentNodes)
        {
            _controller.MergeCoincidentNodesCommand(mergeCoincidentNodes);
        }
        public void CreateElementSetFromElementQuality(string name, string elementQualityMetric,
                                                       string[] partNames, bool largerThan, double limit)
        {
            _controller.CreateElementSetFromElementQualityCommand(name, elementQualityMetric, partNames,
                                                                  largerThan, limit);
        }
        // Part element types
        public PartProperties GetModelPartProperties(string partName)
        {
            return _controller.GetModelPart(partName).GetProperties();
        }
        public PartProperties GetGeometryPartProperties(string partName)
        {
            return _controller.GetGeometryPart(partName).GetProperties();
        }
        public void ReplaceGeometryPartProperties(string oldPartName, PartProperties newPartProperties)
        {
            _controller.ReplaceGeometryPartPropertiesCommand(oldPartName, newPartProperties);
        }
        public void TranslateGeometryParts(string[] partNames, double[] translateVector, int numberOfCopies)
        {
            _controller.TranslateGeometryPartsCommand(partNames, translateVector, numberOfCopies);
        }
        public void RotateGeometryParts(string[] partNames, double[] rotateCenter, double[] rotateAxis,
                                        double rotateAngle, int numberOfCopies)
        {
            _controller.RotateGeometryPartsCommand(partNames, rotateCenter, rotateAxis, rotateAngle, numberOfCopies);
        }
        public void MirrorGeometryParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection, bool copy)
        {
            _controller.MirrorGeometryPartsCommand(partNames, mirrorPoint, mirrorDirection, copy);
        }
        public void ScaleGeometryParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            _controller.ScaleGeometryPartsCommand(partNames, scaleCenter, scaleFactors, copy);
        }
        public void HideGeometryParts(string[] partNames)
        {
            _controller.HideGeometryPartsCommand(partNames);
        }
        public void ShowGeometryParts(string[] partNames)
        {
            _controller.ShowGeometryPartsCommand(partNames);
        }
        public void SetColorForGeometryParts(string[] partNames, Color color)
        {
            _controller.SetColorForGeometryPartsCommand(partNames, color);
        }
        public void ResetColorForGeometryParts(string[] partNames)
        {
            _controller.ResetColorForGeometryPartsCommand(partNames);
        }
        public void SetTransparencyForGeometryParts(string[] partNames, byte alpha)
        {
            _controller.SetTransparencyForGeometryPartsCommand(partNames, alpha);
        }
        public void RemoveGeometryParts(string[] partNames)
        {
            _controller.RemoveGeometryPartsCommand(partNames);
        }
        public void CreateAndImportCompoundPart(string[] partNames)
        {
            _controller.CreateAndImportCompoundPartCommand(partNames);
        }
        public void RegenerateCompoundParts(string[] compoundPartNames)
        {
            _controller.RegenerateCompoundPartsCommand(compoundPartNames);
        }
        public void SwapPartGeometries(string partName1, string partName2)
        {
            _controller.SwapPartGeometriesCommand(partName1, partName2);
        }
        public void ConvertShellToSolid(string[] partNames)
        {
            _controller.ConvertShellToSolidCommand(partNames);
        }
        public void ConvertSolidToShell(string[] partNames)
        {
            _controller.ConvertSolidToShellCommand(partNames);
        }
        public void FindEdgesByAngleForGeometryParts(string[] partNames, double edgeAngle)
        {
            _controller.FindEdgesByAngleForGeometryPartsCommand(partNames, edgeAngle);
        }
        public Dictionary<string, int> FlipFaceOrientations(GeometrySelection geometrySelection)
        {
            _controller.FlipFaceOrientationsCommand(geometrySelection);
            return _controller.LastGeometryOperationCounts;
        }
        // The dialog reads the tool out of the picks as they are made and refuses an OK it cannot apply,
        // so the same is done here before anything is recorded
        public Dictionary<string, int> SplitGeometry(GeometryFeature geometryFeature)
        {
            if (geometryFeature is SplitByPointAndNormal pointAndNormal) ResolvePointAndNormal(pointAndNormal);
            else _controller.UpdateSplitGeometryFromSplitterSelection(geometryFeature);
            _controller.UpdateSplitGeometryFromDirectionSelection(geometryFeature);
            //
            string error = geometryFeature.CheckValidity();
            if (error != null) throw new PythonAPIException(error);
            //
            _controller.SplitGeometryCommand(geometryFeature);
            return _controller.LastGeometryOperationCounts;
        }
        public void RenameSplitSubPart(string compoundPartName, string oldSubPartName, string newSubPartName)
        {
            _controller.RenameSplitSubPartCommand(compoundPartName, oldSubPartName, newSubPartName);
        }
        public string GroupSplitCells(string compoundPartName, string[] cellNames, string subPartName)
        {
            // The grouping is resolved as the command would resolve it, so a refusal is raised before anything
            // is recorded and names what the script asked for
            try
            {
                _controller.GetGroupSplitCellsSubPartName(compoundPartName, cellNames, subPartName);
            }
            catch (CaeException ex)
            {
                throw new PythonAPIException(ex.Message);
            }
            return _controller.GroupSplitCellsCommand(compoundPartName, cellNames, subPartName);
        }
        public string FuseBodies(string compoundPartName, string[] bodyNames)
        {
            // The cells are checked as the command checks them, so a refusal is raised before anything is
            // recorded; cells the modelling kernel cannot fuse are refused by the command, which then records
            // nothing either
            try
            {
                _controller.CheckFuseBodies(compoundPartName, bodyNames);
                return _controller.FuseBodiesCommand(compoundPartName, bodyNames);
            }
            catch (CaeException ex)
            {
                throw new PythonAPIException(ex.Message);
            }
        }
        // A value that was not typed is taken from the picks, or for the point from the center of the
        // region, where the dialog puts a plane before anything is picked. A typed value the picks
        // would replace with a different one is refused rather than dropped.
        private void ResolvePointAndNormal(SplitByPointAndNormal pointAndNormal)
        {
            double[] typedPoint = pointAndNormal.Point;
            double[] typedNormal = pointAndNormal.Normal;
            int[] splitterIds = pointAndNormal.SplitterIds;
            //
            pointAndNormal.Point = new double[3];
            pointAndNormal.Normal = new double[3];
            pointAndNormal.SplitterIds = null;
            _controller.SetSplitGeometryDefaultPlanePoint(pointAndNormal);
            pointAndNormal.SplitterIds = splitterIds;
            if (typedPoint != null) pointAndNormal.Point = typedPoint.ToArray();
            if (typedNormal != null) pointAndNormal.Normal = typedNormal.ToArray();
            //
            _controller.UpdateSplitGeometryFromSplitterSelection(pointAndNormal);
            //
            if (typedPoint != null && !AreSamePoint(typedPoint, pointAndNormal.Point))
                throw new PythonAPIException("The argument 'point' differs from the point the 'splitter' " +
                                             "selection gives. Give one of them.");
            if (typedNormal != null && !AreSameDirection(typedNormal, pointAndNormal.Normal))
                throw new PythonAPIException("The argument 'normal' differs from the normal the 'splitter' " +
                                             "selection gives. Give one of them.");
        }
        private static bool AreSamePoint(double[] first, double[] second)
        {
            for (int i = 0; i < 3; i++)
            {
                if (Math.Abs(first[i] - second[i]) > 1E-9 * (1 + Math.Abs(first[i]))) return false;
            }
            return true;
        }
        private static bool AreSameDirection(double[] first, double[] second)
        {
            double firstLength = Math.Sqrt(first[0] * first[0] + first[1] * first[1] + first[2] * first[2]);
            double secondLength = Math.Sqrt(second[0] * second[0] + second[1] * second[1] + second[2] * second[2]);
            if (firstLength <= 0 || secondLength <= 0) return false;
            //
            double cosine = (first[0] * second[0] + first[1] * second[1] + first[2] * second[2]) /
                            (firstLength * secondLength);
            return cosine > 1 - 1E-9;
        }
        // The faces a part lost are counted around the command rather than reported by it, so the count is
        // the one the model ended up with and not the one the operation believed it had made
        public Dictionary<string, int> Defeature(GeometrySelection geometrySelection)
        {
            Dictionary<string, int> facesBefore = _controller.GetGeometryPartFaceCounts();
            _controller.DefeatureCommand(geometrySelection);
            Dictionary<string, int> facesAfter = _controller.GetGeometryPartFaceCounts();
            //
            Dictionary<string, int> facesRemoved = new Dictionary<string, int>();
            foreach (string partName in _controller.LastDefeaturedPartNames)
            {
                int before;
                int after;
                if (!facesBefore.TryGetValue(partName, out before)) continue;
                if (!facesAfter.TryGetValue(partName, out after)) continue;
                facesRemoved.Add(partName, before - after);
            }
            return facesRemoved;
        }
        public string[] OffsetFace(GeometrySelection geometrySelection, double offset)
        {
            FaceOffset faceOffset = new FaceOffset();
            faceOffset.CreationIds = geometrySelection.GeometryIds;
            faceOffset.CreationData = geometrySelection.CreationData;
            faceOffset.Offset = offset;
            //
            _controller.OffsetFaceCommand(faceOffset);
            return _controller.LastOffsetFacePartNames;
        }
        public Dictionary<string, int> DeleteCadPartFaces(GeometrySelection geometrySelection)
        {
            _controller.DeleteCadPartFacesCommand(geometrySelection);
            return _controller.LastGeometryOperationCounts;
        }
        public Dictionary<string, int> FuseFaces(GeometrySelection geometrySelection)
        {
            _controller.FuseFacesCommand(geometrySelection);
            return _controller.LastGeometryOperationCounts;
        }
        public Dictionary<string, int> RepairGeometry(string[] partNames, GeometryRepair geometryRepair)
        {
            _controller.RepairGeometryCommand(partNames, geometryRepair);
            return _controller.LastGeometryOperationCounts;
        }
        public Dictionary<string, GeometryRepairPreview> PreviewRepairGeometry(string[] partNames,
                                                                                GeometryRepair geometryRepair)
        {
            return _controller.GetRepairGeometryPreview(partNames, geometryRepair);
        }
        // Stl parts
        public void FlipStlPartSurfacesNormal(string[] partNames)
        {
            _controller.FlipStlPartSurfacesNormalCommand(partNames);
        }
        public Dictionary<string, int> DeleteStlPartFaces(GeometrySelection geometrySelection)
        {
            _controller.DeleteStlPartFacesCommand(geometrySelection);
            return _controller.LastGeometryOperationCounts;
        }
        // Model parts
        public void ReplaceModelPartProperties(string oldPartName, PartProperties newPartProperties)
        {
            _controller.ReplaceModelPartPropertiesCommand(oldPartName, newPartProperties);
        }
        public void HideModelParts(string[] partNames)
        {
            _controller.HideModelPartsCommand(partNames);
        }
        public void ShowModelParts(string[] partNames)
        {
            _controller.ShowModelPartsCommand(partNames);
        }
        public void SetColorForModelParts(string[] partNames, Color color)
        {
            _controller.SetColorForModelPartsCommand(partNames, color);
        }
        public void ResetColorForModelParts(string[] partNames)
        {
            _controller.ResetColorForModelPartsCommand(partNames);
        }
        public void SetTransparencyForModelParts(string[] partNames, byte alpha)
        {
            _controller.SetTransparencyForModelPartsCommand(partNames, alpha);
        }
        public void TranslateModelParts(string[] partNames, double[] translateVector, int numberOfCopies)
        {
            _controller.TranslateModelPartsCommand(partNames, translateVector, numberOfCopies);
        }
        public void RotateModelParts(string[] partNames, double[] rotateCenter, double[] rotateAxis,
                                     double rotateAngle, int numberOfCopies)
        {
            _controller.RotateModelPartsCommand(partNames, rotateCenter, rotateAxis, rotateAngle, numberOfCopies);
        }
        public void MirrorModelParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection, bool copy)
        {
            _controller.MirrorModelPartsCommand(partNames, mirrorPoint, mirrorDirection, copy);
        }
        public void ScaleModelParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            _controller.ScaleModelPartsCommand(partNames, scaleCenter, scaleFactors, copy);
        }
        public void MergeModelParts(string[] partNames)
        {
            _controller.MergeModelPartsCommand(partNames);
        }
        public void RemoveModelParts(string[] partNames)
        {
            _controller.RemoveModelPartsCommand(partNames);
        }
        public void FindEdgesByAngleForModelParts(string[] partNames, double edgeAngle)
        {
            _controller.FindEdgesByAngleForModelPartsCommand(partNames, edgeAngle);
        }
    }
}
