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

namespace TestTapePropagation
{
    // Standalone test: opens the cooperhood.pmx project (headless, no visible UI), adds a fresh
    // CompositeLayup with ProcessType = TapePlacement, picks a seed point on the panel, and runs the
    // tape placement (AFP) geodesic propagation algorithm end to end - through the same
    // Controller.DrawTapePropagation / ComputeTapePropagation methods the real dialog uses. The
    // generated tape courses are exported to an OBJ polyline file for visual inspection and the
    // resulting model is saved to a new PMX so it can also be opened in the app.
    class Program
    {
        private static string _pmxFileName;
        private static string _outputDir;
        private static int _exitCode;

        [STAThread]
        static int Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("Test: Tape placement (AFP) geodesic propagation");
            Console.WriteLine("==================================================");
            Console.WriteLine();
            try
            {
                _outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                Directory.CreateDirectory(_outputDir);
                //
                _pmxFileName = args.Length > 0 ? args[0] : FindFile(Path.Combine("cooperhood", "cooperhood.pmx"));
                if (_pmxFileName == null || !File.Exists(_pmxFileName))
                {
                    Console.WriteLine("ERROR: cooperhood.pmx not found.");
                    Console.WriteLine("  Searched in assets_composites/ up from exe directory.");
                    Console.WriteLine("  Usage: TestTapePropagation.exe [pmxPath]");
                    return 1;
                }
                Console.WriteLine("PMX : " + _pmxFileName);
                //
                RunHeadless();
                //
                if (_exitCode == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("----------Finished------------");
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

        // Walk up the directory tree looking for assets_composites/<relativePath>
        private static string FindFile(string relativePath)
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                string candidate = Path.Combine(dir, "assets_composites", relativePath);
                if (File.Exists(candidate)) return candidate;
                string parent = Path.GetDirectoryName(dir);
                if (parent == null || parent == dir) break;
                dir = parent;
            }
            return null;
        }

