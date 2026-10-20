using System;
using System.IO;
using CaeModel;

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
  <StackingPoint>
    <Position X=""0.0"" Y=""0.0"" Z=""0.0""/>
    <MaterialRefDir X=""1.0"" Y=""0.0"" Z=""0.0""/>
    <Layer Angle=""0.0""  Thickness=""0.25""/>
    <Layer Angle=""90.0"" Thickness=""0.25""/>
  </StackingPoint>
  <StackingPoint>
    <Position X=""1.0"" Y=""0.0"" Z=""0.0""/>
    <MaterialRefDir X=""0.0"" Y=""1.0"" Z=""0.0""/>
    <Layer Angle=""45.0"" Thickness=""0.5""/>
  </StackingPoint>
  <StackingPoint>
    <Position X=""0.0"" Y=""1.0"" Z=""0.0""/>
    <MaterialRefDir X=""1.0"" Y=""1.0"" Z=""0.0""/>
    <Layer Angle=""-45.0"" Thickness=""0.3""/>
    <Layer Angle=""0.0""   Thickness=""0.3""/>
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
                failures += Assert("Point count == 3", data.Points.Count == 3);
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
                Console.WriteLine("  Point positions:");
                foreach (var sp in data.Points)
                    Console.WriteLine(string.Format("    ({0:F2}, {1:F2}, {2:F2})", sp.Position[0], sp.Position[1], sp.Position[2]));
            }

            // --- Test 2: Error on bad XML ---
            Console.WriteLine("Test 2: Error on missing Thickness");
            string badXml = @"<?xml version=""1.0""?>
<DiscreteOrientations>
  <StackingPoint>
    <Position X=""0"" Y=""0"" Z=""0""/>
    <MaterialRefDir X=""1"" Y=""0"" Z=""0""/>
    <Layer Angle=""0.0"" Thickness=""-1.0""/>
  </StackingPoint>
</DiscreteOrientations>";
            string tmpBad = Path.GetTempFileName() + ".xml";
            File.WriteAllText(tmpBad, badXml);
            bool threwOnBad = false;
            try { DiscreteOrientationsXmlReader.Read(tmpBad); }
            catch { threwOnBad = true; }
            failures += Assert("Throws on negative thickness", threwOnBad);

            // --- Test 3: CompositeLayup carries DiscreteOrientationsData ---
            Console.WriteLine("Test 3: CompositeLayup data round-trip");
            var layup = new CompositeLayup("TestLayup", 1, CompositeElementType.ConventionalShell);
            layup.DiscreteOrientationsEnabled = true;
            layup.DiscreteOrientationsData = data;
            failures += Assert("Layup enabled", layup.DiscreteOrientationsEnabled);
            failures += Assert("Layup data not null", layup.DiscreteOrientationsData != null);
            failures += Assert("Layup data point count == 3", layup.DiscreteOrientationsData.Points.Count == 3);

            // Cleanup
            try { File.Delete(tmpFile); File.Delete(tmpBad); } catch { }

            Console.WriteLine();
            if (failures == 0)
                Console.WriteLine("All tests PASSED.");
            else
                Console.WriteLine(failures + " test(s) FAILED.");

            return failures == 0 ? 0 : 1;
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
