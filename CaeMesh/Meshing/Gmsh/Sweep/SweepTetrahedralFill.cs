// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CaeGlobals;
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Meshes the cells of a compound that no sweep block covers with tetrahedra, by Netgen, on the surface meshes
    /// the sweep left on their boundaries. A face the cell shares with a swept block carries the quadrilaterals of
    /// the sweep, and Netgen stands a pyramid on every one of them, so the mesh is conformal across the interface.
    ///
    /// The boundaries of all the cells go to one Netgen run, each cell as a domain of its own and each face written
    /// for every cell it bounds, wound outward from that cell. The winding is measured rather than taken from gmsh:
    /// the faces of one shell are wound alike by walking across their edges, and the shell enclosing the most volume
    /// is the outer boundary, which faces outward, while any other shell bounds a cavity and faces into it. Netgen
    /// keeps the boundary points where they are and returns them first, in their input order, so its elements are
    /// joined to the gmsh nodes by index, and the points it adds become nodes of the cell they lie in.
    /// </summary>
    public static class SweepTetrahedralFill
    {
        // Variables
        private const int _numOfReportedLines = 20;
        private const string _inputFileName = "SweepTetrahedralFill.vol";
        private const string _outputFileName = "SweepTetrahedralFillMesh.vol";


        // Methods
        public static void Fill(int[] volumeIds, string[] cellNames, string netgenMesherFileName,
                                string meshParametersFileName, string workDirectory, Action<string> writeOutput)
        {
            if (volumeIds == null || volumeIds.Length == 0) return;
            if (netgenMesherFileName == null || !File.Exists(netgenMesherFileName))
                throw new CaeException("Netgen, which meshes the cells the sweep does not reach, was not found: " +
                                       netgenMesherFileName);
            if (meshParametersFileName == null || !File.Exists(meshParametersFileName))
                throw new CaeException("The mesh parameters of the cells the sweep does not reach were not written.");
            //
            IntPtr[] nodeTags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out nodeTags, out coor, -1, -1, false, false);
            Dictionary<long, int> nodeTagIndex = new Dictionary<long, int>(nodeTags.Length);
            for (int i = 0; i < nodeTags.Length; i++) nodeTagIndex[nodeTags[i].ToInt64()] = i;
            // The boundary of every cell, wound outward
            List<long[]>[] cellFaces = new List<long[]>[volumeIds.Length];
            for (int i = 0; i < volumeIds.Length; i++)
                cellFaces[i] = GetOutwardBoundary(volumeIds[i], cellNames[i], nodeTagIndex, coor);
            // Points numbered in the order they are first met; Netgen numbers from 1
            List<long> pointTags = new List<long>();
            Dictionary<long, int> pointTagNumber = new Dictionary<long, int>();
            foreach (var faces in cellFaces)
            {
                foreach (var face in faces)
                {
                    foreach (var tag in face)
                    {
                        if (!pointTagNumber.ContainsKey(tag))
                        {
                            pointTags.Add(tag);
                            pointTagNumber.Add(tag, pointTags.Count);
                        }
                    }
                }
            }
            //
            string inputFileName = Path.Combine(workDirectory, _inputFileName);
            string outputFileName = Path.Combine(workDirectory, _outputFileName);
            WriteBoundary(inputFileName, cellFaces, pointTags, pointTagNumber, nodeTagIndex, coor);
            //
            writeOutput?.Invoke("Meshing the cells the sweep does not reach by tetrahedra with Netgen: " +
                                String.Join(", ", cellNames) + "...");
            RunNetgen(netgenMesherFileName, inputFileName, outputFileName, meshParametersFileName, workDirectory);
            //
            double[][] points;
            List<int[]>[] cellElements;
            ReadVolumeMesh(outputFileName, volumeIds.Length, out points, out cellElements);
            CheckBoundaryPoints(points, pointTags, nodeTagIndex, coor);
            AddVolumeMesh(volumeIds, cellNames, points, cellElements, pointTags, writeOutput);
            writeOutput?.Invoke("Done.");
        }
        private static List<long[]> GetOutwardBoundary(int volumeId, string cellName, Dictionary<long, int> nodeTagIndex,
                                                       double[] coor)
        {
            Tuple<int, int>[] surfaceDimTags;
            Gmsh.Model.GetBoundary(new Tuple<int, int>[] { new Tuple<int, int>(3, volumeId) }, out surfaceDimTags,
                                   false, false, false);
            //
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] elementNodeTags;
            int numOfNodes;
            long[] face;
            List<long[]> faces = new List<long[]>();
            List<int> faceSurfaceIds = new List<int>();
            foreach (var surfaceDimTag in surfaceDimTags)
            {
                int surfaceId = Math.Abs(surfaceDimTag.Item2);
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out elementNodeTags, 2, surfaceId);
                if (elementTypes.Length == 0)
                    throw new CaeException("The cell " + cellName + " cannot be meshed by tetrahedra: surface " +
                                           surfaceId + " carries no mesh.");
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    if (elementTypes[i] == (int)GmshElementType.LinearTriangle) numOfNodes = 3;
                    else if (elementTypes[i] == (int)GmshElementType.LinearQuadrilateral) numOfNodes = 4;
                    else throw new CaeException("The cell " + cellName + " cannot be meshed by tetrahedra: surface " +
                                                surfaceId + " carries elements of gmsh type " + elementTypes[i] +
                                                ", and only linear triangles and quadrilaterals can bound the tetrahedra.");
                    //
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        face = new long[numOfNodes];
                        for (int k = 0; k < numOfNodes; k++) face[k] = elementNodeTags[i][numOfNodes * j + k].ToInt64();
                        faces.Add(face);
                        faceSurfaceIds.Add(surfaceId);
                    }
                }
            }
            if (faces.Count == 0)
                throw new CaeException("The cell " + cellName + " has no boundary mesh to mesh it by tetrahedra from.");
            //
            OrientShells(faces, faceSurfaceIds, cellName, nodeTagIndex, coor);
            return faces;
        }
        /// <summary>
        /// Winds the faces of every shell alike, walking from face to face across the edges they share - a face
        /// crosses a shared edge in the direction opposite to its neighbour - and then turns every shell to face out
        /// of the cell by the sign of the volume it encloses.
        /// </summary>
        private static void OrientShells(List<long[]> faces, List<int> faceSurfaceIds, string cellName,
                                         Dictionary<long, int> nodeTagIndex, double[] coor)
        {
            Dictionary<(long, long), List<int>> edgeFaces = new Dictionary<(long, long), List<int>>();
            List<int> edgeFaceIds;
            (long, long) key;
            for (int i = 0; i < faces.Count; i++)
            {
                for (int j = 0; j < faces[i].Length; j++)
                {
                    key = GetEdgeKey(faces[i][j], faces[i][(j + 1) % faces[i].Length]);
                    if (!edgeFaces.TryGetValue(key, out edgeFaceIds))
                    {
                        edgeFaceIds = new List<int>(2);
                        edgeFaces.Add(key, edgeFaceIds);
                    }
                    edgeFaceIds.Add(i);
                }
            }
            foreach (var entry in edgeFaces)
            {
                if (entry.Value.Count != 2)
                {
                    string surfaceNames = String.Join(", ", entry.Value.Select(faceId => faceSurfaceIds[faceId])
                                                                       .Distinct().Select(id => "surface " + id));
                    throw new CaeException("The cell " + cellName + " cannot be meshed by tetrahedra: the mesh of its " +
                                           "boundary is not closed, an element edge on " + surfaceNames + " lies on " +
                                           entry.Value.Count + " surface elements instead of 2.");
                }
            }
            //
            bool[] visited = new bool[faces.Count];
            List<List<int>> shells = new List<List<int>>();
            Stack<int> stack = new Stack<int>();
            for (int seed = 0; seed < faces.Count; seed++)
            {
                if (visited[seed]) continue;
                //
                List<int> shell = new List<int>();
                visited[seed] = true;
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    int faceId = stack.Pop();
                    long[] face = faces[faceId];
                    shell.Add(faceId);
                    for (int j = 0; j < face.Length; j++)
                    {
                        long a = face[j];
                        long b = face[(j + 1) % face.Length];
                        foreach (var neighbourId in edgeFaces[GetEdgeKey(a, b)])
                        {
                            if (visited[neighbourId]) continue;
                            if (Crosses(faces[neighbourId], a, b)) Array.Reverse(faces[neighbourId]);
                            visited[neighbourId] = true;
                            stack.Push(neighbourId);
                        }
                    }
                }
                shells.Add(shell);
            }
            //
            double[] shellVolumes = shells.Select(shell => GetEnclosedVolume(faces, shell, nodeTagIndex, coor)).ToArray();
            int outerShell = 0;
            for (int i = 1; i < shells.Count; i++)
            {
                if (Math.Abs(shellVolumes[i]) > Math.Abs(shellVolumes[outerShell])) outerShell = i;
            }
            for (int i = 0; i < shells.Count; i++)
            {
                if ((shellVolumes[i] > 0) != (i == outerShell))
                {
                    foreach (var faceId in shells[i]) Array.Reverse(faces[faceId]);
                }
            }
        }
        private static (long, long) GetEdgeKey(long a, long b)
        {
            return a < b ? (a, b) : (b, a);
        }
        private static bool Crosses(long[] face, long a, long b)
        {
            for (int j = 0; j < face.Length; j++)
            {
                if (face[j] == a && face[(j + 1) % face.Length] == b) return true;
            }
            return false;
        }
        private static double GetEnclosedVolume(List<long[]> faces, List<int> shell, Dictionary<long, int> nodeTagIndex,
                                                double[] coor)
        {
            double volume = 0;
            foreach (var faceId in shell)
            {
                long[] face = faces[faceId];
                int p0 = 3 * nodeTagIndex[face[0]];
                for (int j = 1; j < face.Length - 1; j++)
                {
                    int p1 = 3 * nodeTagIndex[face[j]];
                    int p2 = 3 * nodeTagIndex[face[j + 1]];
                    volume += coor[p0] * (coor[p1 + 1] * coor[p2 + 2] - coor[p1 + 2] * coor[p2 + 1]) +
                              coor[p0 + 1] * (coor[p1 + 2] * coor[p2] - coor[p1] * coor[p2 + 2]) +
                              coor[p0 + 2] * (coor[p1] * coor[p2 + 1] - coor[p1 + 1] * coor[p2]);
                }
            }
            return volume / 6;
        }
        /// <summary>
        /// Writes the boundary in the Netgen vol format: the points first, since a surface element marks its points as
        /// boundary points only when they already exist, and a boundary point is one volume meshing never moves. A
        /// face is written once for every cell it bounds, the cell as the domain inside it and nothing outside, so a
        /// face two cells share is written twice, the second time wound the other way.
        /// </summary>
        private static void WriteBoundary(string fileName, List<long[]>[] cellFaces, List<long> pointTags,
                                          Dictionary<long, int> pointTagNumber, Dictionary<long, int> nodeTagIndex,
                                          double[] coor)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("mesh3d");
            sb.AppendLine("dimension");
            sb.AppendLine("3");
            sb.AppendLine("geomtype");
            sb.AppendLine("0");
            sb.AppendLine();
            sb.AppendLine("points");
            sb.AppendLine(pointTags.Count.ToString(culture));
            foreach (var tag in pointTags)
            {
                int index = 3 * nodeTagIndex[tag];
                sb.Append(coor[index].ToString("R", culture)).Append(' ')
                  .Append(coor[index + 1].ToString("R", culture)).Append(' ')
                  .AppendLine(coor[index + 2].ToString("R", culture));
            }
            sb.AppendLine();
            sb.AppendLine("surfaceelements");
            sb.AppendLine(cellFaces.Sum(faces => faces.Count).ToString(culture));
            for (int i = 0; i < cellFaces.Length; i++)
            {
                int domain = i + 1;
                foreach (var face in cellFaces[i])
                {
                    // surface number, boundary condition, domain inside, domain outside, number of points
                    sb.Append(domain).Append(" 1 ").Append(domain).Append(" 0 ").Append(face.Length);
                    foreach (var tag in face) sb.Append(' ').Append(pointTagNumber[tag]);
                    sb.AppendLine();
                }
            }
            sb.AppendLine();
            sb.AppendLine("endmesh");
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        private static void RunNetgen(string netgenMesherFileName, string inputFileName, string outputFileName,
                                      string meshParametersFileName, string workDirectory)
        {
            if (File.Exists(outputFileName)) File.Delete(outputFileName);
            //
            Queue<string> lastLines = new Queue<string>();
            object lastLinesLock = new object();
            DataReceivedEventHandler keepLine = (sender, e) =>
            {
                if (e.Data == null) return;
                lock (lastLinesLock)
                {
                    lastLines.Enqueue(e.Data);
                    if (lastLines.Count > _numOfReportedLines) lastLines.Dequeue();
                }
            };
            string argument = "VOL_VOLUME_MESH \"" + inputFileName + "\" \"" + outputFileName + "\" \"" +
                              meshParametersFileName + "\"";
            ProcessStartInfo startInfo = new ProcessStartInfo(netgenMesherFileName, argument)
            {
                WorkingDirectory = workDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            int exitCode;
            using (Process process = new Process() { StartInfo = startInfo })
            {
                process.OutputDataReceived += keepLine;
                process.ErrorDataReceived += keepLine;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
            //
            if (exitCode != 0 || !File.Exists(outputFileName))
            {
                string output;
                lock (lastLinesLock) output = String.Join(Environment.NewLine, lastLines.Where(line => line.Trim() != ""));
                throw new CaeException("Netgen could not mesh the cells the sweep does not reach by tetrahedra. Its last " +
                                       "messages:" + Environment.NewLine + output);
            }
        }
        private static void ReadVolumeMesh(string fileName, int numOfCells, out double[][] points,
                                           out List<int[]>[] cellElements)
        {
            points = null;
            cellElements = new List<int[]>[numOfCells];
            for (int i = 0; i < numOfCells; i++) cellElements[i] = new List<int[]>();
            //
            CultureInfo culture = CultureInfo.InvariantCulture;
            char[] separators = new char[] { ' ', '\t' };
            string[] lines = File.ReadAllLines(fileName);
            string[] values;
            int count;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == "points")
                {
                    count = int.Parse(lines[++i].Trim(), culture);
                    points = new double[count][];
                    for (int j = 0; j < count; j++)
                    {
                        values = lines[++i].Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        points[j] = new double[] { double.Parse(values[0], culture), double.Parse(values[1], culture),
                                                   double.Parse(values[2], culture) };
                    }
                }
                else if (line == "volumeelements")
                {
                    count = int.Parse(lines[++i].Trim(), culture);
                    for (int j = 0; j < count; j++)
                    {
                        // domain, number of points, points
                        values = lines[++i].Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        int domain = int.Parse(values[0], culture);
                        int numOfPoints = int.Parse(values[1], culture);
                        if (domain < 1 || domain > numOfCells)
                            throw new CaeException("Netgen returned an element of an unknown cell " + domain + ".");
                        int[] element = new int[numOfPoints];
                        for (int k = 0; k < numOfPoints; k++) element[k] = int.Parse(values[2 + k], culture);
                        cellElements[domain - 1].Add(element);
                    }
                }
            }
            if (points == null) throw new CaeException("The Netgen tetrahedral mesh holds no points.");
        }
        /// <summary>
        /// The elements are joined to the gmsh nodes by index, so a boundary point Netgen moved or renumbered would
        /// join them to the wrong nodes; that is checked rather than trusted.
        /// </summary>
        private static void CheckBoundaryPoints(double[][] points, List<long> pointTags, Dictionary<long, int> nodeTagIndex,
                                                double[] coor)
        {
            if (points.Length < pointTags.Count)
                throw new CaeException("Netgen returned fewer points than the boundary of the tetrahedral cells holds.");
            //
            double[] min = new double[] { double.MaxValue, double.MaxValue, double.MaxValue };
            double[] max = new double[] { -double.MaxValue, -double.MaxValue, -double.MaxValue };
            for (int i = 0; i < pointTags.Count; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    min[j] = Math.Min(min[j], points[i][j]);
                    max[j] = Math.Max(max[j], points[i][j]);
                }
            }
            double tolerance = 1E-9 * Math.Sqrt(Math.Pow(max[0] - min[0], 2) + Math.Pow(max[1] - min[1], 2) +
                                                Math.Pow(max[2] - min[2], 2));
            for (int i = 0; i < pointTags.Count; i++)
            {
                int index = 3 * nodeTagIndex[pointTags[i]];
                for (int j = 0; j < 3; j++)
                {
                    if (Math.Abs(points[i][j] - coor[index + j]) > tolerance)
                        throw new CaeException("Netgen moved a boundary point of the tetrahedral cells, so their mesh " +
                                               "cannot be joined to the sweep.");
                }
            }
        }
        /// <summary>
        /// Adds the points Netgen made as nodes of the cell whose elements use them - two cells meet only on their
        /// boundaries, so an added point lies in one cell - and the elements in gmsh node order. Netgen winds its
        /// elements the other way round from gmsh, which was measured on its tetrahedra and pyramids: every one of
        /// them has a negative volume in gmsh order.
        /// </summary>
        private static void AddVolumeMesh(int[] volumeIds, string[] cellNames, double[][] points,
                                          List<int[]>[] cellElements, List<long> pointTags, Action<string> writeOutput)
        {
            int numOfBoundaryPoints = pointTags.Count;
            IntPtr maxNodeTag;
            IntPtr maxElementTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxNodeTag);
            Gmsh.Model.Mesh.GetMaxElementTag(out maxElementTag);
            long firstNewNodeTag = maxNodeTag.ToInt64() + 1;
            long nextElementTag = maxElementTag.ToInt64() + 1;
            Func<int, long> getNodeTag = pointNumber => pointNumber <= numOfBoundaryPoints
                                                        ? pointTags[pointNumber - 1]
                                                        : firstNewNodeTag + pointNumber - numOfBoundaryPoints - 1;
            //
            for (int i = 0; i < volumeIds.Length; i++)
            {
                HashSet<int> newPointNumbers = new HashSet<int>();
                foreach (var element in cellElements[i])
                {
                    foreach (var pointNumber in element)
                    {
                        if (pointNumber > numOfBoundaryPoints) newPointNumbers.Add(pointNumber);
                    }
                }
                if (newPointNumbers.Count > 0)
                {
                    int[] numbers = newPointNumbers.OrderBy(number => number).ToArray();
                    IntPtr[] tags = new IntPtr[numbers.Length];
                    double[] positions = new double[3 * numbers.Length];
                    for (int j = 0; j < numbers.Length; j++)
                    {
                        tags[j] = new IntPtr(getNodeTag(numbers[j]));
                        positions[3 * j] = points[numbers[j] - 1][0];
                        positions[3 * j + 1] = points[numbers[j] - 1][1];
                        positions[3 * j + 2] = points[numbers[j] - 1][2];
                    }
                    Gmsh.Model.Mesh.AddNodes(3, volumeIds[i], tags, positions);
                }
                //
                List<IntPtr> tetrahedronTags = new List<IntPtr>();
                List<IntPtr> tetrahedronNodes = new List<IntPtr>();
                List<IntPtr> pyramidTags = new List<IntPtr>();
                List<IntPtr> pyramidNodes = new List<IntPtr>();
                List<IntPtr> prismTags = new List<IntPtr>();
                List<IntPtr> prismNodes = new List<IntPtr>();
                int[] order;
                foreach (var element in cellElements[i])
                {
                    if (element.Length == 4)
                    {
                        order = new int[] { 0, 2, 1, 3 };
                        tetrahedronTags.Add(new IntPtr(nextElementTag++));
                        foreach (var k in order) tetrahedronNodes.Add(new IntPtr(getNodeTag(element[k])));
                    }
                    else if (element.Length == 5)
                    {
                        order = new int[] { 0, 3, 2, 1, 4 };
                        pyramidTags.Add(new IntPtr(nextElementTag++));
                        foreach (var k in order) pyramidNodes.Add(new IntPtr(getNodeTag(element[k])));
                    }
                    else if (element.Length == 6)
                    {
                        order = new int[] { 0, 2, 1, 3, 5, 4 };
                        prismTags.Add(new IntPtr(nextElementTag++));
                        foreach (var k in order) prismNodes.Add(new IntPtr(getNodeTag(element[k])));
                    }
                    else throw new CaeException("Netgen returned an element with " + element.Length + " nodes.");
                }
                AddElementsOfOneType(volumeIds[i], (int)GmshElementType.LinearTetra, tetrahedronTags, tetrahedronNodes);
                AddElementsOfOneType(volumeIds[i], (int)GmshElementType.LinearPyramid, pyramidTags, pyramidNodes);
                AddElementsOfOneType(volumeIds[i], (int)GmshElementType.LinearWedge, prismTags, prismNodes);
                //
                string counts = tetrahedronTags.Count + " tetrahedra, " + pyramidTags.Count + " pyramids";
                if (prismTags.Count > 0) counts += ", " + prismTags.Count + " prisms";
                writeOutput?.Invoke(cellNames[i] + ": " + counts + ".");
            }
        }
        private static void AddElementsOfOneType(int volumeId, int elementType, List<IntPtr> tags, List<IntPtr> nodes)
        {
            if (tags.Count == 0) return;
            //
            Gmsh.Model.Mesh.AddElements(3, volumeId, new int[] { elementType }, new IntPtr[][] { tags.ToArray() },
                                        new IntPtr[][] { nodes.ToArray() });
        }
    }
}
