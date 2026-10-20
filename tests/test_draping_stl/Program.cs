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
using CaeModel;
using QuantumConcepts.Formats.StereoLithography;

namespace TestDrapingStl
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                // Parameters (can be overridden via command line: plyAngleDeg [cellSize [limitShear]])
                double plyAngleDeg  = 0.0;
                double cellSize     = 0.0;   // 0 = auto-detect from mesh bbox
                double limitShear   = 45.0;

                if (args.Length >= 1) double.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out plyAngleDeg);
                if (args.Length >= 2) double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out cellSize);
                if (args.Length >= 3) double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out limitShear);

                // Locate STL file
                string stlPath = FindFile("cooperhood_remesh.stl");
                if (stlPath == null)
                {
                    Console.WriteLine("ERROR: cooperhood_remesh.stl not found.");
                    Console.WriteLine("  Searched in assets_composites/ up from exe directory.");
                    Console.WriteLine("  Usage: TestDrapingStl.exe [plyAngleDeg] [cellSize] [limitShear]");
                    return 1;
                }
                Console.WriteLine($"STL : {stlPath}");
                Console.WriteLine($"Params: angle={plyAngleDeg}\u00b0  limitShear={limitShear}\u00b0  cellSize={(cellSize > 0 ? cellSize.ToString("G4") : "auto")}");

                // Read STL
                Console.WriteLine("Reading STL...");
                STLDocument stl;
                using (var stream = File.OpenRead(stlPath))
                    stl = STLDocument.Read(stream, tryBinaryIfTextFailed: true);
                Console.WriteLine($"  Facets: {stl.Facets.Count}");

                // Weld vertices (STL is un-welded, deduplicate shared vertices)
                Console.WriteLine("Welding vertices...");
                double[] coords;
                int[] elemNodeCounts;
                int[] elemNodeIds;
                WeldStl(stl, out coords, out elemNodeCounts, out elemNodeIds);
                int nodeCount = coords.Length / 3;
                Console.WriteLine($"  Unique nodes : {nodeCount}");
                Console.WriteLine($"  Elements     : {elemNodeCounts.Length}");

                // Auto-detect cell size from bounding box if not specified
                if (cellSize <= 0)
                {
                    double xMin = double.MaxValue, xMax = double.MinValue;
                    double yMin = double.MaxValue, yMax = double.MinValue;
                    for (int i = 0; i < nodeCount; i++)
                    {
                        double x = coords[i * 3], y = coords[i * 3 + 1];
                        if (x < xMin) xMin = x; if (x > xMax) xMax = x;
                        if (y < yMin) yMin = y; if (y > yMax) yMax = y;
                    }
                    double shortSide = Math.Min(xMax - xMin, yMax - yMin);
                    cellSize = shortSide / 25.0;
                    Console.WriteLine($"  BBox X: [{xMin:G4} .. {xMax:G4}]  Y: [{yMin:G4} .. {yMax:G4}]");
                    Console.WriteLine($"  Auto cellSize = {cellSize:G4}  (short side / 25)");
                }

                // Run draping simulation
                Console.WriteLine($"Running KinDrape (angle={plyAngleDeg}\u00b0, cellSize={cellSize:G4})...");
                DrapingResult result = KinDrape.Run(coords, elemNodeCounts, elemNodeIds, plyAngleDeg, cellSize);
                if (result == null)
                {
                    Console.WriteLine("ERROR: KinDrape returned null (insufficient geometry).");
                    return 1;
                }

                // Compute statistics
                double maxShear = 0;
                int validCells = 0;
                int nanCells   = 0;
                foreach (double s in result.CellShear)
                {
                    if (double.IsNaN(s)) { nanCells++; continue; }
                    if (s > maxShear) maxShear = s;
                    validCells++;
                }
                Console.WriteLine($"  Grid       : {result.GridRows} x {result.GridCols}");
                Console.WriteLine($"  Total cells: {(result.GridRows - 1) * (result.GridCols - 1)}");
                Console.WriteLine($"  Valid cells: {validCells}  NaN cells: {nanCells}");
                Console.WriteLine($"  Max shear  : {maxShear:F2}°");

                // Write OBJ
                string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                Directory.CreateDirectory(outputDir);
                string objPath = Path.Combine(outputDir, "draping_result.obj");
                WriteObj(objPath, result, limitShear);
                Console.WriteLine($"OBJ written : {objPath}");
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

        // Walk up the directory tree to find assets_composites/cooperhood_remesh.stl
        static string FindFile(string fileName)
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                string candidate = Path.Combine(dir, "assets_composites", fileName);
                if (File.Exists(candidate)) return candidate;
                candidate = Path.Combine(dir, fileName);
                if (File.Exists(candidate)) return candidate;
                string parent = Path.GetDirectoryName(dir);
                if (parent == null || parent == dir) break;
                dir = parent;
            }
            return null;
        }

        // Deduplicate STL vertices using exact float equality
        // (well-formed STL binary files share vertices by identical float values)
        static void WeldStl(STLDocument stl,
            out double[] coords,
            out int[] elemNodeCounts,
            out int[] elemNodeIds)
        {
            var vertexDict  = new Dictionary<(float, float, float), int>();
            var coordList   = new List<double>();
            var countList   = new List<int>();
            var idList      = new List<int>();

            foreach (var facet in stl.Facets)
            {
                if (facet.Vertices.Count < 3) continue;
                countList.Add(facet.Vertices.Count);
                foreach (var v in facet.Vertices)
                {
                    var key = (v.X, v.Y, v.Z);
                    int idx;
                    if (!vertexDict.TryGetValue(key, out idx))
                    {
                        idx = coordList.Count / 3;
                        vertexDict[key] = idx;
                        coordList.Add(v.X);
                        coordList.Add(v.Y);
                        coordList.Add(v.Z);
                    }
                    idList.Add(idx);
                }
            }

            coords        = coordList.ToArray();
            elemNodeCounts = countList.ToArray();
            elemNodeIds   = idList.ToArray();
        }

        // Write OBJ file with vertex colors (supported by MeshLab, Blender, etc.)
        // Uses un-shared vertices so each quad cell gets its own shear color.
        static void WriteObj(string path, DrapingResult result, double limitShear)
        {
            int gridRows = result.GridRows;
            int gridCols = result.GridCols;

            using (var w = new StreamWriter(path, false, System.Text.Encoding.ASCII))
            {
                w.WriteLine("# KinDrape draping result - quad mesh");
                w.WriteLine($"# Grid: {gridRows} x {gridCols}");
                w.WriteLine($"# Vertex color: blue=0°  yellow={limitShear:F0}°  red={(2 * limitShear):F0}°");
                w.WriteLine();

                // Collect valid cells and their quad vertices
                var faces = new List<int>(); // each entry = 1-based vertex index of cell start
                int nextVertex = 1;

                for (int r = 0; r < gridRows - 1; r++)
                {
                    for (int c = 0; c < gridCols - 1; c++)
                    {
                        // Check all 4 corners are valid
                        if (double.IsNaN(result.NodePositions[r,     c,     0]) ||
                            double.IsNaN(result.NodePositions[r,     c + 1, 0]) ||
                            double.IsNaN(result.NodePositions[r + 1, c + 1, 0]) ||
                            double.IsNaN(result.NodePositions[r + 1, c,     0]))
                            continue;

                        int cellNo = r + c * (gridRows - 1);
                        double shear = (cellNo < result.CellShear.Length && !double.IsNaN(result.CellShear[cellNo]))
                            ? result.CellShear[cellNo]
                            : 0.0;

                        double cr, cg, cb;
                        ShearColor(shear, limitShear, out cr, out cg, out cb);

                        // 4 un-shared vertices: bottom-left, bottom-right, top-right, top-left
                        WriteV(w, result.NodePositions, r,     c,     cr, cg, cb);
                        WriteV(w, result.NodePositions, r,     c + 1, cr, cg, cb);
                        WriteV(w, result.NodePositions, r + 1, c + 1, cr, cg, cb);
                        WriteV(w, result.NodePositions, r + 1, c,     cr, cg, cb);

                        faces.Add(nextVertex);
                        nextVertex += 4;
                    }
                }

                w.WriteLine();
                foreach (int vs in faces)
                    w.WriteLine($"f {vs} {vs + 1} {vs + 2} {vs + 3}");
            }
        }

        static void WriteV(StreamWriter w, double[,,] pos, int r, int c, double cr, double cg, double cb)
        {
            w.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "v {0:G8} {1:G8} {2:G8} {3:F3} {4:F3} {5:F3}",
                pos[r, c, 0], pos[r, c, 1], pos[r, c, 2], cr, cg, cb));
        }

        // Map shear angle to RGB: 0=blue, limitShear=yellow, 2*limitShear=red
        static void ShearColor(double shearDeg, double limitShear, out double r, out double g, out double b)
        {
            double t = Math.Max(0, Math.Min(1, shearDeg / (2.0 * limitShear)));
            if (t < 0.5)
            {
                double u = t * 2.0;
                r = u; g = u; b = 1.0 - u;
            }
            else
            {
                double u = (t - 0.5) * 2.0;
                r = 1.0; g = 1.0 - u; b = 0.0;
            }
        }
    }
}
