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
using FileInOut.Input;
using FileInOut.Output;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PrePoMax
{
    internal class QuadWildMesherRunner : IMesherRunner
    {
        // Variables
        private const double DegenerateFaceAreaFactor = 1E-10;
        //
        private Controller _controller;
        private NetgenMesherRunner _netgenRunner;
        private MmgMesherRunner _mmgRunner;
        private GmshMesherRunner _gmshRunner;


        // Properties
        public MesherTypeEnum MesherType { get { return MesherTypeEnum.QuadWild; } }


        // Constructors
        public QuadWildMesherRunner(Controller controller, NetgenMesherRunner netgenRunner,
                                    MmgMesherRunner mmgRunner, GmshMesherRunner gmshRunner)
        {
            _controller = controller;
            _netgenRunner = netgenRunner;
            _mmgRunner = mmgRunner;
            _gmshRunner = gmshRunner;
        }


        // Methods
        public bool CanCreateMeshFromBrep(GeometryPart part)
        {
            return part != null && part.PartType == PartType.Shell;
        }
        public bool CanCreateMeshFromStl(GeometryPart part)
        {
            return part != null && part.PartType == PartType.Shell;
        }
        public bool PreviewEdgeMeshFromBrep(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                            OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            return PreviewEdgeNodes(part, mesherSetupItems, meshSetupItems);
        }
        public bool PreviewEdgeMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                           OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            return PreviewEdgeNodes(part, mesherSetupItems, meshSetupItems);
        }
        public GeneratedMeshData CreateMeshFromBrep(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                                    string workDirectory)
        {
            return CreateMesh(part, mesherSetupItems, workDirectory);
        }
        public GeneratedMeshData CreateMeshFromStl(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                                   string workDirectory)
        {
            return CreateMesh(part, mesherSetupItems, workDirectory);
        }
        private GeneratedMeshData CreateMesh(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                             string workDirectory)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (part.PartType != PartType.Shell)
                throw new CaeException("QuadWild can only mesh shell geometry parts.");
            ShellQuadWild shellQuadWild = GetShellQuadWild(mesherSetupItems);
            if (String.IsNullOrWhiteSpace(workDirectory) || !Directory.Exists(workDirectory))
                throw new CaeException("The QuadWild work directory does not exist.");
            if (Double.IsNaN(shellQuadWild.ScaleFactor) || Double.IsInfinity(shellQuadWild.ScaleFactor) ||
                shellQuadWild.ScaleFactor <= 0)
                throw new CaeException("The QuadWild quad edge multiplier must be a positive finite number.");
            if (Double.IsNaN(shellQuadWild.Alpha) || Double.IsInfinity(shellQuadWild.Alpha) ||
                shellQuadWild.Alpha < 0 || shellQuadWild.Alpha > 1)
                throw new CaeException("The QuadWild alpha value must be a finite number from 0 to 1.");
            //
            string objFileName = Path.Combine(workDirectory, Globals.QuadWildInputObjFileName);
            string sharpFileName = Path.Combine(workDirectory, Globals.QuadWildSharpFileName);
            string prepConfigFileName = Path.Combine(workDirectory, Globals.QuadWildPrepConfigFileName);
            string flowConfigFileName = Path.Combine(workDirectory, Globals.QuadWildFlowConfigFileName);
            string inputPrefix = Path.Combine(workDirectory,
                                              Path.GetFileNameWithoutExtension(Globals.QuadWildInputObjFileName));
            // Prescribed nodes, read by quad_from_patches as <input>.cnodes
            string nodesFileName = inputPrefix + ".cnodes";
            string preparedObjFileName = inputPrefix + "_rem.obj";
            string tracePrefix = inputPrefix + "_rem_p0";
            string tracedObjFileName = tracePrefix + ".obj";
            string[] tracedFileNames = new string[]
            {
                tracedObjFileName, tracePrefix + ".patch", tracePrefix + ".corners",
                tracePrefix + ".feature", tracePrefix + ".c_feature"
            };
            // The quad generation stage writes both files and they share the same topology; only the node
            // positions differ.
            string smoothObjFileName = tracePrefix + "_1_quadrangulation_smooth.obj";
            string unsmoothedObjFileName = tracePrefix + "_1_quadrangulation.obj";
            string finalObjFileName = shellQuadWild.Smooth ? smoothObjFileName : unsmoothedObjFileName;
            //
            DeleteIfExists(objFileName);
            DeleteIfExists(sharpFileName);
            DeleteIfExists(nodesFileName);
            DeleteIfExists(prepConfigFileName);
            DeleteIfExists(flowConfigFileName);
            DeleteIfExists(preparedObjFileName);
            foreach (string tracedFileName in tracedFileNames) DeleteIfExists(tracedFileName);
            DeleteIfExists(smoothObjFileName);
            DeleteIfExists(unsmoothedObjFileName);
            //
            FeMesh triangularMesh = CreateTriangularMesh(part, shellQuadWild, workDirectory, null);
            QuadWildInputFileInfo inputInfo = QuadWildInputWriter.Write(objFileName, sharpFileName, nodesFileName,
                                                                        triangularMesh, part.Name);
            _controller.Form.WriteDataToOutput("QuadWild input: " + inputInfo.NodeCount + " nodes, " +
                                               inputInfo.FaceCount + " triangles, " +
                                               inputInfo.SharpEdgeCount + " sharp-edge segments.");
            if (inputInfo.SeamEdgeCount > 0)
                _controller.Form.WriteDataToOutput("QuadWild shell compound: " + inputInfo.SeamEdgeCount +
                                                   " model edge/s shared by more than two faces are meshed as seams " +
                                                   "between detached sheets (" +
                                                   inputInfo.SplitNodeCoordinates.Length + " split nodes).");
            if (inputInfo.SeamEdgeCount > 0 && shellQuadWild.TriangulationMesher != MesherTypeEnum.Gmsh)
                _controller.Form.WriteDataToOutput("Warning: only the Gmsh triangulation gives every seam an even " +
                                                   "number of elements. With " +
                                                   GetMesherName(shellQuadWild.TriangulationMesher) +
                                                   " a sheet bounded only by seams can have an odd number of " +
                                                   "seam segments, which QuadWild cannot mesh with quads.");
            if (shellQuadWild.Preview)
            {
                _controller.Form.WriteDataToOutput("QuadWild preview is enabled: importing the " +
                                                   GetMesherName(shellQuadWild.TriangulationMesher) +
                                                   " triangular pre-mesh and skipping QuadWild.");
                return new GeneratedMeshData()
                {
                    GeometryPart = part,
                    MeshFileName = objFileName,
                    ConvertToSecondOrder = false,
                    SmoothShaded = true,
                    QuadWildInputFileInfo = inputInfo
                };
            }
            // The preview returned above does not require an installed QuadWild runtime.
            string executable = Application.StartupPath + Globals.QuadWildMesher;
            string patchExecutable = Application.StartupPath + Globals.QuadWildPatchMesher;
            string runtimeDirectory = Path.GetDirectoryName(executable);
            ValidateRuntime(executable, patchExecutable, runtimeDirectory);
            //
            string prepTemplateFileName = Path.Combine(runtimeDirectory, "config", "prep_config", "basic_setup.txt");
            WriteConfig(prepTemplateFileName, prepConfigFileName,
                        new Dictionary<string, string>()
                        {
                            // The triangulation mesher controls the input; QuadWild's remesher would move
                            // the model vertices and prescribed nodes
                            { "do_remesh", "0" }
                        });
            //
            _controller.Form.WriteDataToOutput("Running QuadWild preparation and patch tracing...");
            string argument = Quote(objFileName) + " 2 " + Quote(prepConfigFileName) + " " + Quote(sharpFileName);
            bool preparationFailed = RunStage(part.Name, "preparation and patch tracing", executable, argument,
                                              workDirectory, tracedFileNames);
            EnsureNoPatchesErased(preparedObjFileName, tracedObjFileName, inputInfo.SeamEdgeCount > 0);
            string flowTemplateName;
            if (shellQuadWild.FlowProfile == QuadWildFlowProfileEnum.Aligned) flowTemplateName = "flow.txt";
            else if (shellQuadWild.FlowProfile == QuadWildFlowProfileEnum.NoAlignment)
                flowTemplateName = "flow_noalign.txt";
            else throw new CaeException("The selected QuadWild flow profile is not supported.");
            string flowTemplateFileName = Path.Combine(runtimeDirectory, "config", "main_config", flowTemplateName);
            string flowDependencyFileName = Path.Combine(runtimeDirectory, "config", "main_config",
                                                          "flow_virtual_simple.json");
            string satsumaDependencyFileName = Path.Combine(runtimeDirectory, "config", "satsuma", "default.json");
            WriteConfig(flowTemplateFileName, flowConfigFileName,
                        new Dictionary<string, string>()
                        {
                            { "alpha", shellQuadWild.Alpha.ToString("R", CultureInfo.InvariantCulture) },
                            { "scaleFact", shellQuadWild.ScaleFactor.ToString("R", CultureInfo.InvariantCulture) },
                            { "flow_config_filename", QuoteQuadWildPath(flowDependencyFileName) },
                            { "satsuma_config_filename", QuoteQuadWildPath(satsumaDependencyFileName) }
                        });
            //
            _controller.Form.WriteDataToOutput("Running QuadWild quad generation (" +
                                               GetFlowProfileName(shellQuadWild.FlowProfile) +
                                               ", quad edge multiplier " +
                                               shellQuadWild.ScaleFactor.ToString("R", CultureInfo.InvariantCulture) +
                                               ", alpha " +
                                               shellQuadWild.Alpha.ToString("R", CultureInfo.InvariantCulture) +
                                               (shellQuadWild.Smooth ? ", smoothed" : ", not smoothed") +
                                               ")...");
            argument = Quote(tracedObjFileName) + " 1 " + Quote(flowConfigFileName);
            // Both files are required even when only one is imported, so that a stage that died part way
            // through is still recognized by the freshly-written-output check.
            bool quadGenerationFailed = RunStage(part.Name, "quad generation", patchExecutable, argument,
                                                 workDirectory, unsmoothedObjFileName, smoothObjFileName);
            // The sheets of a shell compound are meshed with coincident node copies on their seams: merge them
            bool hasSeams = inputInfo.SeamEdgeCount > 0;
            if (hasSeams)
            {
                int mergedCount = MergeSplitNodes(finalObjFileName, inputInfo.SplitNodeCoordinates);
                _controller.Form.WriteDataToOutput("QuadWild seams: " + mergedCount + " node copies merged.");
            }
            QuadWildOutputInfo outputInfo = ValidateOutput(finalObjFileName, hasSeams);
            _controller.Form.WriteDataToOutput("QuadWild output: " + outputInfo.NodeCount + " nodes, " +
                                               outputInfo.QuadCount + " quadrilaterals, " +
                                               outputInfo.TriangleCount + " triangles.");
            WarnAboutDegenerateFaces(outputInfo);
            //
            return new GeneratedMeshData()
            {
                GeometryPart = part,
                MeshFileName = finalObjFileName,
                SmoothShaded = true,
                QuadWildInputFileInfo = inputInfo,
                RetainWorkDirectory = shellQuadWild.KeepFilesOnFailure &&
                                      (preparationFailed || quadGenerationFailed)
            };
        }
        private bool PreviewEdgeNodes(GeometryPart part, MeshSetupItem[] mesherSetupItems,
                                      OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            ShellQuadWild shellQuadWild = GetShellQuadWild(mesherSetupItems);
            _controller.Form.WriteDataToOutput("Creating the QuadWild " +
                                               GetMesherName(shellQuadWild.TriangulationMesher) +
                                               " edge-node preview...");
            if (shellQuadWild.TriangulationMesher == MesherTypeEnum.Gmsh)
                return _gmshRunner.PreviewEdgeNodesForQuadWild(part, meshSetupItems);
            else if (shellQuadWild.TriangulationMesher == MesherTypeEnum.Netgen)
                return _netgenRunner.PreviewEdgeNodesForQuadWild(part, meshSetupItems);
            else if (shellQuadWild.TriangulationMesher == MesherTypeEnum.Mmg)
                return PreviewModelEdgeNodes(part, "MMG");
            else throw new CaeException("The selected QuadWild triangulation mesher is not supported.");
        }
        private FeMesh CreateTriangularMesh(GeometryPart part, ShellQuadWild shellQuadWild, string workDirectory,
                                            OrderedDictionary<string, MeshSetupItem> meshSetupItems)
        {
            _controller.Form.WriteDataToOutput("Creating the triangular QuadWild input mesh using " +
                                               GetMesherName(shellQuadWild.TriangulationMesher) + "...");
            FeMesh mesh;
            if (shellQuadWild.TriangulationMesher == MesherTypeEnum.Gmsh)
                mesh = _gmshRunner.CreateFirstOrderShellMeshForQuadWild(part, workDirectory, meshSetupItems);
            else if (shellQuadWild.TriangulationMesher == MesherTypeEnum.Netgen)
                mesh = _netgenRunner.CreateFirstOrderShellMeshForQuadWild(part, workDirectory, meshSetupItems);
            else if (shellQuadWild.TriangulationMesher == MesherTypeEnum.Mmg)
                mesh = _mmgRunner.CreateFirstOrderShellMeshForQuadWild(part, workDirectory, meshSetupItems);
            else throw new CaeException("The selected QuadWild triangulation mesher is not supported.");
            //
            ValidateTriangularMesh(mesh, part.Name);
            return mesh;
        }
        private static ShellQuadWild GetShellQuadWild(MeshSetupItem[] mesherSetupItems)
        {
            if (mesherSetupItems == null || mesherSetupItems.Length != 1 ||
                !(mesherSetupItems[0] is ShellQuadWild shellQuadWild))
                throw new CaeException("Exactly one active shell setup item is required for a part.");
            return shellQuadWild;
        }
        private static void ValidateTriangularMesh(FeMesh mesh, string partName)
        {
            if (mesh == null || mesh.Nodes.Count == 0 || mesh.Elements.Count == 0)
                throw new CaeException("The selected mesher did not create a triangular QuadWild input mesh.");
            if (mesh.Parts.Count != 1 || !mesh.Parts.ContainsKey(partName))
                throw new CaeException("The triangular QuadWild pre-pass produced an unexpected mesh part layout.");
            foreach (FeElement element in mesh.Elements.Values)
            {
                if (!(element is LinearTriangleElement))
                    throw new CaeException("The QuadWild pre-pass must contain only first-order triangles.");
            }
        }
        private bool PreviewModelEdgeNodes(GeometryPart part, string mesherName)
        {
            int[] nodeIds;
            double[][] nodeCoor;
            int[][] cells;
            int[] cellTypes;
            _controller.Model.Geometry.GetNodesAndCellsForModelEdges(part, out nodeIds, out nodeCoor,
                                                                      out cells, out cellTypes);
            if (nodeCoor == null || nodeCoor.Length == 0)
            {
                _controller.Form.WriteDataToOutput("The part has no model-edge nodes to preview.");
                return false;
            }
            // Work on copies because the coordinates are owned by the geometry mesh.
            double[][] displayCoor = new double[nodeCoor.Length][];
            for (int i = 0; i < nodeCoor.Length; i++) displayCoor[i] = nodeCoor[i].ToArray();
            //
            double[] offset = _controller.GetExplodedViewOffsetForGeneratedMeshPart(part, null);
            if (offset != null)
            {
                for (int i = 0; i < displayCoor.Length; i++)
                {
                    for (int j = 0; j < 3; j++) displayCoor[i][j] += offset[j];
                }
            }
            _controller.HighlightNodes(displayCoor);
            _controller.Form.WriteDataToOutput("QuadWild " + mesherName + " edge-node preview: " +
                                               displayCoor.Length + " source model-edge nodes.");
            return true;
        }
        private static string GetMesherName(MesherTypeEnum mesherType)
        {
            if (mesherType == MesherTypeEnum.Mmg) return "MMG";
            else return mesherType.ToString();
        }
        private bool RunStage(string jobName, string stageName, string executable, string argument,
                              string workDirectory, params string[] expectedFileNames)
        {
            JobStatus jobStatus = _controller.JobRunner.Run(jobName, executable, argument, workDirectory,
                                                             _controller.Form.WriteDataToOutput);
            if (jobStatus != JobStatus.OK)
            {
                if (jobStatus == JobStatus.Killed)
                    throw new CaeException("QuadWild " + stageName + " was canceled.");
                else if ((jobStatus == JobStatus.Failed || jobStatus == JobStatus.FailedWithResults) &&
                         AreFilesWritten(expectedFileNames))
                    _controller.Form.WriteDataToOutput("QuadWild " + stageName + " returned " + jobStatus +
                                                       " after writing its output; validating the output...");
                else
                    throw new CaeException("QuadWild " + stageName + " failed (" + jobStatus + ").");
            }
            EnsureFilesWritten(stageName, expectedFileNames);
            return jobStatus != JobStatus.OK;
        }
        private static void ValidateRuntime(string executable, string patchExecutable, string runtimeDirectory)
        {
            if (!File.Exists(executable))
                throw new CaeException("The QuadWild executable was not found: " + executable);
            if (!File.Exists(patchExecutable))
                throw new CaeException("The QuadWild patch executable was not found: " + patchExecutable);
            if (String.IsNullOrWhiteSpace(runtimeDirectory) || !Directory.Exists(runtimeDirectory))
                throw new CaeException("The QuadWild runtime directory was not found.");
        }
        private static void WriteConfig(string templateFileName, string outputFileName,
                                        Dictionary<string, string> replacements)
        {
            if (!File.Exists(templateFileName))
                throw new CaeException("The QuadWild configuration file was not found: " + templateFileName);
            string[] lines = File.ReadAllLines(templateFileName);
            HashSet<string> replacedKeys = new HashSet<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (var replacement in replacements)
                {
                    if (lines[i].StartsWith(replacement.Key + " ", StringComparison.Ordinal))
                    {
                        lines[i] = replacement.Key + " " + replacement.Value;
                        replacedKeys.Add(replacement.Key);
                        break;
                    }
                }
            }
            string missingKey = replacements.Keys.FirstOrDefault(key => !replacedKeys.Contains(key));
            if (missingKey != null)
                throw new CaeException("The QuadWild configuration entry '" + missingKey + "' was not found in " +
                                       templateFileName + ".");
            File.WriteAllText(outputFileName, String.Join(Environment.NewLine, lines) + Environment.NewLine,
                              new UTF8Encoding(false));
        }
        private static string Quote(string value)
        {
            if (value == null || value.Contains("\""))
                throw new CaeException("A QuadWild file path is invalid.");
            return "\"" + value + "\"";
        }
        private static string QuoteQuadWildPath(string value)
        {
            if (!File.Exists(value))
                throw new CaeException("A QuadWild configuration dependency was not found: " + value);
            return Quote(value.Replace('\\', '/'));
        }
        private static string GetFlowProfileName(QuadWildFlowProfileEnum flowProfile)
        {
            if (flowProfile == QuadWildFlowProfileEnum.Aligned) return "aligned flow";
            else if (flowProfile == QuadWildFlowProfileEnum.NoAlignment) return "no alignment";
            else return flowProfile.ToString();
        }
        private static void DeleteIfExists(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);
        }
        private static void EnsureFilesWritten(string stageName, params string[] fileNames)
        {
            foreach (string fileName in fileNames)
            {
                if (!File.Exists(fileName) || new FileInfo(fileName).Length == 0)
                    throw new CaeException("QuadWild " + stageName + " did not create the expected file: " +
                                           fileName);
            }
        }
        private static bool AreFilesWritten(string[] fileNames)
        {
            return fileNames != null && fileNames.Length > 0 &&
                   fileNames.All(fileName => File.Exists(fileName) && new FileInfo(fileName).Length > 0);
        }
        // The tracer deletes every patch it could not make disk-like and then reports success, so a model
        // that lost most of its surface still leaves a readable but almost empty traced mesh behind. Tracing
        // splits triangles and never removes them, so a drop in the triangle count means patches were erased.
        private static void EnsureNoPatchesErased(string preparedObjFileName, string tracedObjFileName, bool hasSeams)
        {
            if (!File.Exists(preparedObjFileName) || !File.Exists(tracedObjFileName)) return;
            int preparedFaceCount = CountObjFaces(preparedObjFileName);
            int tracedFaceCount = CountObjFaces(tracedObjFileName);
            if (preparedFaceCount <= 0 || tracedFaceCount >= preparedFaceCount) return;
            //
            throw new CaeException("QuadWild patch tracing discarded " +
                                   (preparedFaceCount - tracedFaceCount) + " of " + preparedFaceCount +
                                   " triangles because it could not split the surface into disk-like patches. " +
                                   "The remaining mesh would cover only a part of the model." +
                                   (hasSeams ? " In a shell compound a sheet bounded only by short seams can need " +
                                               "more elements per seam: try a smaller maximum element size." : ""));
        }
        private static int CountObjFaces(string fileName)
        {
            int faceCount = 0;
            using (StreamReader reader = new StreamReader(fileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("f ", StringComparison.Ordinal)) faceCount++;
                }
            }
            return faceCount;
        }
        // Merges the output vertices that lie at the position of a split input node into one vertex and removes the
        // copies. The copies of a prescribed seam node are snapped to its input coordinates; QuadWild stores its quad
        // mesh in single precision, so they match the input within the float precision and each other exactly.
        // Returns the number of removed copies.
        private static int MergeSplitNodes(string objFileName, double[][] splitNodeCoordinates)
        {
            string[] lines = File.ReadAllLines(objFileName);
            List<double[]> vertices = new List<double[]>();
            foreach (string line in lines)
            {
                if (!line.StartsWith("v ")) continue;
                string[] tmp = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                vertices.Add(new double[] { double.Parse(tmp[1], NumberStyles.Float, CultureInfo.InvariantCulture),
                                            double.Parse(tmp[2], NumberStyles.Float, CultureInfo.InvariantCulture),
                                            double.Parse(tmp[3], NumberStyles.Float, CultureInfo.InvariantCulture) });
            }
            if (vertices.Count == 0 || splitNodeCoordinates.Length == 0) return 0;
            //
            BoundingBox box = new BoundingBox();
            foreach (double[] vertex in vertices) box.IncludeCoor(vertex);
            double maxAbsCoor = vertices.Max(coor => Math.Max(Math.Abs(coor[0]), Math.Max(Math.Abs(coor[1]), Math.Abs(coor[2]))));
            double tolerance = Math.Max(Math.Max(box.GetDiagonal(), maxAbsCoor), 1E-30) * 1E-6;
            Func<double[], (long, long, long)> cell = coor => ((long)Math.Floor(coor[0] / tolerance),
                                                               (long)Math.Floor(coor[1] / tolerance),
                                                               (long)Math.Floor(coor[2] / tolerance));
            Dictionary<(long, long, long), int> cellSplitNodeIndex = new Dictionary<(long, long, long), int>();
            for (int i = 0; i < splitNodeCoordinates.Length; i++)
            {
                var c = cell(splitNodeCoordinates[i]);
                for (long dx = -1; dx <= 1; dx++)
                    for (long dy = -1; dy <= 1; dy++)
                        for (long dz = -1; dz <= 1; dz++)
                            cellSplitNodeIndex[(c.Item1 + dx, c.Item2 + dy, c.Item3 + dz)] = i;
            }
            // New 1-based index of every vertex; the first vertex at a split node keeps it for all copies
            int[] newIndex = new int[vertices.Count];
            bool[] isCopy = new bool[vertices.Count];
            Dictionary<int, int> splitNodeVertexIndex = new Dictionary<int, int>();
            int count = 0;
            int mergedCount = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (cellSplitNodeIndex.TryGetValue(cell(vertices[i]), out int splitNodeIndex))
                {
                    if (splitNodeVertexIndex.TryGetValue(splitNodeIndex, out int existing))
                    {
                        newIndex[i] = existing;
                        isCopy[i] = true;
                        mergedCount++;
                        continue;
                    }
                    splitNodeVertexIndex.Add(splitNodeIndex, count + 1);
                }
                newIndex[i] = ++count;
            }
            if (mergedCount == 0) return 0;
            //
            StringBuilder output = new StringBuilder();
            int vertexIndex = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith("v "))
                {
                    if (!isCopy[vertexIndex++]) output.AppendLine(line);
                }
                else if (line.StartsWith("f "))
                {
                    string[] tmp = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    output.Append("f");
                    for (int i = 1; i < tmp.Length; i++)
                    {
                        string[] parts = tmp[i].Split('/');
                        parts[0] = newIndex[int.Parse(parts[0], CultureInfo.InvariantCulture) - 1]
                                   .ToString(CultureInfo.InvariantCulture);
                        output.Append(" ").Append(String.Join("/", parts));
                    }
                    output.AppendLine();
                }
                else output.AppendLine(line);
            }
            File.WriteAllText(objFileName, output.ToString());
            return mergedCount;
        }
        private static QuadWildOutputInfo ValidateOutput(string fileName, bool allowSeams)
        {
            QuadWildOutputInfo outputInfo = ReadAndValidateOutput(fileName, allowSeams);
            if (outputInfo.QuadCount == 0)
                throw new CaeException("The QuadWild output does not contain any quadrilateral elements.");
            return outputInfo;
        }
        private static QuadWildOutputInfo ReadAndValidateOutput(string fileName, bool allowSeams = false)
        {
            FeMesh mesh;
            try
            {
                mesh = ObjFileReader.Read(fileName, false);
            }
            catch (Exception ex)
            {
                throw new CaeException("The QuadWild output OBJ could not be read. " + ex.Message);
            }
            if (mesh == null || mesh.Nodes.Count == 0 || mesh.Elements.Count == 0 || mesh.Parts.Count == 0)
                throw new CaeException("The QuadWild output OBJ does not contain a non-empty shell mesh.");
            if (mesh.Parts.Values.Any(part => part.PartType != PartType.Shell))
                throw new CaeException("The QuadWild output OBJ contains a non-shell mesh part.");
            foreach (FeNode node in mesh.Nodes.Values)
            {
                if (Double.IsNaN(node.X) || Double.IsInfinity(node.X) ||
                    Double.IsNaN(node.Y) || Double.IsInfinity(node.Y) ||
                    Double.IsNaN(node.Z) || Double.IsInfinity(node.Z))
                    throw new CaeException("The QuadWild output contains a non-finite node coordinate.");
            }
            //
            int triangleCount = 0;
            int quadCount = 0;
            int degenerateFaceCount = 0;
            Dictionary<(int, int), int> edgeUseCount = new Dictionary<(int, int), int>();
            foreach (FeElement element in mesh.Elements.Values)
            {
                int cornerCount;
                if (element is LinearTriangleElement)
                {
                    triangleCount++;
                    cornerCount = 3;
                }
                else if (element is LinearQuadrilateralElement)
                {
                    quadCount++;
                    cornerCount = 4;
                }
                else throw new CaeException("The QuadWild output contains an unsupported element type.");
                int[] nodeIds = element.NodeIds.Take(cornerCount).ToArray();
                if (nodeIds.Distinct().Count() != cornerCount)
                    throw new CaeException("The QuadWild output contains a face with repeated nodes.");
                // The area of the face itself is measured, not the area of one of its fan triangles. A quad
                // whose corner sits on the line joining its neighbours has a flat corner and a zero area fan
                // triangle, but a perfectly usable area of its own. The allowance is relative to the face
                // size so that it does not depend on the unit the model is drawn in.
                double maxEdgeLengthSquared = GetMaxEdgeLengthSquared(mesh, nodeIds);
                if (GetFaceArea2(mesh, nodeIds) <= maxEdgeLengthSquared * DegenerateFaceAreaFactor)
                    degenerateFaceCount++;
                for (int i = 0; i < cornerCount; i++)
                {
                    (int, int) edgeKey = GetEdgeKey(nodeIds[i], nodeIds[(i + 1) % cornerCount]);
                    if (edgeUseCount.TryGetValue(edgeKey, out int count)) edgeUseCount[edgeKey] = count + 1;
                    else edgeUseCount.Add(edgeKey, 1);
                }
            }
            if (!allowSeams && edgeUseCount.Values.Any(count => count > 2))
                throw new CaeException("The QuadWild output is non-manifold; an edge belongs to more than two faces.");
            return new QuadWildOutputInfo(mesh.Nodes.Count, triangleCount, quadCount, degenerateFaceCount);
        }
        private void WarnAboutDegenerateFaces(QuadWildOutputInfo outputInfo)
        {
            // The QuadWild smoothing stage can pull all corners of a face onto one sharp feature line. The
            // rest of the mesh stays usable, so the collapsed faces are reported instead of rejecting it.
            if (outputInfo.DegenerateFaceCount == 0) return;
            _controller.Form.WriteDataToOutput("Warning: " + outputInfo.DegenerateFaceCount + " QuadWild " +
                                               "output faces collapsed onto a feature line and have no area.");
        }
        private static double GetFaceArea2(FeMesh mesh, int[] nodeIds)
        {
            // Newell's method - the doubled area of a face that does not have to be planar.
            double nx = 0;
            double ny = 0;
            double nz = 0;
            for (int i = 0; i < nodeIds.Length; i++)
            {
                FeNode node0 = mesh.Nodes[nodeIds[i]];
                FeNode node1 = mesh.Nodes[nodeIds[(i + 1) % nodeIds.Length]];
                nx += (node0.Y - node1.Y) * (node0.Z + node1.Z);
                ny += (node0.Z - node1.Z) * (node0.X + node1.X);
                nz += (node0.X - node1.X) * (node0.Y + node1.Y);
            }
            return Math.Sqrt(nx * nx + ny * ny + nz * nz);
        }
        private static double GetMaxEdgeLengthSquared(FeMesh mesh, int[] nodeIds)
        {
            double maxLengthSquared = 0;
            for (int i = 0; i < nodeIds.Length; i++)
            {
                FeNode node0 = mesh.Nodes[nodeIds[i]];
                FeNode node1 = mesh.Nodes[nodeIds[(i + 1) % nodeIds.Length]];
                double dx = node1.X - node0.X;
                double dy = node1.Y - node0.Y;
                double dz = node1.Z - node0.Z;
                maxLengthSquared = Math.Max(maxLengthSquared, dx * dx + dy * dy + dz * dz);
            }
            return maxLengthSquared;
        }
        private static (int, int) GetEdgeKey(int nodeId1, int nodeId2)
        {
            if (nodeId1 < nodeId2) return (nodeId1, nodeId2);
            else return (nodeId2, nodeId1);
        }


        private class QuadWildOutputInfo
        {
            public int NodeCount;
            public int TriangleCount;
            public int QuadCount;
            public int DegenerateFaceCount;

            public QuadWildOutputInfo(int nodeCount, int triangleCount, int quadCount, int degenerateFaceCount)
            {
                NodeCount = nodeCount;
                TriangleCount = triangleCount;
                QuadCount = quadCount;
                DegenerateFaceCount = degenerateFaceCount;
            }
        }
    }
}
