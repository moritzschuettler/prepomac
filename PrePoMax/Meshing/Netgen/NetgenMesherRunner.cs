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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public string Defeature(GeometryPart part, int[] surfaceIds)
        {
            _controller.Form.WriteDataToOutput("");
            string workDirectory = _controller.Settings.GetWorkDirectory();
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return null;
            }
            //
            string executable = Application.StartupPath + Globals.NetGenMesher;
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            _controller.SuppressExplodedView(new string[] { part.Name });
            try
            {
                File.WriteAllText(brepFileName, part.CADFileData);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            //
            int[] sortedSurfaceIds = (int[])surfaceIds.Clone();
            Array.Sort(sortedSurfaceIds);
            string argument = "BREP_DEFEATURE " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              string.Join(" ", sortedSurfaceIds);
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                           _controller.Form.WriteDataToOutput);
            if (jobStatus != JobStatus.OK)
                throw new CaeException("Defeaturing failed for part '" + part.Name + "'.");
            return brepFileName;
        }
        // The faces arrive grouped by the solid they are numbered inside, as a shell split names them: one group
        // of solid zero for a part of its own and one group per picked sub part for a compound
        public string FuseFaces(GeometryPart part, List<int> solidNumbers, List<int[]> kernelFaceIds)
        {
            string brepFileName = WritePartBrep(part);
            if (brepFileName == null) return null;
            //
            StringBuilder faceIdsArgument = new StringBuilder(solidNumbers.Count.ToString());
            for (int i = 0; i < solidNumbers.Count; i++)
            {
                faceIdsArgument.Append(" " + solidNumbers[i] + " " + kernelFaceIds[i].Length);
                foreach (int kernelFaceId in kernelFaceIds[i]) faceIdsArgument.Append(" " + kernelFaceId);
            }
            string argument = "BREP_FUSE_FACES " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              faceIdsArgument.ToString();
            return RunGeometryOperation(part, argument, brepFileName);
        }
        // A preview writes what the repair would take away in place of the result: every edge as a polyline and
        // every vertex as a point
        public string Repair(GeometryPart part, int kernelOptions, double tolerance, bool preview)
        {
            string brepFileName = WritePartBrep(part);
            if (brepFileName == null) return null;
            //
            string outputFileName = preview ? brepFileName + ".preview" : brepFileName;
            if (preview && File.Exists(outputFileName)) File.Delete(outputFileName);
            string argument = (preview ? "BREP_REPAIR_PREVIEW " : "BREP_REPAIR ") +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              "\"" + outputFileName.ToUTF8() + "\" " +
                              kernelOptions + " " + tolerance.ToString("R", CultureInfo.InvariantCulture);
            // A repaired part has its closed faces divided again only while an older command is regenerated, so the
            // preview compares it divided the same way
            if (preview) argument += " " + _controller.NumOfAutomaticSplitFaces;
            return RunGeometryOperation(part, argument, outputFileName);
        }
        // Every closed face of the whole part is divided, so a face two solids of a compound share stays shared
        public string SplitPeriodicFaces(GeometryPart part, int numOfFaces)
        {
            string brepFileName = WritePartBrep(part);
            if (brepFileName == null) return null;
            //
            string argument = "BREP_SPLIT_CLOSED_FACES " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              numOfFaces;
            return RunGeometryOperation(part, argument, brepFileName);
        }
        private string WritePartBrep(GeometryPart part)
        {
            string workDirectory = _controller.Settings.GetWorkDirectory();
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return null;
            }
            //
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            _controller.SuppressExplodedView(new string[] { part.Name });
            try
            {
                File.WriteAllText(brepFileName, part.CADFileData);
            }
            finally
            {
                _controller.ResumeExplodedViews(false);
            }
            return brepFileName;
        }
        // The modelling kernel says why it refused in the output, so a refusal is answered as no file
        private string RunGeometryOperation(GeometryPart part, string argument, string outputFileName)
        {
            string executable = Application.StartupPath + Globals.NetGenMesher;
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument,
                                                           Path.GetDirectoryName(outputFileName),
                                                           _controller.Form.WriteDataToOutput);
            return jobStatus == JobStatus.OK ? outputFileName : null;
        }
        // A move returns the one transformed file, and a pattern returns one file per copy, copy k transformed k times
        public string[] Transform(GeometryPart part, GeometryTransform transform)
        {
            string workDirectory = _controller.Settings.GetWorkDirectory();
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return null;
            }
            //
            string executable = Application.StartupPath + Globals.NetGenMesher;
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            string transformFileName = Path.Combine(workDirectory, Globals.TransformFileName);
            string[] outputFileNames = new string[Math.Max(transform.NumberOfCopies, 1)];
            if (transform.NumberOfCopies == 0) outputFileNames[0] = brepFileName;
            else
            {
                string baseName = Path.Combine(workDirectory, Path.GetFileNameWithoutExtension(brepFileName));
                string extension = Path.GetExtension(brepFileName);
                for (int i = 0; i < outputFileNames.Length; i++)
                    outputFileNames[i] = baseName + "_" + (i + 1).ToString("0000") + extension;
            }
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            if (File.Exists(transformFileName)) File.Delete(transformFileName);
            foreach (var outputFileName in outputFileNames)
            {
                if (File.Exists(outputFileName)) File.Delete(outputFileName);
            }
            //
            File.WriteAllText(brepFileName, part.CADFileData);
            File.WriteAllText(transformFileName, transform.GetDefinition());
            //
            string argument = "BREP_TRANSFORM " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              "\"" + brepFileName.ToUTF8() + "\" " +
                              "\"" + transformFileName.ToUTF8() + "\"";
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory,
                                                           _controller.Form.WriteDataToOutput);
            if (jobStatus != JobStatus.OK)
                throw new CaeException("The " + transform.Type.ToString().ToLower() + " transform failed for part '" +
                                       part.Name + "'.");
            foreach (var outputFileName in outputFileNames)
            {
                if (!File.Exists(outputFileName))
                    throw new CaeException("Netgen reported success but did not write the transformed geometry of part '" +
                                           part.Name + "'.");
            }
            return outputFileNames;
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
            meshingParameters.WriteToFile(meshParametersFileName, part.BoundingBox.GetDiagonal(),
                                          _controller.Settings.Meshing.ParallelMeshingThreads);
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
                                            OrderedDictionary<string, MeshSetupItem> meshSetupItems,
                                            bool previewSweepSourceMesh)
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
            string transfiniteFileName = WriteTransfiniteFile(part, meshingParameters, workDirectory, meshSetupItems);
            meshingParameters.WriteToFile(meshParametersFileName, part.BoundingBox.GetDiagonal(),
                                          _controller.Settings.Meshing.ParallelMeshingThreads, transfiniteFileName);
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
        /// <summary>
        /// The file of the structured faces of a CAD part, by the one definition all meshers follow, and of the numbers of
        /// elements the user set on its edges, which Netgen divides those edges into whatever the element size; null
        /// when Netgen lays no structured grid and no number is set.
        /// </summary>
        private string WriteTransfiniteFile(GeometryPart part, MeshingParameters meshingParameters, string workDirectory,
                                            OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            string fileName = Path.Combine(workDirectory, Globals.TransfiniteFacesFileName);
            if (File.Exists(fileName)) File.Delete(fileName);
            if (!part.IsCADPart) return null;
            // The solids of a compound in the order its brep holds them
            BasePart[] parts;
            if (part is CompoundGeometryPart compoundPart)
                parts = compoundPart.SubPartNames.Select(name => _controller.Model.Geometry.Parts[name]).ToArray();
            else parts = new BasePart[] { part };
            //
            GmshData sizes = new GmshData();
            _controller.GetMeshItemSizes(part.Name, ref sizes, meshSetupItems);
            Dictionary<int, int> pinnedEdgeCounts = new Dictionary<int, int>();
            Dictionary<int, int> sizedEdgeCounts = new Dictionary<int, int>();
            if (sizes.EdgeIdNumElements != null)
            {
                foreach (var entry in sizes.EdgeIdNumElements)
                {
                    // A number an element size gives may move within the range that keeps the element size
                    if (sizes.FixedCountEdgeIds != null && !sizes.FixedCountEdgeIds.Contains(entry.Key))
                        sizedEdgeCounts[entry.Key] = entry.Value;
                    else pinnedEdgeCounts[entry.Key] = entry.Value;
                }
            }
            bool transfinite = meshingParameters.IsTransfiniteThreeSided || meshingParameters.TransfiniteFourSided;
            if (!transfinite && pinnedEdgeCounts.Count == 0) return null;
            //
            if (!NetgenTransfiniteFile.Write(fileName, _controller.Model.Geometry, parts,
                                             meshingParameters.TransfiniteThreeSided,
                                             meshingParameters.TransfiniteFourSided,
                                             meshingParameters.TransfiniteAngleDeg, pinnedEdgeCounts, sizedEdgeCounts))
            {
                if (pinnedEdgeCounts.Count > 0)
                    throw new CaeException("The edges of the compound part " + part.Name + " cannot be matched to the " +
                                           "solids Netgen meshes, so Netgen cannot keep the numbers of elements set on " +
                                           "them. Mesh the part by another mesher.");
                _controller.Form.WriteDataToOutput("The surfaces of the compound part " + part.Name + " cannot be " +
                                                   "matched to the solids Netgen meshes, so Netgen meshes them " +
                                                   "without structured faces.");
                return null;
            }
            return fileName;
        }
        public bool PreviewEdgeNodesForQuadWild(GeometryPart part,
                                                OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            MeshSetupItem[] setupItems = new MeshSetupItem[0];
            if (part.IsCADPart) return PreviewEdgeMeshFromBrep(part, setupItems, meshSetupItems, false);
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
                meshingParameters.WriteToFile(meshParametersFileName, part.BoundingBox.GetDiagonal(),
                                              _controller.Settings.Meshing.ParallelMeshingThreads);
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
                _controller.GetPartMeshingParameters(part.Name).WriteToFile(
                    meshParametersFileName, part.BoundingBox.GetDiagonal(),
                    _controller.Settings.Meshing.ParallelMeshingThreads);
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
                string transfiniteFileName = WriteTransfiniteFile(part, meshingParameters, workDirectory, null);
                meshingParameters.WriteToFile(meshParametersFileName, part.BoundingBox.GetDiagonal(),
                                              _controller.Settings.Meshing.ParallelMeshingThreads, transfiniteFileName);
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
            StringBuilder output = new StringBuilder();
            JobStatus jobStatus = _controller.JobRunner.Run(part.Name, executable, argument, workDirectory, data =>
            {
                lock (output) output.Append(data);
                _controller.Form.WriteDataToOutput(data);
            });
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
            else
            {
                string text;
                lock (output) text = output.ToString();
                throw new CaeException(GetMeshGenerationFailedMessage(part, text));
            }
        }
        /// <summary>
        /// Netgen lists the faces it could not mesh after its surface meshing, each with the reason, and the reason
        /// given for a structured face names the surface. The faces of a part of its own are numbered as its surfaces;
        /// those of a compound are numbered across its solids.
        /// </summary>
        private static string GetMeshGenerationFailedMessage(GeometryPart part, string output)
        {
            List<string> reasons = new List<string>();
            bool faceList = false;
            foreach (var line in output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("SURFACE MESHING ERROR OCCURRED IN"))
                {
                    faceList = true;
                    continue;
                }
                if (!faceList) continue;
                //
                Match match = Regex.Match(line, @"^Face (\d+)(?:: (.*))?$");
                if (!match.Success)
                {
                    faceList = false;
                    continue;
                }
                string face = part is CompoundGeometryPart ? "Face " + match.Groups[1].Value :
                                                             "Surface " + match.Groups[1].Value + " of " + part.Name;
                string reason = match.Groups[2].Success ? match.Groups[2].Value : null;
                if (reason == null) reason = face + " could not be meshed.";
                else if (reason.IndexOf("surface ", StringComparison.OrdinalIgnoreCase) < 0) reason = face + ": " + reason;
                if (!reasons.Contains(reason)) reasons.Add(reason);
            }
            //
            string message = "Mesh generation failed.";
            foreach (var reason in reasons) message += Environment.NewLine + reason;
            return message;
        }
    }
}
