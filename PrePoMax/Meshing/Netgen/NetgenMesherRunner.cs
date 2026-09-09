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
    internal class NetgenMesherRunner : IMesherRunner
    {
        // Variables                                                                                                                
        private Controller _controller;


        // Properties                                                                                                               
        public MesherTypeEnum MesherType { get { return MesherTypeEnum.Netgen; } }


        // Constructors                                                                                                             
        public NetgenMesherRunner(Controller controller)
        {
            _controller = controller;
        }


        // Methods                                                                                                                  
        public bool CanCreateMeshFromBrep(GeometryPart part)
        {
            return true;
        }
        public bool CanCreateMeshFromStl(GeometryPart part)
        {
            return part.PartType == PartType.Solid || part.PartType == PartType.SolidAsShell ||
                   part.PartType == PartType.Shell;
        }
        public bool PreviewEdgeMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems,
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
            string executable = Application.StartupPath + Globals.NetGenMesher;
            string stlFileName = Path.Combine(workDirectory, Globals.StlFileName);
            string volFileName = Path.Combine(workDirectory, Globals.VolFileName);
            string meshParametersFileName = Path.Combine(workDirectory, Globals.MeshParametersFileName);
            string localMeshSizesFileName = Path.Combine(workDirectory, Globals.LocalMeshSizesFileName);
            string edgeNodesFileName = Path.Combine(workDirectory, Globals.EdgeNodesFileName);
            //
            if (File.Exists(stlFileName)) File.Delete(stlFileName);
            if (File.Exists(volFileName)) File.Delete(volFileName);
            if (File.Exists(meshParametersFileName)) File.Delete(meshParametersFileName);
            if (File.Exists(localMeshSizesFileName)) File.Delete(localMeshSizesFileName);
            if (File.Exists(edgeNodesFileName)) File.Delete(edgeNodesFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            FileInOut.Output.StlFileWriter.Write(stlFileName, _controller.Model.Geometry, new string[] { part.Name });
            _controller.CreateLocalMeshSizeFile(part, localMeshSizesFileName, meshSetupItems);
            MeshingParameters meshingParameters = _controller.GetPartMeshingParameters(part.Name, meshSetupItems);
            meshingParameters.WriteToFile(meshParametersFileName, part.BoundingBox.GetDiagonal());
            _controller.Model.Geometry.WriteEdgeNodesToFile(part, edgeNodesFileName);
            _controller.ResumeExplodedViews(false);
            //
            string argument = "STL_EDGE_MESH " +
                              "\"" + stlFileName + "\" " +
                              "\"" + volFileName + "\" " +
                              "\"" + meshParametersFileName + "\" " +
                              "\"" + localMeshSizesFileName + "\" " +
                              "\"" + edgeNodesFileName + "\"";

            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                 _controller.Form.WriteDataToOutput);
            // Job completed
            if (jobStatus == JobStatus.OK)
            {
                if (!File.Exists(volFileName))
                    throw new CaeException("Netgen reported success but did not create the STL edge mesh output.");
                _controller.ImportGeneratedNodeMesh(volFileName, part, false);
                return true;
            }
            else throw new CaeException("Netgen could not create the STL edge mesh for part '" + part.Name + "'.");
        }
        public bool PreviewEdgeMeshFromBrep(GeometryPart part, MeshSetupItem[] mesherSetupItems,
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
            string executable = Application.StartupPath + Globals.NetGenMesher;
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            string volFileName = Path.Combine(workDirectory, Globals.VolFileName);
            string meshParametersFileName = Path.Combine(workDirectory, Globals.MeshParametersFileName);
            string localMeshSizesFileName = Path.Combine(workDirectory, Globals.LocalMeshSizesFileName);
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            if (File.Exists(volFileName)) File.Delete(volFileName);
            if (File.Exists(meshParametersFileName)) File.Delete(meshParametersFileName);
            if (File.Exists(localMeshSizesFileName)) File.Delete(localMeshSizesFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            File.WriteAllText(brepFileName, part.CADFileData);
            _controller.CreateLocalMeshSizeFile(part, localMeshSizesFileName, meshSetupItems);
            MeshingParameters meshingParameters = _controller.GetPartMeshingParameters(part.Name, meshSetupItems);
            meshingParameters.WriteToFile(meshParametersFileName, part.BoundingBox.GetDiagonal());
            _controller.ResumeExplodedViews(false);
            //
            string argument = "BREP_EDGE_MESH " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              "\"" + volFileName + "\" " +
                              "\"" + meshParametersFileName + "\" " +
                              "\"" + localMeshSizesFileName + "\"";
            //
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                 _controller.Form.WriteDataToOutput);
            // Job completed
            if (jobStatus == JobStatus.OK)
            {
                _controller.ImportGeneratedNodeMesh(volFileName, part, false);
                return true;
            }
            else return false;
        }
        public bool PreviewEdgeNodesForQuadWild(GeometryPart part,
                                                OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            MeshSetupItem[] setupItems = new MeshSetupItem[0];
            if (part.IsCADPart) return PreviewEdgeMeshFromBrep(part, setupItems, meshSetupItems);
            else return PreviewEdgeMeshFromStl(part, setupItems, meshSetupItems);
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
            string executable = Application.StartupPath + Globals.NetGenMesher;
            string geometryFileName = Path.Combine(workDirectory,
                                                   part.IsCADPart ? Globals.BrepFileName : Globals.StlFileName);
            string volFileName = Path.Combine(workDirectory, Globals.VolFileName);
            string meshParametersFileName = Path.Combine(workDirectory, Globals.MeshParametersFileName);
            string localMeshSizesFileName = Path.Combine(workDirectory, Globals.LocalMeshSizesFileName);
            string edgeNodesFileName = Path.Combine(workDirectory, Globals.EdgeNodesFileName);
            //
            if (File.Exists(geometryFileName)) File.Delete(geometryFileName);
            if (File.Exists(volFileName)) File.Delete(volFileName);
            if (File.Exists(meshParametersFileName)) File.Delete(meshParametersFileName);
            if (File.Exists(localMeshSizesFileName)) File.Delete(localMeshSizesFileName);
            if (File.Exists(edgeNodesFileName)) File.Delete(edgeNodesFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            try
            {
                if (part.IsCADPart) File.WriteAllText(geometryFileName, part.CADFileData);
                else FileInOut.Output.StlFileWriter.Write(geometryFileName, _controller.Model.Geometry,
                                                          new string[] { part.Name });
                _controller.CreateLocalMeshSizeFile(part, localMeshSizesFileName, meshSetupItems);
                MeshingParameters meshingParameters = _controller.GetPartMeshingParameters(part.Name, meshSetupItems);
                meshingParameters.SecondOrder = false;
                meshingParameters.QuadDominated = false;
                meshingParameters.WriteToFile(meshParametersFileName, part.BoundingBox.GetDiagonal());
                if (!part.IsCADPart) _controller.Model.Geometry.WriteEdgeNodesToFile(part, edgeNodesFileName);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            string argument;
            if (part.IsCADPart)
            {
                argument = "BREP_MESH " +
                           "\"" + geometryFileName.ToUTF8() + "\" " +
                           "\"" + volFileName + "\" " +
                           "\"" + meshParametersFileName + "\" " +
                           "\"" + localMeshSizesFileName + "\"";
            }
            else
            {
                argument = "STL_SURFACE_MESH " +
                           "\"" + geometryFileName + "\" " +
                           "\"" + volFileName + "\" " +
                           "\"" + meshParametersFileName + "\" " +
                           "\"" + localMeshSizesFileName + "\" " +
                           "\"" + edgeNodesFileName + "\"";
            }
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                             _controller.Form.WriteDataToOutput);
            if (jobStatus != JobStatus.OK)
                throw new CaeException("Netgen could not create the triangular QuadWild input mesh.");
            if (!File.Exists(volFileName))
                throw new CaeException("Netgen reported success but did not create the QuadWild input mesh.");
            //
            FeMesh mesh = _controller.Model.PrepareGeneratedMeshFromMeshFile(volFileName, part,
                                                                              false, false, false);
            if (mesh.Parts.Count != 1 || !mesh.Parts.ContainsKey(part.Name))
                throw new CaeException("The Netgen QuadWild pre-pass produced an unexpected mesh part layout.");
            return mesh;
        }
        public GeneratedMeshData CreateMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems, string workDirectory)
        {
            string executable = Application.StartupPath + Globals.NetGenMesher;
            string stlFileName = Path.Combine(workDirectory, Globals.StlFileName);
            string volFileName = Path.Combine(workDirectory, Globals.VolFileName);
            string meshParametersFileName = Path.Combine(workDirectory, Globals.MeshParametersFileName);
            string localMeshSizesFileName = Path.Combine(workDirectory, Globals.LocalMeshSizesFileName);
            string edgeNodesFileName = Path.Combine(workDirectory, Globals.EdgeNodesFileName);
            //
            if (File.Exists(stlFileName)) File.Delete(stlFileName);
            if (File.Exists(volFileName)) File.Delete(volFileName);
            if (File.Exists(meshParametersFileName)) File.Delete(meshParametersFileName);
            if (File.Exists(localMeshSizesFileName)) File.Delete(localMeshSizesFileName);
            if (File.Exists(edgeNodesFileName)) File.Delete(edgeNodesFileName);
            //
            string[] partNames = new string[] { part.Name };
            _controller.SuppressExplodedView(partNames);
            try
            {
                FileInOut.Output.StlFileWriter.Write(stlFileName, _controller.Model.Geometry, partNames);
                _controller.CreateLocalMeshSizeFile(part, localMeshSizesFileName, null);
                _controller.GetPartMeshingParameters(part.Name).WriteToFile(meshParametersFileName,
                                                                            part.BoundingBox.GetDiagonal());
                _controller.Model.Geometry.WriteEdgeNodesToFile(part, edgeNodesFileName);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            string command = part.PartType == PartType.Shell ? "STL_SURFACE_MESH" : "STL_MESH";
            string argument = command + " " +
                              "\"" + stlFileName + "\" " +
                              "\"" + volFileName + "\" " +
                              "\"" + meshParametersFileName + "\" " +
                              "\"" + localMeshSizesFileName + "\" " +
                              "\"" + edgeNodesFileName + "\"";
            //
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                 _controller.Form.WriteDataToOutput);
            // Job completed
            if (jobStatus == JobStatus.OK)
            {
                if (!File.Exists(volFileName))
                    throw new CaeException("Netgen reported success but did not create the mesh output for part '" +
                                           part.Name + "'.");
                return new GeneratedMeshData()
                {
                    GeometryPart = part,
                    MeshFileName = volFileName,
                    SmoothShaded = false
                };
            }
            else
            {
                string description = command == "STL_SURFACE_MESH" ? "surface" : "volume";
                throw new CaeException("Netgen could not create the STL " + description + " mesh for part '" +
                                       part.Name + "'.");
            }
        }
        public GeneratedMeshData CreateMeshFromBrep(GeometryPart part, MeshSetupItem[] mesherSetupItems, string workDirectory)
        {
            string executable = Application.StartupPath + Globals.NetGenMesher;
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            string volFileName = Path.Combine(workDirectory, Globals.VolFileName);
            string meshParametersFileName = Path.Combine(workDirectory, Globals.MeshParametersFileName);
            string localMeshSizesFileName = Path.Combine(workDirectory, Globals.LocalMeshSizesFileName);
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            if (File.Exists(volFileName)) File.Delete(volFileName);
            if (File.Exists(meshParametersFileName)) File.Delete(meshParametersFileName);
            if (File.Exists(localMeshSizesFileName)) File.Delete(localMeshSizesFileName);
            //
            _controller.SuppressExplodedView(new string[] { part.Name });
            MeshingParameters meshingParameters;
            try
            {
                File.WriteAllText(brepFileName, part.CADFileData);
                meshingParameters = _controller.GetPartMeshingParameters(part.Name);
                meshingParameters.WriteToFile(meshParametersFileName, part.BoundingBox.GetDiagonal());
                _controller.CreateLocalMeshSizeFile(part, localMeshSizesFileName, null);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            string argument = "BREP_MESH " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              "\"" + volFileName + "\" " +
                              "\"" + meshParametersFileName + "\" " +
                              "\"" + localMeshSizesFileName + "\"";
            //
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                 _controller.Form.WriteDataToOutput);
            // Job completed
            if (jobStatus == JobStatus.OK)
            {
                return new GeneratedMeshData()
                {
                    GeometryPart = part,
                    MeshFileName = volFileName,
                    SmoothShaded = true
                };
            }
            else throw new CaeException("Mesh generation failed.");
        }
    }
}