        // Drive PrePoMax without its user interface (same pattern as TestCompositeCaeH5): a real,
        // minimized, hidden FrmMain is required because reading a .pmx and drawing the tape overlay
        // both go through Controller/_form - but nothing is ever shown on screen.
        private static void RunHeadless()
        {
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
                controller.Open(_pmxFileName);
                //
                FeModel model = controller.Model;
                Console.WriteLine("  Nodes         : " + model.Mesh.Nodes.Count);
                Console.WriteLine("  Elements      : " + model.Mesh.Elements.Count);
                Console.WriteLine("  Element sets  : " + model.Mesh.ElementSets.Count);
                Console.WriteLine("  Materials     : " + model.Materials.Count);
                Console.WriteLine("  Layups (existing): " + model.CompositeLayups.Count);
                if (model.Mesh.Elements.Count == 0) throw new Exception("The opened model has no elements.");
                //
                Console.WriteLine();
                Console.WriteLine("Step 1: Building a Tape Placement composite layup...");
                CompositeLayup layup = BuildTapePlacementLayup(controller, model);
                Console.WriteLine("  Layup '" + layup.Name + "' region='" + layup.Plies[0].Region +
                                  "' material='" + layup.Plies[0].Material + "'");
                Console.WriteLine("  Seed point    : (" + FormatXyz(layup.Plies[0].PropagationSeedPoint) + ")");
                Console.WriteLine("  Tape width    : " + layup.Plies[0].TapeWidth.Value.ToString("G6", CultureInfo.InvariantCulture));
                //
                Console.WriteLine();
                Console.WriteLine("Step 2: Generating tape courses (Controller.DrawTapePropagation - full UI path)...");
                int drawStatus = controller.DrawTapePropagation(layup, 0);
                Console.WriteLine("  DrawTapePropagation returned: " + drawStatus);
                if (drawStatus < 0)
                    throw new Exception("DrawTapePropagation failed with status " + drawStatus +
                                        " (-1 = no result, -2 = Rosette not implemented).");
                //
                Console.WriteLine();
                Console.WriteLine("Step 3: Recomputing via Controller.ComputeTapePropagation to export the curves...");
                double[][] refCurve;
                List<TapePropagationTape> tapes;
                int status = controller.ComputeTapePropagation(layup, 0, out refCurve, out tapes);
                Console.WriteLine("  ComputeTapePropagation returned: " + status + " tape(s) (plus the ref curve)");
                //
                CheckResults(status, refCurve, tapes);
                ReportCurves(refCurve, tapes);
                //
                string objPath = Path.Combine(_outputDir, "cooperhood_tape_propagation.obj");
                WriteTapeCurvesObj(objPath, refCurve, tapes);
                Console.WriteLine();
                Console.WriteLine("  OBJ written   : " + objPath);
                //
                model.CompositeLayups.Add(layup.Name, layup);
                string outPmx = Path.Combine(_outputDir, "cooperhood_tape_propagation.pmx");
                controller.SaveToPmx(outPmx);
                Console.WriteLine("  PMX written   : " + outPmx);
                //
                ReopenAndVerify(controller, outPmx, layup.Name);
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

        // Builds a fresh CompositeLayup (ProcessType = TapePlacement) covering every element of the
        // opened mesh, seeded at the region's centroid, with an auto-scaled tape width.
        private static CompositeLayup BuildTapePlacementLayup(Controller controller, FeModel model)
        {
            FeMesh mesh = model.Mesh;
            //
            int[] allElementIds = mesh.Elements.Keys.ToArray();
            if (allElementIds.Length == 0) throw new Exception("The mesh has no elements to build a region from.");
            //
            string regionName = "TapePropagationTestRegion";
            int suffix = 1;
            while (mesh.ElementSets.ContainsKey(regionName)) regionName = "TapePropagationTestRegion_" + (suffix++);
            mesh.ElementSets.Add(regionName, new FeElementSet(regionName, allElementIds));
            //
            // Seed point: centroid of the region, and the mesh bounding-box diagonal for an
            // auto-scaled tape width default (mirrors SuggestDrapingCellSize's "short side / 25" idea)
            double cx = 0, cy = 0, cz = 0;
            double xMin = double.MaxValue, xMax = double.MinValue;
            double yMin = double.MaxValue, yMax = double.MinValue;
            double zMin = double.MaxValue, zMax = double.MinValue;
            int n = 0;
            foreach (int eid in allElementIds)
            {
                FeElement element;
                if (!mesh.Elements.TryGetValue(eid, out element)) continue;
                double size;
                double[] cg = element.GetCG(mesh.Nodes, out size);
                cx += cg[0]; cy += cg[1]; cz += cg[2];
                n++;
                foreach (int nid in element.NodeIds)
                {
                    FeNode nd = mesh.Nodes[nid];
                    if (nd.X < xMin) xMin = nd.X; if (nd.X > xMax) xMax = nd.X;
                    if (nd.Y < yMin) yMin = nd.Y; if (nd.Y > yMax) yMax = nd.Y;
                    if (nd.Z < zMin) zMin = nd.Z; if (nd.Z > zMax) zMax = nd.Z;
                }
            }
            if (n == 0) throw new Exception("Could not compute a seed point - no valid elements in the region.");
            double[] seedPoint = { cx / n, cy / n, cz / n };
            double bboxDiagonal = Math.Sqrt(Math.Pow(xMax - xMin, 2) + Math.Pow(yMax - yMin, 2) + Math.Pow(zMax - zMin, 2));
            double tapeWidth = Math.Max(bboxDiagonal * 0.02, 1e-6);
            //
            string materialName;
            if (model.Materials.Count > 0)
            {
                materialName = model.Materials.Values.First().Name;
            }
            else
            {
                Material material = new Material("TapePropagationTestMaterial");
                model.Materials.Add(material.Name, material);
                materialName = material.Name;
            }
            //
            string layupName = "Layup_TapePlacement_Test";
            int layupSuffix = 1;
            while (model.CompositeLayups.ContainsKey(layupName)) layupName = "Layup_TapePlacement_Test_" + (layupSuffix++);
            //
            CompositeLayup layup = new CompositeLayup(layupName, 1, CompositeElementType.ConventionalShell);
            layup.ProcessType = ProcessType.TapePlacement;
            Ply ply = new Ply("Ply-0", regionName, materialName, 0.25, "<layup>", 0.0);
            ply.PropagationType = PropagationPathType.Natural;
            ply.PropagationSeedPoint = seedPoint;
            ply.TapeWidth = tapeWidth;
            layup.Plies[0] = ply;
            //
            return layup;
        }

        // Reopens the saved PMX to confirm the TapePlacement layup and its per-ply propagation fields
        // (seed point / type / tape width) survive binary serialization.
        private static void ReopenAndVerify(Controller controller, string pmxPath, string layupName)
        {
            Console.WriteLine();
            Console.WriteLine("Step 4: Reopening the saved PMX to verify round-trip...");
            controller.Open(pmxPath);
            //
            FeModel model = controller.Model;
            CompositeLayup layup;
            if (!model.CompositeLayups.TryGetValue(layupName, out layup))
                throw new Exception("The saved layup '" + layupName + "' was not found after reopening.");
            if (layup.ProcessType != ProcessType.TapePlacement)
                throw new Exception("ProcessType did not survive the round-trip.");
            Ply ply = layup.Plies[0];
            if (ply.PropagationSeedPoint == null || ply.PropagationSeedPoint.Length != 3)
                throw new Exception("PropagationSeedPoint did not survive the round-trip.");
            if (!ply.TapeWidth.HasValue)
                throw new Exception("TapeWidth did not survive the round-trip.");
            //
            int status = controller.DrawTapePropagation(layup, 0);
            if (status < 0) throw new Exception("Re-running DrawTapePropagation after reopen failed with status " + status);
            //
            Console.WriteLine("  Layup, ProcessType, seed point and tape width all survived the round-trip.");
            Console.WriteLine("  DrawTapePropagation after reopen returned: " + status);
        }

        private static void CheckResults(int status, double[][] refCurve, List<TapePropagationTape> tapes)
        {
            List<string> errors = new List<string>();
            if (status < 0) errors.Add("ComputeTapePropagation returned a failure status: " + status);
            if (refCurve == null || refCurve.Length < 2) errors.Add("The reference curve has fewer than 2 points.");
            if (tapes == null || tapes.Count == 0) errors.Add("No parallel tape courses were generated.");
            if (tapes != null)
            {
                bool hasPositiveSide = tapes.Any(t => t.Side > 0);
                bool hasNegativeSide = tapes.Any(t => t.Side < 0);
                if (!hasPositiveSide || !hasNegativeSide)
                    errors.Add("Expected tape courses on both sides of the reference curve.");
                if (tapes.Any(t => t.Points == null || t.Points.Length < 2))
                    errors.Add("At least one tape course has fewer than 2 points.");
            }
            //
            if (errors.Count > 0)
            {
                foreach (string error in errors) Console.WriteLine("  CHECK FAILED: " + error);
                throw new Exception(errors.Count + " check(s) failed.");
            }
            Console.WriteLine("  All checks passed.");
        }

        private static void ReportCurves(double[][] refCurve, List<TapePropagationTape> tapes)
        {
            Console.WriteLine();
            Console.WriteLine("  Ref curve     : " + refCurve.Length + " pts, length=" +
                              PolylineLength(refCurve).ToString("G6", CultureInfo.InvariantCulture));
            //
            foreach (int side in new[] { 1, -1 })
            {
                var sideTapes = tapes.Where(t => t.Side == side).OrderBy(t => t.Layer).ToList();
                Console.WriteLine("  Side " + (side > 0 ? "+1" : "-1") + "         : " + sideTapes.Count + " layer(s)");
                foreach (TapePropagationTape t in sideTapes.Take(5))
                    Console.WriteLine("    layer " + t.Layer + ": " + t.Points.Length + " pts, length=" +
                                      PolylineLength(t.Points).ToString("G6", CultureInfo.InvariantCulture));
                if (sideTapes.Count > 5) Console.WriteLine("    ...");
            }
        }

        private static double PolylineLength(double[][] pts)
        {
            double total = 0;
            for (int i = 1; i < pts.Length; i++)
            {
                double dx = pts[i][0] - pts[i - 1][0];
                double dy = pts[i][1] - pts[i - 1][1];
                double dz = pts[i][2] - pts[i - 1][2];
                total += Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            return total;
        }

        private static string FormatXyz(double[] p)
        {
            return p[0].ToString("G6", CultureInfo.InvariantCulture) + ", " +
                   p[1].ToString("G6", CultureInfo.InvariantCulture) + ", " +
                   p[2].ToString("G6", CultureInfo.InvariantCulture);
        }

        // Writes every generated curve as an OBJ polyline (widely-supported "v x y z r g b" vertex
        // color extension, e.g. MeshLab/CloudCompare): the reference curve is yellow, side +1 courses
        // fade through a blue family and side -1 courses through a red/orange family - same scheme as
        // vtkControl.AddTapePropagationCurves uses for the in-app overlay.
        private static void WriteTapeCurvesObj(string path, double[][] refCurve, List<TapePropagationTape> tapes)
        {
            using (var w = new StreamWriter(path, false, System.Text.Encoding.ASCII))
            {
                w.WriteLine("# Tape propagation result - polylines");
                w.WriteLine("# ref curve = yellow, side +1 = blue family, side -1 = red family");
                w.WriteLine();
                int nextIndex = 1;
                WriteCurve(w, refCurve, 1.0, 1.0, 0.0, ref nextIndex);
                foreach (TapePropagationTape tape in tapes)
                {
                    double r, g, b;
                    GetColor(tape.Side, tape.Layer, out r, out g, out b);
                    WriteCurve(w, tape.Points, r, g, b, ref nextIndex);
                }
            }
        }
        private static void WriteCurve(StreamWriter w, double[][] pts, double r, double g, double b, ref int nextIndex)
        {
            if (pts == null || pts.Length < 2) return;
            int start = nextIndex;
            foreach (double[] p in pts)
            {
                w.WriteLine(string.Format(CultureInfo.InvariantCulture, "v {0:G8} {1:G8} {2:G8} {3:F3} {4:F3} {5:F3}",
                                           p[0], p[1], p[2], r, g, b));
                nextIndex++;
            }
            var sb = new System.Text.StringBuilder("l");
            for (int i = start; i < nextIndex; i++) sb.Append(' ').Append(i);
            w.WriteLine(sb.ToString());
        }
        private static void GetColor(int side, int layer, out double r, out double g, out double b)
        {
            double t = Math.Min(1.0, (layer - 1) / 9.0);
            if (side > 0) { r = 0.3 * (1 - t); g = 0.6 * (1 - t) + 0.15 * t; b = 1.0; }
            else { r = 1.0; g = 0.55 * (1 - t); b = 0.15 * (1 - t); }
        }
    }
}
