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
            System.Diagnostics.PerformanceCounter ramCounter;
            ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
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
