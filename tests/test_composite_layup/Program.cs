using System;
using System.Collections.Generic;
using System.IO;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using FileInOut.Output;
using FileInOut.Output.Calculix;

namespace TestCompositeLayup
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                Directory.CreateDirectory(outputDir);
                string outputFile = Path.Combine(outputDir, "composite_layup.inp");

                Console.WriteLine("Building half-cylinder + flat plate mesh...");
                FeMesh mesh = BuildMesh();
                Console.WriteLine($"  Nodes   : {mesh.Nodes.Count}");
                Console.WriteLine($"  Elements: {mesh.Elements.Count}");
                Console.WriteLine($"  Parts   : {mesh.Parts.Count}");

                Console.WriteLine("Building composite layup model...");
                FeModel model = BuildModel(mesh);

                Console.WriteLine($"Writing INP file: {outputFile}");
                CalculixFileWriter.Write(outputFile, model, ConvertPyramidsToEnum.Wedges);

                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        // Mesh: flat plate (12 S4R elements, Z=0) + half-cylinder (12 S4R elements, curved)
        //
        //  Flat plate layout (nodes 1..20, elements 1..12):
        //    x: 0,1,2,3,4   y: 0,1,2,3   z: 0
        //    node id = y*5 + x + 1
        //
        //  Half-cylinder (nodes 21..40, elements 13..24):
        //    theta: 0,45,90,135,180 deg   y: 0,1,2,3   pos: (cos(t), y, sin(t))
        //    node id = 20 + iy*5 + itheta + 1
        //
        static FeMesh BuildMesh()
        {
            var nodes    = new Dictionary<int, FeNode>();
            var elements = new Dictionary<int, FeElement>();

            // Flat plate nodes (ids 1..20)
            for (int iy = 0; iy <= 3; iy++)
                for (int ix = 0; ix <= 4; ix++)
                    nodes.Add(iy * 5 + ix + 1, new FeNode(iy * 5 + ix + 1, ix, iy, 0.0));

            // Flat plate elements (ids 1..12) — CCW ordering → normal = +Z
            for (int iy = 0; iy <= 2; iy++)
            {
                for (int ix = 0; ix <= 3; ix++)
                {
                    int eid = iy * 4 + ix + 1;
                    int n0  = iy * 5 + ix + 1;
                    int n1  = iy * 5 + ix + 2;
                    int n2  = (iy + 1) * 5 + ix + 2;
                    int n3  = (iy + 1) * 5 + ix + 1;
                    elements.Add(eid, new LinearQuadrilateralElement(eid, new int[] { n0, n1, n2, n3 }));
                }
            }

            // Half-cylinder nodes (ids 21..40)
            double[] thetas = { 0.0, 45.0, 90.0, 135.0, 180.0 };
            for (int iy = 0; iy <= 3; iy++)
            {
                for (int it = 0; it <= 4; it++)
                {
                    double rad = thetas[it] * Math.PI / 180.0;
                    int nodeId  = 20 + iy * 5 + it + 1;
                    nodes.Add(nodeId, new FeNode(nodeId, Math.Cos(rad), iy, Math.Sin(rad)));
                }
            }

            // Half-cylinder elements (ids 13..24)
            for (int iy = 0; iy <= 2; iy++)
            {
                for (int it = 0; it <= 3; it++)
                {
                    int eid = 12 + iy * 4 + it + 1;
                    int n0  = 20 + iy * 5 + it + 1;
                    int n1  = 20 + iy * 5 + it + 2;
                    int n2  = 20 + (iy + 1) * 5 + it + 2;
                    int n3  = 20 + (iy + 1) * 5 + it + 1;
                    elements.Add(eid, new LinearQuadrilateralElement(eid, new int[] { n0, n1, n2, n3 }));
                }
            }

            // Single InpElementSet groups all elements under the S4R type
            var allIds = new HashSet<int>(elements.Keys);
            var inpSets = new List<InpElementSet>
            {
                new InpElementSet("S4R", new HashSet<string> { "S4R" }, allIds)
            };

            FeMesh mesh = new FeMesh(nodes, elements, MeshRepresentation.Mesh, inpSets);

            // Element sets used as ply regions
            var flatIds = new List<int>();
            for (int i = 1; i <= 12; i++) flatIds.Add(i);

            var cylIds = new List<int>();
            for (int i = 13; i <= 24; i++) cylIds.Add(i);

            var fullIds = new List<int>();
            fullIds.AddRange(flatIds);
            fullIds.AddRange(cylIds);

            mesh.ElementSets.Add("Region_Full", new FeElementSet("Region_Full", fullIds.ToArray()));
            mesh.ElementSets.Add("Region_Flat", new FeElementSet("Region_Flat", flatIds.ToArray()));
            mesh.ElementSets.Add("Region_Cyl",  new FeElementSet("Region_Cyl",  cylIds.ToArray()));

            return mesh;
        }

        static FeModel BuildModel(FeMesh mesh)
        {
            UnitSystem unitSystem = new UnitSystem(UnitSystemType.MM_TON_S_C);
            unitSystem.SetConverterUnits();

            FeModel model = new FeModel("CompositeHalfCylinder", unitSystem, true);
            model.SetMesh(mesh);
            model.Properties.ModelSpace = ModelSpaceEnum.ThreeD;

            // Carbon fibre / epoxy lamina material
            double[][] laminaData = new double[][]
            {
                new double[] { 150000.0, 10000.0, 0.3, 5000.0, 5000.0, 3500.0, 20.0 }
            };
            Lamina laminaProp = new Lamina(laminaData);
            Material mat = new Material("CarbonEpoxy");
            mat.AddProperty(laminaProp);
            model.Materials.Add(mat.Name, mat);

            // Quasi-isotropic layup [0/45/-45/90]:
            //   Ply-0 (  0 deg): full mesh (flat + cylinder)
            //   Ply-1 ( 45 deg): flat plate only
            //   Ply-2 (-45 deg): flat plate only
            //   Ply-3 ( 90 deg): full mesh (flat + cylinder)
            //
            // This produces two ITAs:
            //   ITA_1 — cylinder elements (covered by Ply-0 and Ply-3)
            //           → 4 normal groups (θ = 22.5°, 67.5°, 112.5°, 157.5°)
            //   ITA_2 — flat elements (covered by Ply-0, 1, 2, 3)
            //           → 1 normal group (normal = +Z)
            CompositeLayup layup = new CompositeLayup("Layup_QI", 4, CompositeElementType.ConventionalShell);
            layup.Plies[0] = new Ply("Ply-0", "Region_Full", mat.Name, 0.25, "<layup>",   0.0);
            layup.Plies[1] = new Ply("Ply-1", "Region_Flat", mat.Name, 0.25, "<layup>",  45.0);
            layup.Plies[2] = new Ply("Ply-2", "Region_Flat", mat.Name, 0.25, "<layup>", -45.0);
            layup.Plies[3] = new Ply("Ply-3", "Region_Full", mat.Name, 0.25, "<layup>",  90.0);
            model.CompositeLayups.Add(layup.Name, layup);

            return model;
        }
    }
}
