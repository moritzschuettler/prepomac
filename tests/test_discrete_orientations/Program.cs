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
using CaeMesh;
using CaeModel;
using FileInOut.Output.Calculix;

namespace TestDiscreteOrientations
{
    class Program
    {
        static int Main(string[] args)
        {
            int failures = 0;

            // --- Test 1: XML reader ---
            Console.WriteLine("Test 1: XML reader");
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<DiscreteOrientations>
  <GlobalRosette X=""2.0"" Y=""0.0"" Z=""0.0""/>
  <Units Length=""mm"" Angle=""deg""/>
  <StackingPoint Id=""1"">
    <Position X=""0.0"" Y=""0.0"" Z=""0.0""/>
    <Stacking MaterialRefDirX=""1.0"" MaterialRefDirY=""0.0"" MaterialRefDirZ=""0.0"">
      <Layer Angle=""0.0""  Thickness=""0.25""/>
      <Layer Angle=""90.0"" Thickness=""0.25""/>
    </Stacking>
  </StackingPoint>
  <StackingPoint Id=""2"">
    <Position X=""1.0"" Y=""0.0"" Z=""0.0""/>
    <Stacking MaterialRefDirX=""0.0"" MaterialRefDirY=""1.0"" MaterialRefDirZ=""0.0"">
      <Layer Angle=""45.0"" Thickness=""0.5""/>
    </Stacking>
  </StackingPoint>
  <StackingPoint Id=""3"">
    <Position X=""0.0"" Y=""1.0"" Z=""0.0""/>
    <Stacking MaterialRefDirX=""1.0"" MaterialRefDirY=""1.0"" MaterialRefDirZ=""0.0"">
      <Layer Angle=""-45.0"" Thickness=""0.3""/>
      <Layer Angle=""0.0""   Thickness=""0.3""/>
    </Stacking>
  </StackingPoint>
  <StackingPoint Id=""4"">
    <Position X=""1.0"" Y=""1.0"" Z=""0.0""/>
    <Stacking>
      <Layer Angle=""30.0"" Thickness=""0.2""/>
    </Stacking>
  </StackingPoint>
</DiscreteOrientations>";

            string tmpFile = Path.GetTempFileName() + ".xml";
            File.WriteAllText(tmpFile, xml);

            DiscreteOrientationsData data = null;
            try
            {
                data = DiscreteOrientationsXmlReader.Read(tmpFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  FAIL: exception reading XML: " + ex.Message);
                failures++;
            }

            if (data != null)
            {
                failures += Assert("Point count == 4", data.Points.Count == 4);
                failures += Assert("Units length == mm", data.UnitsLength == "mm");
                failures += Assert("Units angle == deg", data.UnitsAngle == "deg");
                // GlobalRosette should be normalized
                double glen = Math.Sqrt(data.GlobalRosette[0] * data.GlobalRosette[0] +
                                        data.GlobalRosette[1] * data.GlobalRosette[1] +
                                        data.GlobalRosette[2] * data.GlobalRosette[2]);
                failures += Assert("GlobalRosette normalized", Math.Abs(glen - 1.0) < 1e-9);
                failures += Assert("Point 0 id == 1", data.Points[0].Id == 1);
                failures += Assert("Point 0 layers == 2", data.Points[0].Layers.Count == 2);
                failures += Assert("Point 0 layer 0 angle == 0", data.Points[0].Layers[0].Angle == 0.0);
                failures += Assert("Point 0 layer 1 angle == 90", data.Points[0].Layers[1].Angle == 90.0);
                failures += Assert("Point 0 layer 0 thickness == 0.25", data.Points[0].Layers[0].Thickness == 0.25);
                failures += Assert("Point 1 layers == 1", data.Points[1].Layers.Count == 1);
                failures += Assert("Point 1 matRefDir Y == 1", Math.Abs(data.Points[1].MaterialRefDir[1] - 1.0) < 1e-9);
                failures += Assert("Point 2 layers == 2", data.Points[2].Layers.Count == 2);
                failures += Assert("Point 2 layer 0 angle == -45", data.Points[2].Layers[0].Angle == -45.0);
                // MaterialRefDir should be normalized
                double[] mrd = data.Points[2].MaterialRefDir;
                double len = Math.Sqrt(mrd[0] * mrd[0] + mrd[1] * mrd[1] + mrd[2] * mrd[2]);
                failures += Assert("Point 2 matRefDir normalized", Math.Abs(len - 1.0) < 1e-9);
                // Absent MaterialRefDir attributes => null (use GlobalRosette)
                failures += Assert("Point 3 matRefDir null", data.Points[3].MaterialRefDir == null);
                Console.WriteLine("  Point positions:");
                foreach (var sp in data.Points)
                    Console.WriteLine(string.Format("    ({0:F2}, {1:F2}, {2:F2})", sp.Position[0], sp.Position[1], sp.Position[2]));
            }

            // --- Test 2: Error on bad XML ---
            Console.WriteLine("Test 2: Error on negative thickness");
            string badXml = @"<?xml version=""1.0""?>
<DiscreteOrientations>
  <GlobalRosette X=""1.0"" Y=""0.0"" Z=""0.0""/>
  <Units Length=""mm"" Angle=""deg""/>
  <StackingPoint Id=""1"">
    <Position X=""0"" Y=""0"" Z=""0""/>
    <Stacking>
      <Layer Angle=""0.0"" Thickness=""-1.0""/>
    </Stacking>
  </StackingPoint>
</DiscreteOrientations>";
            string tmpBad = Path.GetTempFileName() + ".xml";
            File.WriteAllText(tmpBad, badXml);
            bool threwOnBad = false;
            try { DiscreteOrientationsXmlReader.Read(tmpBad); }
            catch { threwOnBad = true; }
            failures += Assert("Throws on negative thickness", threwOnBad);

            Console.WriteLine("Test 3: Error on missing GlobalRosette");
            string noRosetteXml = @"<?xml version=""1.0""?>
<DiscreteOrientations>
  <Units Length=""mm"" Angle=""deg""/>
  <StackingPoint Id=""1"">
    <Position X=""0"" Y=""0"" Z=""0""/>
    <Stacking>
      <Layer Angle=""0.0"" Thickness=""0.25""/>
    </Stacking>
  </StackingPoint>
</DiscreteOrientations>";
            string tmpNoRosette = Path.GetTempFileName() + ".xml";
            File.WriteAllText(tmpNoRosette, noRosetteXml);
            bool threwOnNoRosette = false;
            try { DiscreteOrientationsXmlReader.Read(tmpNoRosette); }
            catch { threwOnNoRosette = true; }
            failures += Assert("Throws on missing GlobalRosette", threwOnNoRosette);

            // --- Test 4: CompositeLayup carries DiscreteOrientationsData ---
            Console.WriteLine("Test 4: CompositeLayup data round-trip");
            var layup = new CompositeLayup("TestLayup", 1, CompositeElementType.ConventionalShell);
            layup.DiscreteOrientationsEnabled = true;
            layup.DiscreteOrientationsData = data;
            failures += Assert("Layup enabled", layup.DiscreteOrientationsEnabled);
            failures += Assert("Layup data not null", layup.DiscreteOrientationsData != null);
            failures += Assert("Layup data point count == 4",
                layup.DiscreteOrientationsData != null && layup.DiscreteOrientationsData.Points.Count == 4);

            // --- Test 5: Solid mapping — one group per (stacking point x ply) ---
            Console.WriteLine("Test 5: Solid mapping (points x plies groups)");
            {
                FeMesh mesh = BuildLayeredHexMesh(false);
                DiscreteOrientationsData solidData = BuildSolidData();
                CompositeLayup solidLayup = BuildSolidLayup(solidData);
                var writer = new DiscreteOrientationsWriter(solidLayup, mesh);
                writer.Process();

                failures += Assert("Element set groups == 4", writer.ItaElementSets.Count == 4);
                failures += Assert("Solid sections == 4", writer.SolidSectionKeywords.Count == 4);
                failures += Assert("Shell sections == 0", writer.SectionKeywords.Count == 0);
                failures += Assert("Orientations == 4", writer.OrientationKeywords.Count == 4);
                failures += Assert("No warnings", writer.Warnings.Count == 0);
                // Nearest-point split: point 1 (x=0.5) <- elements 1,3 / 5,7; point 2 (x=1.5) <- 2,4 / 6,8
                failures += Assert("Ply 0 / point 1 group: elements {1,3}, angle 10, MatA",
                    CheckSolidGroup(writer, new int[] { 1, 3 }, 10.0, "MatA"));
                failures += Assert("Ply 0 / point 2 group: elements {2,4}, angle 45, MatA",
                    CheckSolidGroup(writer, new int[] { 2, 4 }, 45.0, "MatA"));
                failures += Assert("Ply 1 / point 1 group: elements {5,7}, angle 30, MatB",
                    CheckSolidGroup(writer, new int[] { 5, 7 }, 30.0, "MatB"));
                failures += Assert("Ply 1 / point 2 group: elements {6,8}, angle 60, MatB",
                    CheckSolidGroup(writer, new int[] { 6, 8 }, 60.0, "MatB"));
                failures += Assert("Parent elset union emitted",
                    writer.SolidParentElementSets.Count == 1 &&
                    writer.SolidParentElementSets[0].GetKeywordString().Contains("SolidPart"));
                failures += Assert("Log contains per-ply group counts",
                    writer.LogText != null && writer.LogText.Contains("Per-ply group counts"));
            }

            // --- Test 6: Tow-drop — stacking point with fewer layers than plies ---
            Console.WriteLine("Test 6: Tow-drop fallback");
            {
                FeMesh mesh = BuildLayeredHexMesh(false);
                // Point 2 has only 1 layer -> ply 1 elements nearest to it are a dropped-ply zone
                DiscreteOrientationsData solidData = BuildSolidData();
                solidData.Points[1].Layers.RemoveAt(1);
                CompositeLayup solidLayup = BuildSolidLayup(solidData);
                solidLayup.Plies[1].RotationAngle = 90.0;
                var writer = new DiscreteOrientationsWriter(solidLayup, mesh);
                writer.Process();

                failures += Assert("Sections still == 4", writer.SolidSectionKeywords.Count == 4);
                failures += Assert("Tow-drop warning emitted",
                    writer.Warnings.Any(w => w.Contains("tow-drop")));
                failures += Assert("Dropped group uses ply rotation angle 90",
                    CheckSolidGroup(writer, new int[] { 6, 8 }, 90.0, "MatB"));
                failures += Assert("Other ply 1 group keeps XML angle 30",
                    CheckSolidGroup(writer, new int[] { 5, 7 }, 30.0, "MatB"));
            }

            // --- Test 7: Ply without region — warning, no all-mesh fallback ---
            Console.WriteLine("Test 7: Ply without region");
            {
                FeMesh mesh = BuildLayeredHexMesh(false);
                DiscreteOrientationsData solidData = BuildSolidData();
                CompositeLayup solidLayup = BuildSolidLayup(solidData);
                solidLayup.Plies[1].Region = "NoSuchSet";
                var writer = new DiscreteOrientationsWriter(solidLayup, mesh);
                writer.Process();

                failures += Assert("Only ply 0 groups generated", writer.ItaElementSets.Count == 2);
                failures += Assert("Missing region warning emitted",
                    writer.Warnings.Any(w => w.Contains("not found")));
                failures += Assert("No all-mesh fallback (4 elements mapped)",
                    writer.ItaElementSets.Sum(s => s.Labels.Length) == 4);
            }

            // --- Test 8: Shell elements in a solid layup region are skipped ---
            Console.WriteLine("Test 8: Non-solid elements skipped");
            {
                FeMesh mesh = BuildLayeredHexMesh(true);
                DiscreteOrientationsData solidData = BuildSolidData();
                CompositeLayup solidLayup = BuildSolidLayup(solidData);
                var writer = new DiscreteOrientationsWriter(solidLayup, mesh);
                writer.Process();

                failures += Assert("Skipped-element warning emitted",
                    writer.Warnings.Any(w => w.Contains("non-solid")));
                failures += Assert("Shell element excluded from ply 0 groups",
                    writer.ItaElementSets.Where(s => s.Name.Contains("_P0_"))
                                         .Sum(s => s.Labels.Length) == 4);
            }

            // Cleanup
            try { File.Delete(tmpFile); File.Delete(tmpBad); File.Delete(tmpNoRosette); } catch { }

            Console.WriteLine();
            if (failures == 0)
                Console.WriteLine("All tests PASSED.");
            else
                Console.WriteLine(failures + " test(s) FAILED.");

            return failures == 0 ? 0 : 1;
        }

        // 2x2 elements x 2 layers hex mesh: nodes (0..2, 0..2, 0..2), elements 1-4 in layer 1
        // (z 0-1, set "Layer1"), elements 5-8 in layer 2 (z 1-2, set "Layer2")
        static FeMesh BuildLayeredHexMesh(bool withShellInLayer1)
        {
            var nodes = new Dictionary<int, FeNode>();
            for (int iz = 0; iz <= 2; iz++)
                for (int iy = 0; iy <= 2; iy++)
                    for (int ix = 0; ix <= 2; ix++)
                    {
                        int nid = iz * 9 + iy * 3 + ix + 1;
                        nodes.Add(nid, new FeNode(nid, ix, iy, iz));
                    }

            var elements = new Dictionary<int, FeElement>();
            for (int iz = 0; iz <= 1; iz++)
                for (int iy = 0; iy <= 1; iy++)
                    for (int ix = 0; ix <= 1; ix++)
                    {
                        int eid = iz * 4 + iy * 2 + ix + 1;
                        int n0 = iz * 9 + iy * 3 + ix + 1;
                        int[] nids = new int[] { n0, n0 + 1, n0 + 4, n0 + 3,
                                                 n0 + 9, n0 + 10, n0 + 13, n0 + 12 };
                        elements.Add(eid, new LinearHexaElement(eid, nids));
                    }

            var inpSets = new List<InpElementSet>
            {
                new InpElementSet("C3D8", new HashSet<string> { "C3D8" }, new HashSet<int>(elements.Keys))
            };
            if (withShellInLayer1)
            {
                elements.Add(9, new LinearQuadrilateralElement(9, new int[] { 1, 2, 5, 4 }));
                inpSets.Add(new InpElementSet("S4", new HashSet<string> { "S4" }, new HashSet<int> { 9 }));
            }

            FeMesh mesh = new FeMesh(nodes, elements, MeshRepresentation.Mesh, inpSets);

            var layer1 = new List<int> { 1, 2, 3, 4 };
            if (withShellInLayer1) layer1.Add(9);
            mesh.ElementSets.Add("Layer1", new FeElementSet("Layer1", layer1.ToArray()));
            mesh.ElementSets.Add("Layer2", new FeElementSet("Layer2", new int[] { 5, 6, 7, 8 }));
            return mesh;
        }

        // Two stacking points with 2 layers each: point 1 at x=0.5 (angles 10/30, ref dir +X),
        // point 2 at x=1.5 (angles 45/60, ref dir +Y)
        static DiscreteOrientationsData BuildSolidData()
        {
            return new DiscreteOrientationsData
            {
                GlobalRosette = new double[] { 1.0, 0.0, 0.0 },
                Points = new List<StackingPoint>
                {
                    new StackingPoint
                    {
                        Id = 1,
                        Position = new double[] { 0.5, 1.0, 1.0 },
                        MaterialRefDir = new double[] { 1.0, 0.0, 0.0 },
                        Layers = new List<StackingLayer>
                        {
                            new StackingLayer { Angle = 10.0, Thickness = 1.0 },
                            new StackingLayer { Angle = 30.0, Thickness = 1.0 }
                        }
                    },
                    new StackingPoint
                    {
                        Id = 2,
                        Position = new double[] { 1.5, 1.0, 1.0 },
                        MaterialRefDir = new double[] { 0.0, 1.0, 0.0 },
                        Layers = new List<StackingLayer>
                        {
                            new StackingLayer { Angle = 45.0, Thickness = 1.0 },
                            new StackingLayer { Angle = 60.0, Thickness = 1.0 }
                        }
                    }
                }
            };
        }

        static CompositeLayup BuildSolidLayup(DiscreteOrientationsData data)
        {
            CompositeLayup layup = new CompositeLayup("LayupS", 2, CompositeElementType.Solid);
            layup.Plies[0] = new Ply("Ply-1", "Layer1", "MatA", 1.0, "<layup>", 0.0);
            layup.Plies[1] = new Ply("Ply-2", "Layer2", "MatB", 1.0, "<layup>", 0.0);
            layup.DiscreteOrientationsEnabled = true;
            layup.DiscreteOrientationsData = data;
            layup.SolidPartName = "SolidPart";
            return layup;
        }

        // Finds the group element set matching the expected element ids and checks its
        // orientation angle and section material (set, orientation and section are index-aligned)
        static bool CheckSolidGroup(DiscreteOrientationsWriter writer, int[] expectedIds,
                                    double expectedAngle, string expectedMaterial)
        {
            for (int i = 0; i < writer.ItaElementSets.Count; i++)
            {
                int[] labels = writer.ItaElementSets[i].Labels.OrderBy(l => l).ToArray();
                if (!labels.SequenceEqual(expectedIds.OrderBy(l => l))) continue;
                string oriName = "Ori_" + writer.ItaElementSets[i].Name;
                if (!writer.OrientationKeywords[i].GetKeywordString().Contains("Name=" + oriName)) return false;
                double angle = GetOrientationAngle(writer.OrientationKeywords[i]);
                if (Math.Abs(angle - expectedAngle) > 1e-9) return false;
                string section = writer.SolidSectionKeywords[i].GetKeywordString();
                return section.Contains("Elset=" + writer.ItaElementSets[i].Name) &&
                       section.Contains("Material=" + expectedMaterial) &&
                       section.Contains("Orientation=" + oriName);
            }
            return false;
        }

        static double GetOrientationAngle(CalOrientation orientation)
        {
            // Second data line is "rotationAxis, angle"
            string[] lines = orientation.GetDataString()
                .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] parts = lines[1].Split(',');
            return double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
        }

        static int Assert(string name, bool condition)
        {
            if (condition)
            {
                Console.WriteLine("  PASS: " + name);
                return 0;
            }
            else
            {
                Console.WriteLine("  FAIL: " + name);
                return 1;
            }
        }
    }
}
