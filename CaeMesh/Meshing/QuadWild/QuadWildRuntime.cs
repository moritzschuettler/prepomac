// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CaeMesh
{
    /// <summary>
    /// The files and the two executables of a QuadWild run, shared by the shell mesher, which runs the stages as
    /// jobs of the application, and the sweep, which runs them from inside the gmsh process.
    /// </summary>
    public static class QuadWildRuntime
    {
        // Variables                                                                                                                
        public const string MesherFileName = "quadwild.exe";
        public const string PatchMesherFileName = "quad_from_patches.exe";
        private const int _numOfReportedLines = 20;
        // QuadWild options that switch off the patch layout (stage 1) and mesh quality (stage 2) improvements
        public static readonly string[] OldAlgorithmPrepOptions = new string[]
        {
            "max_side_turning 0", "aligned_hole_corners 0", "thin_patch_ratio 0"
        };
        public static readonly string[] OldAlgorithmFlowOptions = new string[]
        {
            "input_line_count 0", "input_mesh_size 0", "graded_alpha 0", "field_smooth_sweeps 0",
            "split_boundary_fans 0", "smooth_curves 0", "rotate_edges 0", "boundary_layer 0", "nonquad_min_side 1",
            "thin_corner_merge 0", "thin_corner_pattern 0",
            "strip_equal_sides 0", "bend_corners 0", "symmetric_mesh 0",
            "merge_infeasible_patches 0", "curve_max_angle 0", "even_holes 0",
            "feature_chain_smooth 0", "smooth_no_fold 0", "transfinite_three_sided 0", "transfinite_four_sided 0",
            "bad_quad_short_side_corner 0", "flat_side_padding 0", "even_feature_loops 0"
        };


        // Methods                                                                                                                  
        /// <summary>
        /// The quad generation options of the transfinite faces: structured grids on the faces with 3 and 4 sides whose
        /// corner angles lie between 180° minus the threshold angle and the threshold angle.
        /// </summary>
        public static string[] GetTransfiniteFlowOptions(bool threeSided, bool fourSided, double angleDeg)
        {
            return new string[]
            {
                "transfinite_three_sided " + (threeSided ? 1 : 0),
                "transfinite_four_sided " + (fourSided ? 1 : 0),
                "transfinite_angle " + angleDeg.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
            };
        }
        public static void ValidateRuntime(string executable, string patchExecutable, string runtimeDirectory)
        {
            if (!File.Exists(executable))
                throw new CaeException("The QuadWild executable was not found: " + executable);
            if (!File.Exists(patchExecutable))
                throw new CaeException("The QuadWild patch executable was not found: " + patchExecutable);
            if (String.IsNullOrWhiteSpace(runtimeDirectory) || !Directory.Exists(runtimeDirectory))
                throw new CaeException("The QuadWild runtime directory was not found.");
        }
        /// <summary>
        /// The preparation configuration: the template with QuadWild's own remeshing switched off, since the
        /// triangulation it is given is the one the nodes are meant to follow.
        /// </summary>
        public static void WritePrepConfig(string runtimeDirectory, string outputFileName, bool oldAlgorithm)
        {
            string templateFileName = Path.Combine(runtimeDirectory, "config", "prep_config", "basic_setup.txt");
            WriteConfig(templateFileName, outputFileName, new Dictionary<string, string>() { { "do_remesh", "0" } },
                        oldAlgorithm ? OldAlgorithmPrepOptions : null);
        }
        /// <summary>
        /// The quad generation configuration of the flow profile, with the dependencies it names made absolute so
        /// that the stage runs from any folder.
        /// </summary>
        public static void WriteFlowConfig(string runtimeDirectory, string outputFileName,
                                           QuadWildFlowProfileEnum flowProfile, double alpha,
                                           IEnumerable<string> extraLines)
        {
            string flowTemplateName;
            if (flowProfile == QuadWildFlowProfileEnum.Aligned) flowTemplateName = "flow.txt";
            else if (flowProfile == QuadWildFlowProfileEnum.NoAlignment) flowTemplateName = "flow_noalign.txt";
            else throw new CaeException("The selected QuadWild flow profile is not supported.");
            string mainConfigDirectory = Path.Combine(runtimeDirectory, "config", "main_config");
            string flowDependencyFileName = Path.Combine(mainConfigDirectory, "flow_virtual_simple.json");
            string satsumaDependencyFileName = Path.Combine(runtimeDirectory, "config", "satsuma", "default.json");
            WriteConfig(Path.Combine(mainConfigDirectory, flowTemplateName), outputFileName,
                        new Dictionary<string, string>()
                        {
                            { "alpha", alpha.ToString("R", System.Globalization.CultureInfo.InvariantCulture) },
                            // The quad size follows the triangulation: its sizes and edge node counts
                            { "scaleFact", "1" },
                            { "flow_config_filename", QuoteQuadWildPath(flowDependencyFileName) },
                            { "satsuma_config_filename", QuoteQuadWildPath(satsumaDependencyFileName) }
                        },
                        extraLines);
        }
        public static void WriteConfig(string templateFileName, string outputFileName,
                                       Dictionary<string, string> replacements, IEnumerable<string> extraLines = null)
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
            // optional "key value" lines at the end, read by QuadWild's option parser
            if (extraLines != null)
                lines = lines.Concat(extraLines).ToArray();
            File.WriteAllText(outputFileName, String.Join(Environment.NewLine, lines) + Environment.NewLine,
                              new UTF8Encoding(false));
        }
        /// <summary>
        /// A file path for the command line of a stage, in its extended-length form.
        /// </summary>
        public static string Quote(string fileName)
        {
            return QuoteText(fileName == null ? null : GetExtendedLengthPath(fileName));
        }
        public static string QuoteQuadWildPath(string value)
        {
            if (!File.Exists(value))
                throw new CaeException("A QuadWild configuration dependency was not found: " + value);
            return QuoteText(value.Replace('\\', '/'));
        }
        private static string QuoteText(string value)
        {
            if (value == null || value.Contains("\""))
                throw new CaeException("A QuadWild file path is invalid.");
            return "\"" + value + "\"";
        }
        /// <summary>
        /// The path with the \\?\ prefix, which lifts the MAX_PATH limit of 259 characters. Neither QuadWild executable
        /// is long path aware, and quad_from_patches does not report a file it cannot open: from a work directory long
        /// enough that the path of its smoothed mesh reached the limit, it wrote the unsmoothed mesh only, and the stage
        /// failed with a timing table for its last messages. The executables open prefixed paths, and give the files
        /// they derive from them - the patches, the prescribed nodes, the meshes - the same prefix.
        /// </summary>
        public static string GetExtendedLengthPath(string path)
        {
            if (path.StartsWith(@"\\?\", StringComparison.Ordinal)) return path;
            string fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(@"\\", StringComparison.Ordinal)) return @"\\?\UNC\" + fullPath.Substring(2);
            return @"\\?\" + fullPath;
        }
        public static void DeleteIfExists(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);
        }
        public static void EnsureFilesWritten(string stageName, params string[] fileNames)
        {
            foreach (string fileName in fileNames)
            {
                if (!File.Exists(fileName) || new FileInfo(fileName).Length == 0)
                    throw new CaeException("QuadWild " + stageName + " did not create the expected file: " +
                                           fileName);
            }
        }
        public static bool AreFilesWritten(string[] fileNames)
        {
            return fileNames != null && fileNames.Length > 0 &&
                   fileNames.All(fileName => File.Exists(fileName) && new FileInfo(fileName).Length > 0);
        }
        /// <summary>
        /// The tracer deletes every patch it could not make disk-like and then reports success, so a model that lost
        /// most of its surface still leaves a readable but almost empty traced mesh behind. Tracing splits triangles
        /// and never removes them, so a drop in the triangle count means patches were erased.
        /// </summary>
        public static void EnsureNoPatchesErased(string preparedObjFileName, string tracedObjFileName, string advice)
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
                                   (advice != null ? " " + advice : ""));
        }
        public static int CountObjFaces(string fileName)
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
        /// <summary>
        /// Runs one stage and waits for it. The exit codes of QuadWild do not tell success from failure, so the
        /// stage is judged by the files it was expected to write; the last lines it printed are kept for the error
        /// that names a stage that wrote nothing. The stage is given its files by full paths and runs in its own
        /// folder, since a process cannot be started in a work folder of MAX_PATH characters or more.
        /// </summary>
        public static void RunStage(string stageName, string executable, string argument,
                                    params string[] expectedFileNames)
        {
            foreach (var fileName in expectedFileNames) DeleteIfExists(fileName);
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
            ProcessStartInfo startInfo = new ProcessStartInfo(executable, argument)
            {
                WorkingDirectory = Path.GetDirectoryName(executable),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process process = new Process() { StartInfo = startInfo })
            {
                process.OutputDataReceived += keepLine;
                process.ErrorDataReceived += keepLine;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            //
            if (!AreFilesWritten(expectedFileNames))
            {
                string output;
                lock (lastLinesLock) output = String.Join(Environment.NewLine, lastLines);
                throw new CaeException("QuadWild " + stageName + " failed. Its last messages:" + Environment.NewLine +
                                       output);
            }
        }
    }
}
