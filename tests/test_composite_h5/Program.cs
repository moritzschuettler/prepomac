// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CaeMesh;
using CaeModel;
using PrePoMax;
using PrePoMax.Forms;

namespace TestCompositeCaeH5
{
    // Standalone test: reads a Composite CAE HDF5 file, imports it as a composite layup and writes a PMX
    // file from scratch. The model starts empty, so the finite element mesh stored in the HDF5 file is
    // used as the structural mesh.
    class Program
    {
        private static string _h5FileName;
        private static string _pmxFileName;
        private static int _exitCode;

        [STAThread]
        static int Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("Test: Composite CAE HDF5 import -> PMX");
            Console.WriteLine("==================================================");
            Console.WriteLine();
            //
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string outputDir = Path.Combine(baseDir, "output");
                Directory.CreateDirectory(outputDir);
                //
                _h5FileName = args.Length > 0 ? args[0] : GetDefaultH5FileName();
                _pmxFileName = args.Length > 1 ? args[1]
                                               : Path.Combine(outputDir, Path.GetFileNameWithoutExtension(_h5FileName) + ".pmx");
                //
                if (!File.Exists(_h5FileName)) throw new FileNotFoundException("HDF5 file not found: " + _h5FileName);
                //
                ReportFileContents();
                RunImportAndSavePmx();
                //
                if (_exitCode == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("----------Finished------------");
                    Console.WriteLine("PMX file written: " + _pmxFileName);
                }
                return _exitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("----------Error---------------");
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }
        private static string GetDefaultH5FileName()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, @"Downloads\200401_Rocker_PA_print_2NEW.hdf5");
        }
        // Step 1: read the file with the CaeModel reader only and report what it contains
        private static void ReportFileContents()
        {
            Console.WriteLine("Step 1: Reading the Composite CAE HDF5 file...");
            Console.WriteLine("  Source: " + _h5FileName);
            //
            CompositeCaeData data = CompositeCaeH5Reader.Read(_h5FileName);
            //
            Console.WriteLine("  Vendor        : " + (data.Vendor ?? "-"));
            Console.WriteLine("  Format version: " + (data.FormatVersion ?? "-"));
            Console.WriteLine("  Length unit   : " + (data.LengthUnit ?? "-"));
            Console.WriteLine("  Plies         : " + data.Plies.Count);
            Console.WriteLine("  Layers        : " + data.GetLayerCount());
            Console.WriteLine("  Triangles     : " + data.GetTriangleCount());
            Console.WriteLine("  Total thickness: " + data.GetTotalThickness().ToString("G6", CultureInfo.InvariantCulture));
            //
            BoundingBox box = data.GetBoundingBox();
            Console.WriteLine("  Bounding box  : [" + box.MinX.ToString("G6", CultureInfo.InvariantCulture) + ", " +
                              box.MinY.ToString("G6", CultureInfo.InvariantCulture) + ", " +
                              box.MinZ.ToString("G6", CultureInfo.InvariantCulture) + "] - [" +
                              box.MaxX.ToString("G6", CultureInfo.InvariantCulture) + ", " +
                              box.MaxY.ToString("G6", CultureInfo.InvariantCulture) + ", " +
                              box.MaxZ.ToString("G6", CultureInfo.InvariantCulture) + "]");
            //
            Console.WriteLine("  Materials     : " + data.Materials.Count);
            foreach (CompositeCaeMaterial material in data.Materials)
            {
                if (material.HasMechanicalData)
                {
                    Console.WriteLine("    " + material.Name + " (" + material.RawName + ", " +
                                      (material.Architecture ?? "-") + "): E1=" + material.E1 + " E2=" + material.E2 +
                                      " E3=" + material.E3 + " G12=" + material.G12 + " G13=" + material.G13 +
                                      " G23=" + material.G23 + " v12=" + material.Nu12 + " v13=" + material.Nu13 +
                                      " v23=" + material.Nu23);
                }
                else
                {
                    Console.WriteLine("    " + material.Name + " (" + material.RawName + "): no mechanical data");
                }
            }
            //
            foreach (string warning in data.Warnings) Console.WriteLine("  Warning: " + warning);
            //
            Console.WriteLine("  First plies:");
            foreach (CompositeCaePly ply in data.Plies.Take(3))
            {
                Console.WriteLine("    L" + ply.LayerId + " " + ply.RawName + " angle=" + ply.Orientation +
                                  " thickness=" + ply.Thickness + " triangles=" + ply.Surface.TriangleCount);
            }
            Console.WriteLine();
        }
        // Step 2: drive PrePoMax without its user interface to run the import and write the PMX file
        private static void RunImportAndSavePmx()
        {
            Console.WriteLine("Step 2: Importing the layup and writing the PMX file...");
            //
            CommandLineOptions options = new CommandLineOptions();
            options.ShowGui = "No";
            options.ExitAfterRegeneration = "Yes";
            //
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //
            using (FrmMain mainForm = new FrmMain(options))
            {
                mainForm.WindowState = FormWindowState.Minimized;
                mainForm.ShowInTaskbar = false;
                mainForm.Shown += (sender, e) => Run(mainForm);
                //
                Application.Run(mainForm);
            }
        }
        private static void Run(FrmMain mainForm)
        {
            try
            {
                Controller controller = mainForm.Controller;
                // An empty model without a mesh; the mesh of the HDF5 file has to be used instead
                controller.New();
                Console.WriteLine("  Model elements before import: " + controller.Model.Mesh.Elements.Count);
                //
                string layupName = "Layup-" + Path.GetFileNameWithoutExtension(_h5FileName);
                controller.ImportCompositeLayupFromH5Command(_h5FileName, layupName, null, 0, 1E-6);
                //
                Console.WriteLine();
                FeModel model = controller.Model;
                Console.WriteLine("  Nodes         : " + model.Mesh.Nodes.Count);
                Console.WriteLine("  Elements      : " + model.Mesh.Elements.Count);
                Console.WriteLine("  Parts         : " + model.Mesh.Parts.Count);
                Console.WriteLine("  Element sets  : " + model.Mesh.ElementSets.Count);
                Console.WriteLine("  Materials     : " + model.Materials.Count);
                Console.WriteLine("  Layups        : " + model.CompositeLayups.Count);
                //
                foreach (CompositeLayup layup in model.CompositeLayups.Values)
                {
                    Console.WriteLine("  Layup '" + layup.Name + "': " + layup.Plies.Count + " plies");
                    foreach (Ply ply in layup.Plies.Take(3))
                    {
                        int count = model.Mesh.ElementSets.ContainsKey(ply.Region)
                                  ? model.Mesh.ElementSets[ply.Region].Labels.Length : 0;
                        Console.WriteLine("    " + ply.Name + ": region=" + ply.Region + " (" + count + " elements)" +
                                          " material=" + ply.Material + " thickness=" + ply.Thickness +
                                          " angle=" + ply.RotationAngle);
                    }
                    if (layup.Plies.Count > 3) Console.WriteLine("    ...");
                }
                //
                CheckResults(model);
                //
                Directory.CreateDirectory(Path.GetDirectoryName(_pmxFileName));
                controller.SaveToPmx(_pmxFileName);
                //
                if (!File.Exists(_pmxFileName)) throw new Exception("The PMX file was not written.");
                Console.WriteLine();
                Console.WriteLine("  PMX size      : " + new FileInfo(_pmxFileName).Length + " bytes");
                //
                ReopenPmx(controller);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("----------Error---------------");
                Console.WriteLine(ex.ToString());
                _exitCode = 1;
            }
            finally
            {
                mainForm.Close();
            }
        }
        // Step 3: read the file back to make sure the layup survives the binary serialization
        private static void ReopenPmx(Controller controller)
        {
            Console.WriteLine();
            Console.WriteLine("Step 3: Reopening the PMX file...");
            //
            controller.Open(_pmxFileName);
            //
            FeModel model = controller.Model;
            Console.WriteLine("  Elements      : " + model.Mesh.Elements.Count);
            Console.WriteLine("  Element sets  : " + model.Mesh.ElementSets.Count);
            Console.WriteLine("  Materials     : " + model.Materials.Count);
            Console.WriteLine("  Layups        : " + model.CompositeLayups.Count);
            foreach (CompositeLayup layup in model.CompositeLayups.Values)
                Console.WriteLine("  Layup '" + layup.Name + "': " + layup.Plies.Count + " plies");
            //
            CheckResults(model);
            //
            ImportIntoExistingMesh(controller);
        }
        // Step 4: the model now has a mesh, so the mesh of the file must not be used a second time
        private static void ImportIntoExistingMesh(Controller controller)
        {
            Console.WriteLine();
            Console.WriteLine("Step 4: Importing a second layup into the existing mesh...");
            //
            int elementCountBefore = controller.Model.Mesh.Elements.Count;
            int partCountBefore = controller.Model.Mesh.Parts.Count;
            //
            controller.ImportCompositeLayupFromH5Command(_h5FileName, "Layup-Second", null, 0, 1E-6);
            //
            FeModel model = controller.Model;
            Console.WriteLine("  Elements      : " + model.Mesh.Elements.Count + " (was " + elementCountBefore + ")");
            Console.WriteLine("  Parts         : " + model.Mesh.Parts.Count + " (was " + partCountBefore + ")");
            Console.WriteLine("  Materials     : " + model.Materials.Count);
            Console.WriteLine("  Layups        : " + model.CompositeLayups.Count);
            //
            if (model.Mesh.Elements.Count != elementCountBefore)
                throw new Exception("The mesh was modified although the model already had one.");
            if (model.Mesh.Parts.Count != partCountBefore)
                throw new Exception("Parts were added although the model already had a mesh.");
            if (model.Materials.Count != 1)
                throw new Exception("The existing material was not reused.");
            if (model.CompositeLayups.Count != 2)
                throw new Exception("The second composite layup was not added.");
            //
            Console.WriteLine("  All checks passed.");
            // Save under a different name so that the PMX file of step 2 keeps a single layup; PrePoMax
            // saves the model into the last opened file when it is closed with unsaved changes
            string secondFileName = Path.Combine(Path.GetDirectoryName(_pmxFileName),
                                                 Path.GetFileNameWithoutExtension(_pmxFileName) + "_two_layups.pmx");
            controller.SaveToPmx(secondFileName);
            Console.WriteLine("  Written       : " + secondFileName);
        }
        private static void CheckResults(FeModel model)
        {
            List<string> errors = new List<string>();
            //
            if (model.Mesh.Elements.Count == 0) errors.Add("The model has no elements.");
            if (model.CompositeLayups.Count != 1) errors.Add("Expected exactly one composite layup.");
            //
            foreach (CompositeLayup layup in model.CompositeLayups.Values)
            {
                if (layup.Plies.Count == 0) errors.Add("The layup has no plies.");
                foreach (Ply ply in layup.Plies)
                {
                    if (!model.Mesh.ElementSets.ContainsKey(ply.Region))
                        errors.Add("The region '" + ply.Region + "' of ply '" + ply.Name + "' does not exist.");
                    if (ply.Thickness <= 0) errors.Add("The thickness of ply '" + ply.Name + "' is not positive.");
                }
            }
            //
            if (errors.Count > 0)
            {
                foreach (string error in errors) Console.WriteLine("  CHECK FAILED: " + error);
                throw new Exception(errors.Count + " check(s) failed.");
            }
            Console.WriteLine("  All checks passed.");
        }
    }
}
