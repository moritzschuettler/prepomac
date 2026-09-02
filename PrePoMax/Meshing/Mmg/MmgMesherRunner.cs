// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using CaeMesh;
using FileInOut.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PrePoMax
{
    internal class MmgMesherRunner : IMesherRunner
    {
        // Variables                                                                                                                
        private Controller _controller;


        // Properties                                                                                                               
        public MesherTypeEnum MesherType { get { return MesherTypeEnum.Mmg; } }


        // Constructors                                                                                                             
        public MmgMesherRunner(Controller controller)
        {
            _controller = controller;
        }


        // Methods                                                                                                                  
        public bool CanCreateMeshFromBrep(GeometryPart part)
        {
            return false;
        }
        public bool CanCreateMeshFromStl(GeometryPart part)
        {
            return part.PartType == PartType.Shell;
        }
        public GeneratedMeshData CreateMeshFromBrep(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                                    string workDirectory)
        {
            throw new NotSupportedException("MesherTypeException");
        }
        public bool PreviewEdgeMeshFromBrep(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                            OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            throw new NotSupportedException("MesherTypeException");
        }
        public bool PreviewEdgeMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                           OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            return false;
        }
        public FeMesh CreateFirstOrderShellMeshForQuadWild(GeometryPart part, string workDirectory,
                                                           OrderedDictionary<string, MeshSetupItem> meshSetupItems = null)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (part.PartType != PartType.Shell)
                throw new CaeException("The QuadWild triangular pre-pass requires a shell geometry part.");
            if (String.IsNullOrWhiteSpace(workDirectory) || !Directory.Exists(workDirectory))
                throw new CaeException("The QuadWild work directory does not exist.");
            //
            string executable = Application.StartupPath + Globals.MmgsMesher;
            string mmgInFileName = Path.Combine(workDirectory, Globals.MmgMeshFileName);
            string mmgOutFileName = Path.Combine(workDirectory, Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) +
                                                 ".o" + Path.GetExtension(Globals.MmgMeshFileName));
            string mmgSolFileName = Path.Combine(workDirectory,
                                                 Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) + ".sol");
            //
            if (File.Exists(mmgInFileName)) File.Delete(mmgInFileName);
            if (File.Exists(mmgOutFileName)) File.Delete(mmgOutFileName);
            if (File.Exists(mmgSolFileName)) File.Delete(mmgSolFileName);
            //
            MeshingParameters meshingParameters = _controller.GetPartMeshingParameters(part.Name, meshSetupItems);
            meshingParameters.SecondOrder = false;
            meshingParameters.QuadDominated = false;
            Dictionary<int, double> sourceNodeIdMeshSize =
                GetQuadWildNodeMeshSizes(part, meshingParameters, meshSetupItems);
            _controller.SuppressExplodedView(new string[] { part.Name });
            try
            {
                MmgFileWriter.Write(mmgInFileName, part, _controller.Model.Geometry, true, false);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            double hausdorff = meshingParameters.Hausdorff;
            if (meshingParameters.RelativeSize)
                hausdorff = part.BoundingBox.GetDiagonal() * meshingParameters.FactorHausdorff;
            _controller.Form.WriteDataToOutput("MMG QuadWild pre-pass: reconstructing the STL surface...");
            string argument = "-m " + ramCounter.NextValue() * 0.9 + " " +
                              "-ar 0.01 " +
                              "-hmax " + meshingParameters.MaxH + " " +
                              "-hmin " + meshingParameters.MinH + " " +
                              "-hausd " + hausdorff + " " +
                              "-in \"" + mmgInFileName + "\" " +
                              "-out \"" + mmgOutFileName + "\" ";
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                             _controller.Form.WriteDataToOutput);
            if (jobStatus != JobStatus.OK)
                throw new CaeException("MMG could not reconstruct the STL surface for the QuadWild pre-pass.");
            // Match the normal MMG STL workflow: its first pass reconstructs the surface description, while
            // the second -nr pass performs the actual remeshing. Stopping here merely returns the source STL mesh.
            FeMesh intermediateMesh = FileInOut.Input.MmgFileReader.Read(
                mmgOutFileName, FileInOut.Input.ElementsToImport.Shell, MeshRepresentation.Geometry);
            GeometryPart intermediatePart;
            if (intermediateMesh.Parts.Count == 1)
                intermediatePart = (GeometryPart)intermediateMesh.Parts.First().Value;
            else intermediateMesh.MergeGeometryParts(intermediateMesh.Parts.Keys.ToArray(),
                                                      out intermediatePart, out _);
            //
            if (File.Exists(mmgInFileName)) File.Delete(mmgInFileName);
            if (File.Exists(mmgOutFileName)) File.Delete(mmgOutFileName);
            if (File.Exists(mmgSolFileName)) File.Delete(mmgSolFileName);
            MmgFileWriter.Write(mmgInFileName, intermediatePart, intermediateMesh, true, true);
            if (sourceNodeIdMeshSize != null)
            {
                Dictionary<int, double> intermediateNodeIdMeshSize = TransferQuadWildNodeMeshSizes(
                    sourceNodeIdMeshSize, _controller.Model.Geometry, intermediatePart, intermediateMesh,
                    part.BoundingBox.GetDiagonal());
                MmgFileWriter.WriteSolution(mmgSolFileName, intermediatePart, intermediateMesh,
                                            intermediateNodeIdMeshSize);
            }
            //
            _controller.Form.WriteDataToOutput("MMG QuadWild pre-pass: remeshing the reconstructed surface...");
            string solutionArgument = File.Exists(mmgSolFileName) ? "-sol \"" + mmgSolFileName + "\" " : "";
            argument = "-nr " +
                       "-m " + ramCounter.NextValue() * 0.9 + " " +
                       "-hmax " + meshingParameters.MaxH + " " +
                       "-hmin " + meshingParameters.MinH + " " +
                       "-hausd " + hausdorff + " " +
                       solutionArgument +
                       "-in \"" + mmgInFileName + "\" " +
                       "-out \"" + mmgOutFileName + "\" ";
            jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                   _controller.Form.WriteDataToOutput);
            if (jobStatus != JobStatus.OK)
                throw new CaeException("MMG could not remesh the triangular QuadWild input surface.");
            //
            FeMesh mesh = _controller.Model.PrepareGeneratedMeshFromMeshFile(mmgOutFileName, part,
                                                                              false, false, false);
            if (mesh.Parts.Count != 1 || !mesh.Parts.ContainsKey(part.Name))
                throw new CaeException("The MMG QuadWild pre-pass produced an unexpected mesh part layout.");
            return mesh;
        }
        private static Dictionary<int, double> TransferQuadWildNodeMeshSizes(
            Dictionary<int, double> sourceNodeIdMeshSize, FeMesh sourceMesh,
            GeometryPart targetPart, FeMesh targetMesh, double boundingBoxDiagonal)
        {
            int[] sourceNodeIds = sourceNodeIdMeshSize.Keys.ToArray();
            if (sourceNodeIds.Length == 0) return null;
            // The first MMG pass can insert or move nodes. Transfer the source metric to the reconstructed
            // surface by nearest position so local mesh sizes survive into the actual remeshing pass.
            double worldSize = boundingBoxDiagonal;
            if (worldSize <= 0 || Double.IsNaN(worldSize) || Double.IsInfinity(worldSize)) worldSize = 1;
            FeNode firstNode = sourceMesh.Nodes[sourceNodeIds[0]];
            Octree.PointOctree<int> octree = new Octree.PointOctree<int>(
                worldSize, new Octree.Point(firstNode.X, firstNode.Y, firstNode.Z), worldSize * 1E-9);
            foreach (int nodeId in sourceNodeIds)
            {
                FeNode node = sourceMesh.Nodes[nodeId];
                octree.Add(nodeId, new Octree.Point(node.X, node.Y, node.Z));
            }
            //
            Dictionary<int, double> targetNodeIdMeshSize = new Dictionary<int, double>();
            foreach (int targetNodeId in targetPart.NodeLabels)
            {
                FeNode targetNode = targetMesh.Nodes[targetNodeId];
                Octree.Point targetPoint = new Octree.Point(targetNode.X, targetNode.Y, targetNode.Z);
                double searchRadius = worldSize * 1E-9;
                int[] nearbyNodeIds = new int[0];
                for (int i = 0; i < 32 && nearbyNodeIds.Length == 0; i++)
                {
                    nearbyNodeIds = octree.GetNearby(targetPoint, searchRadius);
                    searchRadius *= 2;
                }
                if (nearbyNodeIds.Length == 0) nearbyNodeIds = sourceNodeIds;
                //
                int nearestNodeId = nearbyNodeIds[0];
                double nearestDistance2 = Double.MaxValue;
                foreach (int sourceNodeId in nearbyNodeIds)
                {
                    FeNode sourceNode = sourceMesh.Nodes[sourceNodeId];
                    double dx = targetNode.X - sourceNode.X;
                    double dy = targetNode.Y - sourceNode.Y;
                    double dz = targetNode.Z - sourceNode.Z;
                    double distance2 = dx * dx + dy * dy + dz * dz;
                    if (distance2 < nearestDistance2)
                    {
                        nearestDistance2 = distance2;
                        nearestNodeId = sourceNodeId;
                    }
                }
                targetNodeIdMeshSize.Add(targetNodeId, sourceNodeIdMeshSize[nearestNodeId]);
            }
            return targetNodeIdMeshSize;
        }
        private Dictionary<int, double> GetQuadWildNodeMeshSizes(GeometryPart part, MeshingParameters meshingParameters,
                                                                 OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            if (meshSetupItems == null) meshSetupItems = _controller.Model.Geometry.MeshSetupItems;
            HashSet<int> targetPartIds = new HashSet<int>();
            targetPartIds.Add(part.PartId);
            if (part is CompoundGeometryPart compoundPart)
            {
                foreach (string subPartName in compoundPart.SubPartNames)
                    targetPartIds.Add(_controller.Model.Geometry.Parts[subPartName].PartId);
            }
            //
            Dictionary<int, double> nodeIdMeshSize = null;
            double defaultSize = meshingParameters.MaxH;
            if (defaultSize <= 0) defaultSize = part.BoundingBox.GetDiagonal();
            if (defaultSize <= 0) defaultSize = 1;
            if (defaultSize < meshingParameters.MinH) defaultSize = meshingParameters.MinH;
            foreach (var entry in meshSetupItems)
            {
                if (!(entry.Value is LocalMeshSize localMeshSize) || !localMeshSize.Active || !localMeshSize.Valid ||
                    localMeshSize.CreationIds == null)
                    continue;
                foreach (int geometryId in localMeshSize.CreationIds)
                {
                    int[] itemTypePartIds = FeMesh.GetItemTypePartIdsFromGeometryId(geometryId);
                    if (!targetPartIds.Contains(itemTypePartIds[2])) continue;
                    if (nodeIdMeshSize == null)
                    {
                        nodeIdMeshSize = new Dictionary<int, double>();
                        foreach (int nodeId in part.NodeLabels) nodeIdMeshSize.Add(nodeId, defaultSize);
                    }
                    //
                    GeometryType geometryType = (GeometryType)itemTypePartIds[1];
                    if (localMeshSize.Type == LocalMeshSizeTypeEnum.ElementSize)
                    {
                        double meshSize = localMeshSize.GetMeshSize(-1, meshingParameters.MinH,
                                                                   meshingParameters.MaxH);
                        SetNodeMeshSize(nodeIdMeshSize,
                                        _controller.Model.Geometry.GetNodeIdsFromGeometryId(geometryId), meshSize);
                    }
                    else if (localMeshSize.Type == LocalMeshSizeTypeEnum.NumberOfElements)
                    {
                        BasePart selectedPart = _controller.Model.Geometry.GetPartFromId(itemTypePartIds[2]);
                        int[] edgeIds;
                        if (geometryType.IsEdge()) edgeIds = new int[] { itemTypePartIds[0] };
                        else if (geometryType.IsSurface()) edgeIds = selectedPart.Visualization.FaceEdgeIds[itemTypePartIds[0]];
                        else if (geometryType == GeometryType.Part)
                        {
                            edgeIds = new int[selectedPart.Visualization.EdgeCount];
                            for (int i = 0; i < edgeIds.Length; i++) edgeIds[i] = i;
                        }
                        else continue;
                        //
                        foreach (int edgeId in edgeIds)
                        {
                            double edgeLength = selectedPart.Visualization.EdgeLengths[edgeId];
                            double meshSize = localMeshSize.GetMeshSize(edgeLength, meshingParameters.MinH,
                                                                       meshingParameters.MaxH);
                            int edgeGeometryId = FeMesh.GetGeometryId(edgeId, (int)GeometryType.Edge,
                                                                     selectedPart.PartId);
                            SetNodeMeshSize(nodeIdMeshSize,
                                            _controller.Model.Geometry.GetNodeIdsFromGeometryId(edgeGeometryId), meshSize);
                        }
                    }
                    else throw new NotSupportedException();
                }
            }
            return nodeIdMeshSize;
        }
        private static void SetNodeMeshSize(Dictionary<int, double> nodeIdMeshSize, int[] nodeIds, double meshSize)
        {
            foreach (int nodeId in nodeIds)
            {
                if (nodeIdMeshSize.TryGetValue(nodeId, out double existingSize) && meshSize < existingSize)
                    nodeIdMeshSize[nodeId] = meshSize;
            }
        }
        public GeneratedMeshData CreateMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems, string workDirectory)
        {
            string executable = Application.StartupPath + Globals.MmgsMesher;
            string mmgInFileName = Path.Combine(workDirectory, Globals.MmgMeshFileName);
            string mmgOutFileName = Path.Combine(workDirectory, Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) +
                                                 ".o" + Path.GetExtension(Globals.MmgMeshFileName));
            string mmgSolFileName = Path.Combine(workDirectory,
                                                 Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) +
                                                 ".sol");
            //
            if (File.Exists(mmgInFileName)) File.Delete(mmgInFileName);
            if (File.Exists(mmgOutFileName)) File.Delete(mmgOutFileName);
            if (File.Exists(mmgSolFileName)) File.Delete(mmgSolFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            MeshingParameters meshingParameters = _controller.GetPartMeshingParameters(part.Name);
            try
            {
                MmgFileWriter.Write(mmgInFileName, part, _controller.Model.Geometry, meshingParameters.KeepModelEdges, false);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            System.Diagnostics.PerformanceCounter ramCounter;
            ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            double hausdorff = meshingParameters.Hausdorff;
            if (meshingParameters.RelativeSize) hausdorff = part.BoundingBox.GetDiagonal() * meshingParameters.FactorHausdorff;
            //
            string argument = //"-nr " + 
                              "-m " + ramCounter.NextValue() * 0.9 + " " +
                              "-ar 0.01 " + // this removes curving of the faces
                                            //"-hsiz 0.08 " +  
                              "-hmax " + meshingParameters.MaxH + " " +
                              "-hmin " + meshingParameters.MinH + " " +
                              "-hausd " + hausdorff + " " +
                              "-in \"" + mmgInFileName + "\" " +
                              "-out \"" + mmgOutFileName + "\" ";
            //
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                 _controller.Form.WriteDataToOutput);
            // Job completed
            if (jobStatus == JobStatus.OK)
            {
                FeMesh mesh = FileInOut.Input.MmgFileReader.Read(mmgOutFileName, FileInOut.Input.ElementsToImport.Shell,
                                                                 MeshRepresentation.Geometry);
                GeometryPart partOut;
                if (mesh.Parts.Count == 1) partOut = (GeometryPart)mesh.Parts.First().Value;
                else mesh.MergeGeometryParts(mesh.Parts.Keys.ToArray(), out partOut, out _);
                //
                if (File.Exists(mmgInFileName)) File.Delete(mmgInFileName);
                if (File.Exists(mmgOutFileName)) File.Delete(mmgOutFileName);
                if (File.Exists(mmgSolFileName)) File.Delete(mmgSolFileName);
                //
                MmgFileWriter.Write(mmgInFileName, partOut, mesh, meshingParameters.KeepModelEdges, true);
                //
                argument = "-nr " +
                           "-m " + ramCounter.NextValue() * 0.9 + " " +
                           //"-ar 0 " +
                           //"-hsiz 0.08 " +  
                           "-hmax " + meshingParameters.MaxH + " " +
                           "-hmin " + meshingParameters.MinH + " " +
                           "-hausd " + hausdorff + " " +
                           "-in \"" + mmgInFileName + "\" " +
                           "-out \"" + mmgOutFileName + "\" ";
                //
                jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                           _controller.Form.WriteDataToOutput);
                //
                if (jobStatus == JobStatus.OK)
                {
                    return new GeneratedMeshData()
                    {
                        GeometryPart = part,
                        MeshFileName = mmgOutFileName,
                        SmoothShaded = false
                    };
                }
                else throw new CaeException("Mesh generation failed.");
            }
            else throw new CaeException("Mesh generation failed.");
        }
        public bool RemeshShellElementsByPart(MeshPart part, int[] elementIds, RemeshingParameters remeshingParameters,
                                               bool preview)
        {
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return false;
            }
            //
            string executable = Application.StartupPath + Globals.MmgsMesher;
            string mmgInFileName = Path.Combine(workDirectory, Globals.MmgMeshFileName);
            string mmgOutFileName = Path.Combine(workDirectory, Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) +
                                                 ".o" + Path.GetExtension(Globals.MmgMeshFileName));
            string mmgSolFileName = Path.Combine(workDirectory,
                                                 Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) +
                                                 ".sol");
            //
            if (File.Exists(mmgInFileName)) File.Delete(mmgInFileName);
            if (File.Exists(mmgOutFileName)) File.Delete(mmgOutFileName);
            if (File.Exists(mmgSolFileName)) File.Delete(mmgSolFileName);
            //
            Dictionary<(int, int), FeNode> midNodes;
            MmgFileWriter.WriteShellElements(mmgInFileName, elementIds, part, _controller.Model.Mesh,
                                             remeshingParameters.KeepModelEdges, out midNodes);
            //
            PerformanceCounter ramCounter;
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            //
            string argument = "-nr " +
                              "-optim " +
                              //"-ar 30 " +
                              "-m " + ramCounter.NextValue() * 0.9 + " " +
                              "-hgrad " + 1 + " " +
                              "-hgradreq " + 1 + " " +
                              "-hmax " + remeshingParameters.MaxH + " " +
                              "-hmin " + remeshingParameters.MinH + " " +
                              "-hausd " + remeshingParameters.Hausdorff + " " +
                              "-in \"" + mmgInFileName + "\" " +
                              "-out \"" + mmgOutFileName + "\" " +
                              "-v 5 ";
            //
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                            _controller.Form.WriteDataToOutput);
            // Job completed
            if (jobStatus == JobStatus.OK)
            {
                // Check if all elements are linear or all elements are parabolic
                HashSet<bool> parabolic = new HashSet<bool>();
                foreach (var elementType in part.ElementTypes) parabolic.Add(FeElement.IsParabolic(elementType));
                if (parabolic.Count != 1) throw new NotSupportedException();
                //
                _controller.ImportGeneratedRemesh(mmgOutFileName, elementIds, part, parabolic.First(), midNodes, preview);
                return true;
            }
            else throw new CaeException("Mesh generation failed.");
        }
        public bool SplitPartMeshUsingSurface(SplitPartMeshData splitPartMeshData)
        {
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return false;
            }
            //
            string executable = Application.StartupPath + Globals.Mmg3DMesher;
            string mmgInFileName = Path.Combine(workDirectory, Globals.MmgMeshFileName);
            string mmgOutFileName = Path.Combine(workDirectory, Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) +
                                                 ".o" + Path.GetExtension(Globals.MmgMeshFileName));
            string mmgSolFileName = Path.Combine(workDirectory,
                                                 Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) +
                                                 ".sol");
            string mmgMatFileName = Path.Combine(workDirectory,
                                                 Path.GetFileNameWithoutExtension(Globals.MmgMeshFileName) +
                                                 ".mmg3d");
            //
            if (File.Exists(mmgInFileName)) File.Delete(mmgInFileName);
            if (File.Exists(mmgOutFileName)) File.Delete(mmgOutFileName);
            if (File.Exists(mmgSolFileName)) File.Delete(mmgSolFileName);
            if (File.Exists(mmgMatFileName)) File.Delete(mmgMatFileName);
            //
            BasePart[] baseParts;
            Dictionary<int, double> nodeIdDistance;
            _controller.GetSplitPartMeshUsingSurfaceData(splitPartMeshData, out baseParts, out nodeIdDistance);
            // In file
            string[] basePartNames = new string[baseParts.Length];
            for (int i = 0; i < baseParts.Length; i++) basePartNames[i] = baseParts[i].Name;
            Guid? targetGroupId =
                Controller.GetLastOrderedDictionaryItemParentGroupId(_controller.Model.Mesh.Parts, basePartNames);
            HashSet<string> oldPartNames = Controller.CaptureOrderedDictionaryItemNamesForGroupPlacement(
                _controller.Model.Mesh.Parts, targetGroupId);
            _controller.ExportPartsAsMmgMesh(basePartNames, mmgInFileName, true);
            // Sol file
            FeMesh mesh = _controller.DisplayedMesh;
            MmgFileWriter.WriteSolution(mmgSolFileName, baseParts, mesh, nodeIdDistance);
            // Mat file
            MmgFileWriter.WriteMaterial(mmgMatFileName, baseParts);
            //
            PerformanceCounter ramCounter;
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            //
            string argument = "-ls " + //splitPartMeshData.Offset + " " +  // offset is applied in the signed distance field
                              "-rmc " +
                              //"-optim " + // tends to create elements of the same size
                              "-m " + ramCounter.NextValue() * 0.9 + " " +
                              "-hmax " + splitPartMeshData.MaxH + " " +
                              "-hmin " + splitPartMeshData.MinH + " " +
                              "-hausd " + splitPartMeshData.Hausdorff + " " +
                              "-hgrad " + "1.5" + " " +
                              "-nr " +
                              "-in \"" + mmgInFileName + "\" " +
                              "-sol \"" + mmgSolFileName + "\" " +
                              "-out \"" + mmgOutFileName + "\" " +
                              "-v 5 ";  // this solves the problem of mmg not stopping
            //
            JobStatus jobStatus = _controller.JobRunner.Run("SplitMeshPart", executable, argument, workDirectory,
                                                 _controller.Form.WriteDataToOutput);
            // Job completed
            if (jobStatus == JobStatus.OK)
            {
                // Check if all elements are linear or all elements are parabolic
                HashSet<bool> parabolic = new HashSet<bool>();
                foreach (var part in baseParts)
                {
                    foreach (var elementType in part.ElementTypes)
                        parabolic.Add(FeElement.IsParabolic(elementType));
                }
                if (parabolic.Count != 1) throw new NotSupportedException();
                //
                _controller.Model.ImportMeshFromMmgFile(mmgOutFileName, FileInOut.Input.ElementsToImport.Solid, parabolic.First());
                Controller.MoveAddedOrderedDictionaryItemsToGroup(_controller.Model.Mesh.Parts, oldPartNames, targetGroupId);
                //
                _controller.HideModelParts(basePartNames);
                //
                _controller.UpdateAfterImport(".mesh"); // calls _controller.UpdateAfterPartsChanged();
                //
                return true;
            }
            else throw new CaeException("Mesh generation failed.");
        }
    }
}
