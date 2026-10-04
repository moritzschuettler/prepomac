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
using System.IO;
using System.Linq;
using System.Text;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Solves the interval assignment of a sweep beside the counts the sweep settles itself, and writes how the two
    /// compare, without changing the mesh. It runs only when the environment variable PMX_INTERVAL_SHADOW names a folder:
    /// every solve appends its report to interval_shadow.txt and a line to interval_shadow_summary.txt there, and leaves
    /// the model it solved in lp format beside them.
    /// </summary>
    public static class SweepIntervalShadow
    {
        // Variables
        private const string _environmentVariable = "PMX_INTERVAL_SHADOW";
        private const string _applyEnvironmentVariable = "PMX_INTERVAL_APPLY";
        private const int _timeoutSeconds = 20;
        private const int _maxListed = 40;
        private static int _numOfReports = 0;


        // Properties
        public static string Folder { get { return Environment.GetEnvironmentVariable(_environmentVariable); } }
        public static bool Enabled { get { return !string.IsNullOrEmpty(Folder) && Directory.Exists(Folder); } }
        /// <summary>
        /// Whether the sweep meshes with the solved counts instead of settling its own, when the solve keeps every hard row,
        /// for the trial of the assignment on the models: set PMX_INTERVAL_APPLY to 1 beside PMX_INTERVAL_SHADOW.
        /// </summary>
        public static bool Apply { get { return Enabled && Environment.GetEnvironmentVariable(_applyEnvironmentVariable) == "1"; } }


        // Methods
        /// <summary>
        /// Reports the solve of the problem against the counts the sweep settled, or against the message it refused the
        /// meshing with. Nothing it does reaches the meshing: a failure is written to the report.
        /// </summary>
        public static void Report(IntervalProblem problem, Dictionary<int, int> settledEdgeIdNumOfElements,
                                  string refusal, Func<string, string> nameEntities)
        {
            if (problem == null || !Enabled) return;
            //
            int index = ++_numOfReports;
            StringBuilder report = new StringBuilder();
            string summary;
            try
            {
                Func<int, int> getSettled = edgeId =>
                    settledEdgeIdNumOfElements != null && settledEdgeIdNumOfElements.TryGetValue(edgeId, out int count) ?
                    count : problem.EdgeIdTarget[edgeId];
                Func<int, int> getTarget = edgeId => problem.EdgeIdTarget[edgeId];
                Func<IntervalRowTierEnum, int> countRows = tier => problem.Rows.Count(row => row.Tier == tier);
                //
                report.AppendLine("=== Solve " + index + (Apply ? " (applied)" : "") + ": " + problem.EdgeIdTarget.Count + " edges (" +
                                  problem.EdgeIdFixedCount.Count + " fixed), " + problem.Rows.Count + " rows (" +
                                  countRows(IntervalRowTierEnum.Hard) + " hard, " +
                                  countRows(IntervalRowTierEnum.Structured) + " structured, " +
                                  countRows(IntervalRowTierEnum.Preferred) + " preferred)");
                // The counts the sweep settled
                string settled;
                List<IntervalRow> settledViolations = null;
                if (refusal != null)
                {
                    settled = "refused";
                    report.AppendLine("Sweep refused: " + nameEntities(refusal));
                }
                else
                {
                    settledViolations = problem.GetViolatedRows(getSettled);
                    settled = "settled";
                    report.AppendLine("Sweep settled the counts, deviation " +
                                      problem.GetDeviation(getSettled).ToString("0.###") + ", breaking " +
                                      settledViolations.Count + " row/s");
                    AppendRows(report, settledViolations, getSettled, nameEntities);
                }
                // The solve
                string lpFileName = Path.Combine(Folder, "interval_" + index + ".lp");
                IntervalSolution solution = problem.Solve(false, _timeoutSeconds, lpFileName);
                report.AppendLine("lp_solve: " + solution.StatusName + " in " + solution.Seconds.ToString("0.###") + " s");
                string solved = solution.StatusName;
                int numOfDifferent = 0;
                if (solution.HasCounts)
                {
                    Func<int, int> getSolved = edgeId => solution.EdgeIdNumOfElements[edgeId];
                    File.WriteAllLines(Path.Combine(Folder, "interval_" + index + "_counts.txt"),
                                       problem.EdgeIds.OrderBy(edgeId => edgeId)
                                              .Select(edgeId => edgeId + "\t" + getSettled(edgeId) + "\t" + getSolved(edgeId) +
                                                                "\t" + getTarget(edgeId)));
                    report.AppendLine("Solved counts, deviation " + problem.GetDeviation(getSolved).ToString("0.###") +
                                      ", giving up " + solution.ViolatedRows.Count + " soft row/s");
                    AppendRows(report, solution.ViolatedRows, getSolved, nameEntities);
                    //
                    int[] differentEdgeIds = problem.EdgeIds.Where(edgeId => getSolved(edgeId) != getSettled(edgeId))
                                                            .OrderBy(edgeId => edgeId).ToArray();
                    numOfDifferent = differentEdgeIds.Length;
                    report.AppendLine(numOfDifferent + " edge/s differ from the " +
                                      (refusal != null ? "targets" : "settled counts"));
                    foreach (var edgeId in differentEdgeIds.Take(_maxListed))
                        report.AppendLine("  Edge " + edgeId + " (" + nameEntities("Edge " + edgeId) + "): " +
                                          (refusal != null ? "target " : "settled ") + getSettled(edgeId) +
                                          ", solved " + getSolved(edgeId) + ", target " + getTarget(edgeId));
                    if (numOfDifferent > _maxListed) report.AppendLine("  ...");
                    solved += "/" + solution.ViolatedRows.Count + " given up";
                }
                else
                {
                    // Which hard rows cannot be kept together
                    IntervalSolution relaxed = problem.Solve(true, _timeoutSeconds);
                    report.AppendLine("Relaxed: " + relaxed.StatusName + ", " + relaxed.ViolatedRows.Count +
                                      " row/s cannot be kept");
                    if (relaxed.HasCounts)
                        AppendRows(report, relaxed.ViolatedRows, edgeId => relaxed.EdgeIdNumOfElements[edgeId],
                                   nameEntities);
                    solved += "/" + relaxed.ViolatedRows.Count(row => row.Tier == IntervalRowTierEnum.Hard) +
                              " hard in conflict";
                }
                summary = index + "\t" + settled + (settledViolations != null ? "/" + settledViolations.Count + " broken" : "") +
                          "\t" + solved + "\t" + numOfDifferent + " differ\t" + solution.Seconds.ToString("0.###") + " s";
            }
            catch (Exception ex)
            {
                report.AppendLine("The shadow solve failed: " + ex);
                summary = index + "\tfailed\t" + ex.GetType().Name + ": " + ex.Message;
            }
            try
            {
                File.AppendAllText(Path.Combine(Folder, "interval_shadow.txt"), report.ToString() + Environment.NewLine);
                File.AppendAllText(Path.Combine(Folder, "interval_shadow_summary.txt"), summary + Environment.NewLine);
            }
            catch
            {
            }
        }
        /// <summary>
        /// Writes the chains, walls, cross sections and blocks the problem was built from to topology.txt, in gmsh tags,
        /// for checking the solved counts against the topology outside the application.
        /// </summary>
        public static void WriteTopology(List<SweepChain> chains, List<int> chainBlockIndices, List<SweepWall> walls,
                                         List<int[]> crossSectionVertexIds, List<int> crossSectionBlockIndices,
                                         List<SweepBlock> blocks)
        {
            if (!Enabled) return;
            //
            try
            {
                StringBuilder dump = new StringBuilder();
                for (int c = 0; c < chains.Count; c++)
                    dump.AppendLine("chain " + c + " block " + (chainBlockIndices != null && c < chainBlockIndices.Count ?
                                                                chainBlockIndices[c] : -1) +
                                    " edges " + string.Join(",", chains[c].EdgeIds) + " vertices " +
                                    string.Join(",", chains[c].VertexIds));
                foreach (var wall in walls)
                    dump.AppendLine("wall " + wall.SurfaceId + " src " + string.Join(",", wall.SourceEdgeIds) + " tgt " +
                                    string.Join(",", wall.TargetEdgeIds) + " dir " + string.Join(",", wall.DirectionEdgeIds));
                for (int g = 0; g < crossSectionVertexIds.Count; g++)
                    dump.AppendLine("section block " + crossSectionBlockIndices[g] + " vertices " +
                                    string.Join(",", crossSectionVertexIds[g]));
                if (blocks != null)
                {
                    for (int b = 0; b < blocks.Count; b++)
                        dump.AppendLine("block " + b + " sources " + string.Join(",", blocks[b].SourceSurfaceIds) +
                                        " walls " + string.Join(",", blocks[b].WallSurfaceIds) + " targets " +
                                        string.Join(",", blocks[b].TargetSurfaceIds));
                }
                File.WriteAllText(Path.Combine(Folder, "topology.txt"), dump.ToString());
            }
            catch
            {
            }
        }
        private static void AppendRows(StringBuilder report, List<IntervalRow> rows, Func<int, int> getCount,
                                       Func<string, string> nameEntities)
        {
            foreach (var row in rows.Take(_maxListed)) report.AppendLine("  " + row.Describe(getCount));
            if (rows.Count > _maxListed) report.AppendLine("  ...");
        }
    }
}
