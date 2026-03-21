using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using FileInOut.Input;
using FileInOut.Output;
using FileInOut.Output.Calculix;

namespace TestCompositeTensileCouponExport
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("Test: INP Export — Composite Tensile Coupon");
            Console.WriteLine("==============================================");
            Console.WriteLine();

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string outputDir = Path.Combine(baseDir, "output");
                Directory.CreateDirectory(outputDir);

                // Resolve the Abaqus INP file from the assets folder (5 levels up from bin\x64\Debug\)
                string assetsDir = Path.GetFullPath(
                    Path.Combine(baseDir, @"..\..\..\..\..\assets_composites\abq_tensile_coupon"));
                string abqInpFile = Path.Combine(assetsDir, "tensile_coupon_ABQ.inp");
                string outputInpFile = Path.Combine(outputDir, "tensile_coupon_composite.inp");

                if (!File.Exists(abqInpFile))
                    throw new FileNotFoundException("ABQ INP file not found: " + abqInpFile);

                Console.WriteLine("Step 1: Reading mesh from Abaqus INP file...");
                Console.WriteLine($"  Source: {abqInpFile}");

                UnitSystem unitSystem = new UnitSystem(UnitSystemType.MM_TON_S_C);
                unitSystem.SetConverterUnits();

                FeModel model = new FeModel("TensileCouponComposite", unitSystem, true);
                model.Properties.ModelSpace = ModelSpaceEnum.ThreeD;
                OrderedDictionary<int[], CalculixUserKeyword> userKeywords;
                InpFileReader.Read(abqInpFile, ElementsToImport.Shell, model,
                    msg => { }, out userKeywords);

                Console.WriteLine($"  Nodes   : {model.Mesh.Nodes.Count}");
                Console.WriteLine($"  Elements: {model.Mesh.Elements.Count}");
                Console.WriteLine($"  Parts   : {model.Mesh.Parts.Count}");
                Console.WriteLine($"  ElSets  : {model.Mesh.ElementSets.Count}");
                Console.WriteLine();

                // Verify the ply region element set is present
                const string elsetName = "CompositeLayup-1-1";
                if (!model.Mesh.ElementSets.ContainsKey(elsetName))
                    throw new Exception($"Element set '{elsetName}' not found after reading ABQ INP.");

                int elsetCount = model.Mesh.ElementSets[elsetName].Labels.Length;
                Console.WriteLine($"  Element set '{elsetName}': {elsetCount} elements");
                Console.WriteLine();

                Console.WriteLine("Step 2: Setting up composite material...");

                // Remove any material added by the reader (may be incomplete due to format differences)
                const string matName = "composite-material";
                if (model.Materials.ContainsKey(matName))
                    model.Materials.Remove(matName);

                // Lamina: E1=138000, E2=9500, nu12=0.28, G12=5200, G13=5200, G23=1450 MPa
                double[][] laminaData = new double[][]
                {
                    new double[] { 138000.0, 9500.0, 0.28, 5200.0, 5200.0, 1450.0, 0.0 }
                };
                Lamina laminaProp = new Lamina(laminaData);
                Material mat = new Material(matName);
                mat.AddProperty(laminaProp);
                model.Materials.Add(matName, mat);

                Console.WriteLine($"  Material '{matName}' added (Lamina E1={laminaData[0][0]}, E2={laminaData[0][1]})");
                Console.WriteLine();

                Console.WriteLine("Step 3: Creating composite layup...");

                // Remove any shell sections imported from the ABQ file (composite section fails to parse)
                model.Sections.Clear();

                // 3-ply quasi-isotropic [0/45/-45], all plies on the same element set
                CompositeLayup layup = new CompositeLayup("CompositeLayup-1", 3,
                    CompositeElementType.ConventionalShell);
                layup.Plies[0] = new Ply("Ply-1", elsetName, matName, 0.3, "<layup>", 0.0);
                layup.Plies[1] = new Ply("Ply-2", elsetName, matName, 0.3, "<layup>", 45.0);
                layup.Plies[2] = new Ply("Ply-3", elsetName, matName, 0.3, "<layup>", -45.0);
                model.CompositeLayups.Add(layup.Name, layup);

                Console.WriteLine($"  Layup '{layup.Name}': {layup.Plies.Count} plies, region '{elsetName}'");
                Console.WriteLine();

                Console.WriteLine("Step 4: Exporting to CalculiX INP...");
                CalculixFileWriter.Write(outputInpFile, model, ConvertPyramidsToEnum.Wedges);
                Console.WriteLine($"  Written: {outputInpFile}");
                Console.WriteLine();

                Console.WriteLine("Step 5: Verifying INP output...");
                bool passed = VerifyOutput(outputInpFile, elsetName);

                Console.WriteLine();
                if (passed)
                {
                    Console.WriteLine("==============================================");
                    Console.WriteLine("TEST PASSED");
                    Console.WriteLine("==============================================");
                    return 0;
                }
                else
                {
                    Console.WriteLine("==============================================");
                    Console.WriteLine("TEST FAILED");
                    Console.WriteLine("==============================================");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("==============================================");
                Console.WriteLine($"TEST ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("==============================================");
                return 2;
            }
        }

        static bool VerifyOutput(string inpFile, string expectedElset)
        {
            string content = File.ReadAllText(inpFile);
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            bool passed = true;

            // 1. No redundant ITA element set should be generated
            bool hasItaElset = lines.Any(l =>
                l.TrimStart().StartsWith("*Elset", StringComparison.OrdinalIgnoreCase) &&
                l.IndexOf("_ITA_", StringComparison.OrdinalIgnoreCase) >= 0);
            if (hasItaElset)
            {
                Console.WriteLine("  FAIL: Found redundant *Elset with _ITA_ suffix (fix not applied).");
                foreach (var l in lines.Where(l =>
                    l.TrimStart().StartsWith("*Elset", StringComparison.OrdinalIgnoreCase) &&
                    l.IndexOf("_ITA_", StringComparison.OrdinalIgnoreCase) >= 0))
                    Console.WriteLine("        " + l);
                passed = false;
            }
            else
            {
                Console.WriteLine("  PASS: No redundant ITA element sets.");
            }

            // 2. Shell section must reference the existing element set directly
            string expectedSectionLine = $"Elset={expectedElset}";
            bool hasSectionWithElset = lines.Any(l =>
                l.TrimStart().StartsWith("*Shell section", StringComparison.OrdinalIgnoreCase) &&
                l.IndexOf("Composite", StringComparison.OrdinalIgnoreCase) >= 0 &&
                l.IndexOf(expectedSectionLine, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!hasSectionWithElset)
            {
                Console.WriteLine($"  FAIL: No composite shell section with 'Elset={expectedElset}' found.");
                foreach (var l in lines.Where(l =>
                    l.TrimStart().StartsWith("*Shell section", StringComparison.OrdinalIgnoreCase)))
                    Console.WriteLine("        " + l);
                passed = false;
            }
            else
            {
                Console.WriteLine($"  PASS: *Shell section uses Elset={expectedElset}.");
            }

            // 3. Exactly 3 orientation cards (one per ply)
            int oriCount = lines.Count(l =>
                l.TrimStart().StartsWith("*Orientation", StringComparison.OrdinalIgnoreCase) &&
                l.IndexOf("Name=", StringComparison.OrdinalIgnoreCase) >= 0);
            if (oriCount != 3)
            {
                Console.WriteLine($"  FAIL: Expected 3 *Orientation cards, found {oriCount}.");
                passed = false;
            }
            else
            {
                Console.WriteLine($"  PASS: Exactly 3 *Orientation cards.");
            }

            // 4. The shell section has 3 ply data lines
            // Count data lines immediately after the shell section keyword
            bool insideSection = false;
            int plyLineCount = 0;
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("*Shell section", StringComparison.OrdinalIgnoreCase) &&
                    line.IndexOf("Composite", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    line.IndexOf(expectedSectionLine, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    insideSection = true;
                    continue;
                }
                if (insideSection)
                {
                    if (line.TrimStart().StartsWith("*")) break;
                    if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("**"))
                        plyLineCount++;
                }
            }
            if (plyLineCount != 3)
            {
                Console.WriteLine($"  FAIL: Expected 3 ply data lines in shell section, found {plyLineCount}.");
                passed = false;
            }
            else
            {
                Console.WriteLine($"  PASS: Shell section has 3 ply data lines.");
            }

            return passed;
        }
    }
}
