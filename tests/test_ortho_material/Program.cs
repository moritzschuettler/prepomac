using System;
using System.IO;
using CaeGlobals;
using CaeModel;
using FileInOut.Output;

namespace TestOrthoMaterial
{
    /// <summary>
    /// Test program for orthotropic lamina material type.
    /// Creates a model with a lamina material, generates a minimal shell mesh,
    /// exports to INP file and verifies the material card is correctly written.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("Test: Orthotropic Lamina Material INP Export");
            Console.WriteLine("==============================================");
            Console.WriteLine();

            try
            {
                // Define paths
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string outputDir = Path.Combine(baseDir, "output");
                string outputInpFile = Path.Combine(outputDir, "test_lamina_material.inp");

                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                Console.WriteLine($"Output directory: {outputDir}");
                Console.WriteLine($"Output INP file: {outputInpFile}");
                Console.WriteLine();


                // Run the test
                bool success = TestLaminaMaterialExport(outputInpFile);

                if (success)
                {
                    Console.WriteLine();
                    Console.WriteLine("==============================================");
                    Console.WriteLine("TEST PASSED: Lamina material card is correct!");
                    Console.WriteLine("==============================================");
                    return 0;
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("==============================================");
                    Console.WriteLine("TEST FAILED: Material card verification failed");
                    Console.WriteLine("==============================================");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("==============================================");
                Console.WriteLine($"TEST ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine("==============================================");
                return 2;
            }
        }

        /// <summary>
        /// Tests the lamina material export to INP format.
        /// </summary>
        static bool TestLaminaMaterialExport(string outputInpFile)
        {
            // Define lamina material properties (typical carbon fiber/epoxy)
            // Note: Values are in MPa for the MM_TON_S_C unit system
            // The EquationContainer stores values internally and the output will be in MPa
            // Properties: E1, E2, Nu12, G12, G13, G23, Temperature
            double e1 = 150000.0;   // MPa - Young's modulus in fiber direction (150 GPa)
            double e2 = 10000.0;    // MPa - Young's modulus transverse to fiber (10 GPa)
            double nu12 = 0.3;      // Poisson's ratio (dimensionless)
            double g12 = 5000.0;    // MPa - In-plane shear modulus (5 GPa)
            double g13 = 5000.0;    // MPa - Out-of-plane shear modulus (1-3) (5 GPa)
            double g23 = 3500.0;    // MPa - Out-of-plane shear modulus (2-3) (3.5 GPa)
            double temp = 20.0;     // Temperature (room temperature)

            Console.WriteLine("Step 1: Creating FeModel with unit system...");

            // IMPORTANT: Create unit system FIRST so that the pressure converter
            // uses MPa when we create the Lamina material
            UnitSystem unitSystem = new UnitSystem(UnitSystemType.MM_TON_S_C);

            // Explicitly set converter units to ensure static converters use MPa
            unitSystem.SetConverterUnits();

            // Create FeModel
            FeModel model = new FeModel("TestLaminaModel", unitSystem, true);

            Console.WriteLine($"  Unit system: MM_TON_S_C (mm, ton, s, C)");
            Console.WriteLine($"  Pressure unit: MPa");
            Console.WriteLine();

            Console.WriteLine("Step 2: Creating lamina material...");
            Console.WriteLine($"  E1 = {e1} MPa (fiber direction)");
            Console.WriteLine($"  E2 = {e2} MPa (transverse)");
            Console.WriteLine($"  Nu12 = {nu12}");
            Console.WriteLine($"  G12 = {g12} MPa");
            Console.WriteLine($"  G13 = {g13} MPa");
            Console.WriteLine($"  G23 = {g23} MPa");
            Console.WriteLine($"  Temperature = {temp} C");
            Console.WriteLine();

            // Create the lamina material AFTER the model/unit system is set up
            // so that values are stored in the correct units (MPa)
            double[][] laminaData = new double[][]
            {
                new double[] { e1, e2, nu12, g12, g13, g23, temp }
            };

            Lamina laminaProperty = new Lamina(laminaData);
            Material carbonEpoxyMaterial = new Material("CarbonEpoxy_T300");
            carbonEpoxyMaterial.AddProperty(laminaProperty);

            // Add the material to the model
            model.Materials.Add(carbonEpoxyMaterial.Name, carbonEpoxyMaterial);

            Console.WriteLine($"  Material '{carbonEpoxyMaterial.Name}' added to model");
            Console.WriteLine();

            Console.WriteLine("Step 3: Exporting materials to INP file...");

            // Export only materials to INP file (simpler test, no mesh required)
            string[] materialNames = new string[] { carbonEpoxyMaterial.Name };
            CalculixFileWriter.WriteMaterials(outputInpFile, model, materialNames);

            Console.WriteLine($"  INP file written: {outputInpFile}");
            Console.WriteLine();

            Console.WriteLine("Step 4: Verifying material card in INP file...");

            // Read and verify the INP file
            bool verified = VerifyMaterialCard(outputInpFile, e1, e2, nu12, g12, g13, g23);

            return verified;
        }

        /// <summary>
        /// Verifies that the material card in the INP file matches expected values.
        /// For lamina materials, PrePoMax exports as *Elastic, TYPE=ENGINEERING CONSTANTS with 9 values:
        /// E1, E2, E3(=E2), Nu12, Nu13(=Nu12), Nu23(=Nu12), G12, G13, G23
        /// </summary>
        static bool VerifyMaterialCard(string inpFile, double e1, double e2, double nu12,
                                       double g12, double g13, double g23)
        {
            string content = File.ReadAllText(inpFile);
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            bool foundMaterial = false;
            bool foundElasticOrtho = false;
            bool verifiedValues = false;

            // Expected values based on CalLamina.cs logic
            double expectedE3 = e2;      // E3 = E2 (transverse isotropy)
            double expectedNu13 = nu12;  // Nu13 = Nu12
            double expectedNu23 = nu12;  // Nu23 = Nu12

            Console.WriteLine("  Searching for material definition...");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Find material definition
                if (line.StartsWith("*Material", StringComparison.OrdinalIgnoreCase) &&
                    line.IndexOf("CarbonEpoxy_T300", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundMaterial = true;
                    Console.WriteLine($"  Found: {line}");
                    continue;
                }

                // Find elastic ortho definition after material
                if (foundMaterial && line.StartsWith("*Elastic", StringComparison.OrdinalIgnoreCase) &&
                    line.IndexOf("TYPE=ENGINEERING CONSTANTS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foundElasticOrtho = true;
                    Console.WriteLine($"  Found: {line}");

                    // Next line should contain the values
                    if (i + 1 < lines.Length)
                    {
                        string valuesLine = lines[i + 1].Trim();
                        Console.WriteLine($"  Values: {valuesLine}");

                        // Parse the values
                        string[] values = valuesLine.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        if (values.Length >= 9)
                        {
                            // Parse and verify each value
                            double parsedE1 = ParseCalculixValue(values[0]);
                            double parsedE2 = ParseCalculixValue(values[1]);
                            double parsedE3 = ParseCalculixValue(values[2]);
                            double parsedNu12 = ParseCalculixValue(values[3]);
                            double parsedNu13 = ParseCalculixValue(values[4]);
                            double parsedNu23 = ParseCalculixValue(values[5]);
                            double parsedG12 = ParseCalculixValue(values[6]);
                            double parsedG13 = ParseCalculixValue(values[7]);
                            double parsedG23 = ParseCalculixValue(values[8]);

                            Console.WriteLine();
                            Console.WriteLine("  Verification of values:");
                            Console.WriteLine($"    E1:   Expected={e1}, Found={parsedE1}, Match={IsClose(e1, parsedE1)}");
                            Console.WriteLine($"    E2:   Expected={e2}, Found={parsedE2}, Match={IsClose(e2, parsedE2)}");
                            Console.WriteLine($"    E3:   Expected={expectedE3}, Found={parsedE3}, Match={IsClose(expectedE3, parsedE3)}");
                            Console.WriteLine($"    Nu12: Expected={nu12}, Found={parsedNu12}, Match={IsClose(nu12, parsedNu12)}");
                            Console.WriteLine($"    Nu13: Expected={expectedNu13}, Found={parsedNu13}, Match={IsClose(expectedNu13, parsedNu13)}");
                            Console.WriteLine($"    Nu23: Expected={expectedNu23}, Found={parsedNu23}, Match={IsClose(expectedNu23, parsedNu23)}");
                            Console.WriteLine($"    G12:  Expected={g12}, Found={parsedG12}, Match={IsClose(g12, parsedG12)}");
                            Console.WriteLine($"    G13:  Expected={g13}, Found={parsedG13}, Match={IsClose(g13, parsedG13)}");
                            Console.WriteLine($"    G23:  Expected={g23}, Found={parsedG23}, Match={IsClose(g23, parsedG23)}");

                            verifiedValues = IsClose(e1, parsedE1) &&
                                             IsClose(e2, parsedE2) &&
                                             IsClose(expectedE3, parsedE3) &&
                                             IsClose(nu12, parsedNu12) &&
                                             IsClose(expectedNu13, parsedNu13) &&
                                             IsClose(expectedNu23, parsedNu23) &&
                                             IsClose(g12, parsedG12) &&
                                             IsClose(g13, parsedG13) &&
                                             IsClose(g23, parsedG23);
                        }
                        else
                        {
                            Console.WriteLine($"  ERROR: Expected 9 values, found {values.Length}");
                        }
                    }
                    break;
                }
            }

            if (!foundMaterial)
            {
                Console.WriteLine("  ERROR: Material 'CarbonEpoxy_T300' not found in INP file");
                return false;
            }

            if (!foundElasticOrtho)
            {
                Console.WriteLine("  ERROR: *Elastic, TYPE=ENGINEERING CONSTANTS not found for the material");
                return false;
            }

            return verifiedValues;
        }

        /// <summary>
        /// Parses a CalculiX formatted number (handles scientific notation).
        /// </summary>
        static double ParseCalculixValue(string value)
        {
            value = value.Trim();
            // CalculiX may use D for exponent instead of E
            value = value.Replace("D", "E").Replace("d", "e");
            return double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Checks if two doubles are close within a relative tolerance.
        /// </summary>
        static bool IsClose(double expected, double actual, double relTol = 1e-6)
        {
            if (expected == 0)
            {
                return Math.Abs(actual) < relTol;
            }
            return Math.Abs((expected - actual) / expected) < relTol;
        }
    }
}
