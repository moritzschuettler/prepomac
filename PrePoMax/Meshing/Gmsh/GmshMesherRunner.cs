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
    internal class GmshMesherRunner : IMesherRunner
    {
        // Variables                                                                                                                
        private Controller _controller;


        // Properties                                                                                                               
        public MesherTypeEnum MesherType { get { return MesherTypeEnum.Gmsh; } }


        // Constructors                                                                                                             
        public GmshMesherRunner(Controller controller)
        {
            _controller = controller;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The setup items as the mesher is to see them for one part. A sweep mesh item may span parts that
        /// are meshed one at a time, so it is replaced by a clone carrying only the blocks that lie on the
        /// part being meshed.
        /// </summary>
        private MeshSetupItem[] GetSetupItemsForPart(GeometryPart part, MeshSetupItem[] meshSetupItems)
        {
            if (meshSetupItems == null) return null;
            //
            int[] partIds = null;
            MeshSetupItem[] partMeshSetupItems = null;
            for (int i = 0; i < meshSetupItems.Length; i++)
            {
                if (!(meshSetupItems[i] is SweepMesh sweepMesh)) continue;
                //
                if (partIds == null)
                {
                    string[] meshablePartNames = _controller.GetMeshablePartNames(new string[] { part.Name });
                    partIds = _controller.Model.Geometry.GetPartIdsFomPartNames(meshablePartNames);
                }
                if (partMeshSetupItems == null) partMeshSetupItems = meshSetupItems.ToArray();
                partMeshSetupItems[i] = sweepMesh.GetBlocksOnParts(partIds);
            }
            return partMeshSetupItems == null ? meshSetupItems : partMeshSetupItems;
        }
        public bool CanCreateMeshFromBrep(GeometryPart part)
        {
            return true;
        }
        public bool CanCreateMeshFromStl(GeometryPart part)
        {
            return true;
        }
        public bool PreviewEdgeMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                           OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            throw new NotSupportedException("MesherTypeException");
        }
        public string Defeature(GeometryPart part, int[] surfaceIds)
        {
            _controller.Form.WriteDataToOutput("");
            //
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return null;
            }
            //
            string executable = Application.StartupPath + Globals.GmshCaller;
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            string gmshDataFileName = Path.Combine(workDirectory, Globals.GmshDataFileName);
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            if (File.Exists(gmshDataFileName)) File.Delete(gmshDataFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            File.WriteAllText(brepFileName, part.CADFileData);
            GmshData gmshData = new GmshData();
            gmshData.GeometryFileName = brepFileName;
            gmshData.SurfaceIds = surfaceIds;
            _controller.Model.Geometry.GetPartTopologyForGmsh(part.Name, ref gmshData);
            gmshData.WriteToFile(gmshDataFileName);
            _controller.ResumeExplodedViews(false);
            //
            string argument = Globals.GmshDataFileName + " " + CaeMesh.GmshCommandEnum.Defeature;
            //
            string error = null;
            bool jobCompleted;
            if (Debugger.IsAttached)
            {
                GmshAPI gmsh = new GmshAPI(gmshData, _controller.Form.WriteDataToOutput);
                error = gmsh.Defeature();
                jobCompleted = (error == null);
            }
            else
            {
                JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                     _controller.Form.WriteDataToOutput);
                // Job completed
                jobCompleted = jobStatus == JobStatus.OK;
            }
            //
            if (jobCompleted)
            {
                return brepFileName;
            }
            else
            {
                string message = "Defeaturing failed.";
                if (error != null) message += Environment.NewLine + error;
                throw new CaeException(message);
            }
        }
        public bool PreviewEdgeMeshFromBrep(GeometryPart part, MeshSetupItem[] gmshSetupItems,
                                                OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return false;
            }
            //
            string executable = Application.StartupPath + Globals.GmshCaller;
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            string inpFileName = Path.Combine(workDirectory, Globals.InpMeshFileName);
            string gmshDataFileName = Path.Combine(workDirectory, Globals.GmshDataFileName);
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            if (File.Exists(inpFileName)) File.Delete(inpFileName);
            if (File.Exists(gmshDataFileName)) File.Delete(gmshDataFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            File.WriteAllText(brepFileName, part.CADFileData);
            MeshingParameters partMeshingParameters = _controller.GetPartMeshingParameters(part.Name, meshSetupItems);
            GmshData gmshData = new GmshData();
            gmshData.GeometryFileName = brepFileName;
            gmshData.InpFileName = inpFileName;
            gmshData.PartMeshingParameters = partMeshingParameters;
            gmshData.GmshSetupItems = GetSetupItemsForPart(part, gmshSetupItems);
            gmshData.Preview = true;
            _controller.Model.Geometry.GetPartTopologyForGmsh(part.Name, ref gmshData);
            _controller.GetMeshItemSizes(part.Name, ref gmshData, meshSetupItems);
            gmshData.WriteToFile(gmshDataFileName);
            _controller.ResumeExplodedViews(false);
            //
            string argument = Globals.GmshDataFileName + " " + CaeMesh.GmshCommandEnum.Mesh;
            //
            bool jobCompleted;
            if (Debugger.IsAttached)
            {
                CaeMesh.GmshAPI gmsh = new CaeMesh.GmshAPI(gmshData, _controller.Form.WriteDataToOutput);
                string error = gmsh.CreateMesh();
                jobCompleted = error == null;
            }
            else
            {
                JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                     _controller.Form.WriteDataToOutput);
                // Job completed
                jobCompleted = jobStatus == JobStatus.OK;
            }
            //
            if (jobCompleted)
            {
                int id2;
                int[] key;
                FeElement element;
                CompareIntArray comparer = new CompareIntArray();
                HashSet<int[]> edgeKeys = new HashSet<int[]>(comparer);
                List<double[][]> lines = new List<double[][]>();
                double[][] line;
                //
                FileInOut.Input.ElementsToImport elementsToImport;
                bool importEdges = gmshSetupItems.Length == 1 &&
                    (gmshSetupItems[0] is ShellGmsh || gmshSetupItems[0] is ExtrudeMesh || gmshSetupItems[0] is SweepMesh ||
                     gmshSetupItems[0] is RevolveMesh);
                if (importEdges) elementsToImport = FileInOut.Input.ElementsToImport.Shell;
                else elementsToImport = FileInOut.Input.ElementsToImport.Beam;
                // Mesh
                FeMesh mesh = FileInOut.Input.InpFileReader.ReadMesh(inpFileName, elementsToImport, false);
                // Exploded view
                if (_controller.IsExplodedViewActive())
                {
                    double[] offset;
                    if (part is CompoundGeometryPart cgp) offset = _controller.Model.Geometry.Parts[cgp.SubPartNames[0]].Offset;
                    else offset = part.Offset;
                    //
                    Dictionary<string, double[]> partOffsets = new Dictionary<string, double[]>();
                    foreach (var entry in mesh.Parts) partOffsets.Add(entry.Key, offset);
                    mesh.ApplyExplodedView(partOffsets);
                }
                // Import edges
                if (importEdges)
                {
                    foreach (var entry in mesh.Elements)
                    {
                        element = entry.Value;
                        int numOfNodes = -1;
                        if (entry.Value is LinearTriangleElement) numOfNodes = 3;
                        else if (entry.Value is LinearQuadrilateralElement) numOfNodes = 4;
                        //
                        if (numOfNodes > 0)
                        {
                            for (int i = 0; i < numOfNodes; i++)
                            {
                                id2 = (i + 1) % numOfNodes;
                                key = Tools.GetSortedKey(element.NodeIds[i], element.NodeIds[id2]);
                                if (!edgeKeys.Contains(key))
                                {
                                    line = new double[2][];
                                    line[0] = mesh.Nodes[element.NodeIds[i]].Coor;
                                    line[1] = mesh.Nodes[element.NodeIds[id2]].Coor;
                                    lines.Add(line);
                                    edgeKeys.Add(key);
                                }
                            }
                        }
                        else throw new NotSupportedException();
                    }
                    //
                    _controller.HighlightConnectedEdges(lines.ToArray());
                }
                // Import nodes
                else
                {
                    double[][] nodeCoor = mesh.GetAllNodeCoor();
                    _controller.HighlightNodes(nodeCoor);
                }
                return true;
            }
            else return false;
        }
        public bool PreviewEdgeNodesForQuadWild(GeometryPart part,
                                                OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            string workDirectory = _controller.Settings.GetWorkDirectory();
            if (String.IsNullOrWhiteSpace(workDirectory) || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return false;
            }
            // A tetrahedral setup item makes the existing Gmsh preview path stop after its 1D edge mesh.
            string executable = Application.StartupPath + Globals.GmshCaller;
            string geometryFileName = Path.Combine(workDirectory,
                                                   part.IsCADPart ? Globals.BrepFileName : Globals.StlFileName);
            string inpFileName = Path.Combine(workDirectory, Globals.QuadWildGmshInpFileName);
            string gmshDataFileName = Path.Combine(workDirectory, Globals.QuadWildGmshDataFileName);
            //
            if (File.Exists(geometryFileName)) File.Delete(geometryFileName);
            if (File.Exists(inpFileName)) File.Delete(inpFileName);
            if (File.Exists(gmshDataFileName)) File.Delete(gmshDataFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            GmshData gmshData = new GmshData();
            try
            {
                if (part.IsCADPart) File.WriteAllText(geometryFileName, part.CADFileData);
                else StlFileWriter.Write(geometryFileName, _controller.Model.Geometry,
                                         new string[] { part.Name });
                MeshingParameters partMeshingParameters = _controller.GetPartMeshingParameters(part.Name,
                                                                                                meshSetupItems);
                partMeshingParameters.SecondOrder = false;
                partMeshingParameters.QuadDominated = false;
                TetrahedralGmsh edgeMesher = new TetrahedralGmsh("QuadWild_edge_preview");
                //
                gmshData.GeometryFileName = geometryFileName;
                gmshData.InpFileName = inpFileName;
                gmshData.PartMeshingParameters = partMeshingParameters;
                gmshData.GmshSetupItems = new MeshSetupItem[] { edgeMesher };
                gmshData.Preview = true;
                if (!part.IsCADPart)
                    gmshData.StlFeatureAngleDeg = _controller.Settings.General.EdgeAngle;
                _controller.Model.Geometry.GetPartTopologyForGmsh(part.Name, ref gmshData);
                _controller.GetMeshItemSizes(part.Name, ref gmshData, meshSetupItems);
                gmshData.WriteToFile(gmshDataFileName);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            string argument = Globals.QuadWildGmshDataFileName + " " + CaeMesh.GmshCommandEnum.Mesh;
            bool jobCompleted;
            if (Debugger.IsAttached)
            {
                GmshAPI gmsh = new GmshAPI(gmshData, _controller.Form.WriteDataToOutput);
                jobCompleted = gmsh.CreateMesh() == null;
            }
            else
            {
                JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                                 _controller.Form.WriteDataToOutput);
                jobCompleted = jobStatus == JobStatus.OK;
            }
            if (!jobCompleted) return false;
            // The preview output contains only line elements, so display its nodes without importing a mesh.
            FeMesh mesh = FileInOut.Input.InpFileReader.ReadMesh(inpFileName,
                                                                 FileInOut.Input.ElementsToImport.Beam, false);
            if (_controller.IsExplodedViewActive())
            {
                double[] offset;
                if (part is CompoundGeometryPart compoundPart)
                    offset = _controller.Model.Geometry.Parts[compoundPart.SubPartNames[0]].Offset;
                else offset = part.Offset;
                Dictionary<string, double[]> partOffsets = new Dictionary<string, double[]>();
                foreach (var entry in mesh.Parts) partOffsets.Add(entry.Key, offset);
                mesh.ApplyExplodedView(partOffsets);
            }
            double[][] nodeCoor = mesh.GetAllNodeCoor();
            _controller.HighlightNodes(nodeCoor);
            _controller.Form.WriteDataToOutput("QuadWild Gmsh edge-node preview: " + nodeCoor.Length + " nodes.");
            return true;
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
            string executable = Application.StartupPath + Globals.GmshCaller;
            string geometryFileName = Path.Combine(workDirectory,
                                                   part.IsCADPart ? Globals.BrepFileName : Globals.StlFileName);
            string inpFileName = Path.Combine(workDirectory, Globals.QuadWildGmshInpFileName);
            string gmshDataFileName = Path.Combine(workDirectory, Globals.QuadWildGmshDataFileName);
            //
            if (File.Exists(geometryFileName)) File.Delete(geometryFileName);
            if (File.Exists(inpFileName)) File.Delete(inpFileName);
            if (File.Exists(gmshDataFileName)) File.Delete(gmshDataFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            GmshData gmshData = new GmshData();
            try
            {
                if (part.IsCADPart) File.WriteAllText(geometryFileName, part.CADFileData);
                else StlFileWriter.Write(geometryFileName, _controller.Model.Geometry,
                                         new string[] { part.Name });
                // QuadWild requires a purely linear triangular input mesh. Keep all existing size controls.
                MeshingParameters partMeshingParameters = _controller.GetPartMeshingParameters(part.Name, meshSetupItems);
                partMeshingParameters.SecondOrder = false;
                partMeshingParameters.QuadDominated = false;
                ShellGmsh shellGmsh = new ShellGmsh("QuadWild_triangular_pre-pass");
                shellGmsh.AlgorithmRecombine = GmshAlgorithmRecombineEnum.None;
                //
                gmshData.GeometryFileName = geometryFileName;
                gmshData.InpFileName = inpFileName;
                gmshData.PartMeshingParameters = partMeshingParameters;
                gmshData.GmshSetupItems = new MeshSetupItem[] { shellGmsh };
                gmshData.Preview = false;
                if (!part.IsCADPart)
                    gmshData.StlFeatureAngleDeg = _controller.Settings.General.EdgeAngle;
                _controller.Model.Geometry.GetPartTopologyForGmsh(part.Name, ref gmshData);
                _controller.GetMeshItemSizes(part.Name, ref gmshData, meshSetupItems);
                gmshData.WriteToFile(gmshDataFileName);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            string argument = Globals.QuadWildGmshDataFileName + " " + CaeMesh.GmshCommandEnum.Mesh;
            string error = null;
            bool jobCompleted;
            if (Debugger.IsAttached)
            {
                GmshAPI gmsh = new GmshAPI(gmshData, _controller.Form.WriteDataToOutput);
                error = gmsh.CreateMesh();
                jobCompleted = error == null;
            }
            else
            {
                JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                     _controller.Form.WriteDataToOutput);
                jobCompleted = jobStatus == JobStatus.OK;
            }
            //
            if (!jobCompleted)
            {
                string message = "The triangular QuadWild input mesh could not be created.";
                if (error != null) message += Environment.NewLine + error;
                throw new CaeException(message);
            }
            // Importing with the geometry preserves the Gmsh curve groups as model-edge topology.
            FeMesh mesh = _controller.Model.PrepareGeneratedMeshFromMeshFile(inpFileName, part,
                                                                              false, false, false);
            if (mesh.Parts.Count != 1 || !mesh.Parts.ContainsKey(part.Name))
                throw new CaeException("The triangular QuadWild pre-pass produced an unexpected mesh part layout.");
            return mesh;
        }
        public GeneratedMeshData CreateMeshFromStl(GeometryPart part, MeshSetupItem[] meshSetupItems,
                                                        string workDirectory)
        {
            _controller.Form.WriteDataToOutput("");
            //
            string executable = Application.StartupPath + Globals.GmshCaller;
            string stlFileName = Path.Combine(workDirectory, Globals.StlFileName);
            string inpFileName = Path.Combine(workDirectory, Globals.InpMeshFileName);
            string gmshDataFileName = Path.Combine(workDirectory, Globals.GmshDataFileName);
            //
            if (File.Exists(stlFileName)) File.Delete(stlFileName);
            if (File.Exists(inpFileName)) File.Delete(inpFileName);
            if (File.Exists(gmshDataFileName)) File.Delete(gmshDataFileName);
            //
            string[] partNames = new string[] { part.Name };
            _controller.SuppressExplodedView(partNames);
            GmshData gmshData = new GmshData();
            try
            {
                StlFileWriter.Write(stlFileName, _controller.Model.Geometry, partNames);
                MeshingParameters partMeshingParameters = _controller.GetPartMeshingParameters(part.Name);
                gmshData.GeometryFileName = stlFileName;
                gmshData.InpFileName = inpFileName;
                gmshData.PartMeshingParameters = partMeshingParameters;
                gmshData.GmshSetupItems = meshSetupItems;
                gmshData.Preview = false;
                gmshData.StlFeatureAngleDeg = _controller.Settings.General.EdgeAngle;
                _controller.Model.Geometry.GetPartTopologyForGmsh(part.Name, ref gmshData);
                _controller.GetMeshItemSizes(part.Name, ref gmshData);
                gmshData.WriteToFile(gmshDataFileName);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            string argument = Globals.GmshDataFileName + " " + CaeMesh.GmshCommandEnum.Mesh;
            //
            bool jobCompleted;
            if (Debugger.IsAttached)
            {
                CaeMesh.GmshAPI gmshAPI = new GmshAPI(gmshData, _controller.Form.WriteDataToOutput);
                string error = gmshAPI.CreateMesh();
                jobCompleted = error == null;
            }
            else
            {
                JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                     _controller.Form.WriteDataToOutput);
                // Job completed
                jobCompleted = jobStatus == JobStatus.OK;
            }
            //
            if (jobCompleted)
            {
                return new GeneratedMeshData()
                {
                    GeometryPart = part,
                    MeshFileName = inpFileName,
                    SmoothShaded = true
                };
            }
            else return null;
        }
        public GeneratedMeshData CreateMeshFromBrep(GeometryPart part, MeshSetupItem[] meshSetupItems,
                                                         string workDirectory)
        {
            _controller.Form.WriteDataToOutput("");
            //
            string executable = Application.StartupPath + Globals.GmshCaller;
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            string inpFileName = Path.Combine(workDirectory, Globals.InpMeshFileName);
            string gmshDataFileName = Path.Combine(workDirectory, Globals.GmshDataFileName);
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            if (File.Exists(inpFileName)) File.Delete(inpFileName);
            if (File.Exists(gmshDataFileName)) File.Delete(gmshDataFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            GmshData gmshData = new GmshData();
            try
            {
                File.WriteAllText(brepFileName, part.CADFileData);
                MeshingParameters partMeshingParameters = _controller.GetPartMeshingParameters(part.Name);
                gmshData.GeometryFileName = brepFileName;
                gmshData.InpFileName = inpFileName;
                gmshData.PartMeshingParameters = partMeshingParameters;
                gmshData.GmshSetupItems = GetSetupItemsForPart(part, meshSetupItems);
                gmshData.Preview = false;
                _controller.Model.Geometry.GetPartTopologyForGmsh(part.Name, ref gmshData);
                _controller.GetMeshItemSizes(part.Name, ref gmshData);
                gmshData.WriteToFile(gmshDataFileName);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            string argument = Globals.GmshDataFileName + " " + CaeMesh.GmshCommandEnum.Mesh;
            //
            string error = null;
            bool jobCompleted;
            if (Debugger.IsAttached)
            {
                GmshAPI gmsh = new GmshAPI(gmshData, _controller.Form.WriteDataToOutput);
                error = gmsh.CreateMesh();
                jobCompleted = (error == null);
            }
            else
            {
                JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                     _controller.Form.WriteDataToOutput);
                // Job completed
                jobCompleted = jobStatus == JobStatus.OK;
            }
            //
            if (jobCompleted)
            {
                return new GeneratedMeshData()
                {
                    GeometryPart = part,
                    MeshFileName = inpFileName,
                    SmoothShaded = true
                };
            }
            else
            {
                string message = "Mesh generation failed.";
                if (error != null) message += Environment.NewLine + error;
                throw new CaeException(message);
            }
        }
        public Dictionary<int, List<Vec3D>> GeNormalsFromGeometryAtMeshNodes(GeometryPart part, FeMesh mesh)
        {
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return null;
            }
            //
            string executable = Application.StartupPath + Globals.GmshCaller;
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            File.WriteAllText(brepFileName, part.CADFileData);
            //
            List<FeNode> nodes;
            HashSet<int> nodeIds;
            BasePart meshPart = mesh.Parts[part.Name];
            Dictionary<int, FeNode[]> faceIdNodes = new Dictionary<int, FeNode[]>();
            for (int i = 0; i < meshPart.Visualization.FaceCount; i++)
            {
                nodes = new List<FeNode>();
                nodeIds = meshPart.Visualization.GetNodeIdsForSurfaceId(i);
                foreach (var nodeId in nodeIds) nodes.Add(mesh.Nodes[nodeId]);
                //
                faceIdNodes.Add(FeMesh.GmshTopologyId(i, part.PartId), nodes.ToArray());
            }
            //
            GmshData gmshData = new GmshData();
            gmshData.GeometryFileName = brepFileName;
            gmshData.FaceIdNodes = faceIdNodes;
            _controller.Model.Geometry.GetPartTopologyForGmsh(part.Name, ref gmshData);
            //
            _controller.ResumeExplodedViews(false);
            //
            GmshAPI gmsh = new GmshAPI(gmshData, _controller.Form.WriteDataToOutput);
            string error = gmsh.GetOccNormals();
            if (error != null)
                throw new CaeException("The geometry normals of the part " + part.Name +
                                       " could not be computed. " + error);
            return gmsh.GmshData.NodeIdNormals;
        }
    }
}
