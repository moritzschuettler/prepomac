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
using System.Linq;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// Lays the parts of a finished disassembly plan out the way an exploded technical drawing does.
    ///
    /// The planner answers how far a part has to travel before nothing is in its way any more. That is a
    /// question about the geometry and its answer makes a poor drawing: the distances have nothing to do
    /// with each other, they follow whatever direction the mating faces happened to give, and the picture
    /// they produce is as wide as the longest escape route in the assembly.
    ///
    /// The order the planner found is kept, since that is the part of its answer worth having, and the
    /// distances are thrown away. Every part is given one movement along the global axis nearest to the
    /// direction it left in, the parts that stand behind one another along that axis are stacked in the
    /// order they came off, and the parts that stand next to one another keep their own place across it.
    /// Nothing here asks how far a part had to travel to get out, so a bolt sunk deep into a casing ends up
    /// beside the one that only had to clear a cover.
    /// </summary>
    internal static class ExplodedViewDrawingLayout
    {
        // Variables                                                                                                                
        public static bool Enabled = true;
        /// <summary>
        /// The space left between two parts of one stack, relative to the assembly bounding box diagonal
        /// </summary>
        public static double GapFactor = 0.02;
        /// <summary>
        /// The space left between two parts of one stack that do not touch in the assembly
        /// </summary>
        public static double GroupGapFactor = 0.06;
        //
        private const double _tiny = 1E-12;


        // Methods                                                                                                                  
        public static void Apply(DisassemblyContactGraph graph, DirectionalDisassemblyPlan plan)
        {
            if (!Enabled || graph == null || plan == null || plan.Moves == null) return;
            //
            ExplodedViewCluster[] clusters = graph.Clusters;
            int count = clusters.Length;
            if (count == 0) return;
            //
            BoundingBox assemblyBoundingBox = new BoundingBox();
            foreach (ExplodedViewCluster cluster in clusters) assemblyBoundingBox.IncludeBox(cluster.BoundingBox);
            double assemblyDiagonal = assemblyBoundingBox.GetDiagonal();
            //
            int[] laneAxes = new int[count];
            double[] laneSigns = new double[count];
            int[] lastSteps = new int[count];
            for (int i = 0; i < count; i++)
            {
                laneAxes[i] = -1;
                lastSteps[i] = -1;
            }
            //
            foreach (DisassemblyMove move in plan.Moves)
            {
                foreach (int clusterId in move.ClusterIds)
                {
                    if (move.Step > lastSteps[clusterId]) lastSteps[clusterId] = move.Step;
                }
            }
            //
            for (int i = 0; i < count; i++)
            {
                if (lastSteps[i] < 0) continue;
                //
                Vec3D netOffset = new Vec3D(plan.ClusterOffsets[i]);
                if (netOffset.Len2 <= _tiny) continue;
                //
                GetNearestAxis(netOffset, out laneAxes[i], out laneSigns[i]);
            }
            //
            double[] distances = new double[count];
            //
            foreach (var laneEntry in GetLaneClusterIds(count, laneAxes, laneSigns))
            {
                int laneAxis = (int)laneEntry.Key[0];
                double laneSign = laneEntry.Key[1];
                //
                foreach (List<int> stackClusterIds in GetStacks(clusters, laneEntry.Value, laneAxis))
                {
                    SortStack(clusters, stackClusterIds, laneAxis, laneSign, lastSteps);
                    PackStack(graph, clusters, stackClusterIds, laneAxis, laneSign, assemblyBoundingBox,
                              assemblyDiagonal, distances);
                }
            }
            //
            EqualizeCopies(graph, count, laneAxes, laneSigns, distances);
            //
            foreach (var laneEntry in GetLaneClusterIds(count, laneAxes, laneSigns))
            {
                int laneAxis = (int)laneEntry.Key[0];
                double laneSign = laneEntry.Key[1];
                //
                foreach (List<int> stackClusterIds in GetStacks(clusters, laneEntry.Value, laneAxis))
                {
                    SortStack(clusters, stackClusterIds, laneAxis, laneSign, lastSteps);
                    RepackStack(graph, clusters, stackClusterIds, laneAxis, laneSign, assemblyBoundingBox,
                                assemblyDiagonal, distances);
                }
            }
            //
            WriteOffsets(plan, count, laneAxes, laneSigns, distances, lastSteps);
        }
        /// <summary>
        /// The global axis the direction is closest to, which is the one its largest component lies on
        /// </summary>
        private static void GetNearestAxis(Vec3D direction, out int axis, out double sign)
        {
            axis = 0;
            double largest = Math.Abs(direction.X);
            if (Math.Abs(direction.Y) > largest) { largest = Math.Abs(direction.Y); axis = 1; }
            if (Math.Abs(direction.Z) > largest) { largest = Math.Abs(direction.Z); axis = 2; }
            //
            double component = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            sign = component < 0 ? -1 : 1;
        }
        private static Dictionary<double[], List<int>> GetLaneClusterIds(int count, int[] laneAxes, double[] laneSigns)
        {
            Dictionary<string, double[]> laneKeys = new Dictionary<string, double[]>();
            Dictionary<string, List<int>> clusterIdsByLane = new Dictionary<string, List<int>>();
            //
            for (int i = 0; i < count; i++)
            {
                if (laneAxes[i] < 0) continue;
                //
                string key = laneAxes[i] + "|" + laneSigns[i];
                if (!clusterIdsByLane.ContainsKey(key))
                {
                    clusterIdsByLane.Add(key, new List<int>());
                    laneKeys.Add(key, new double[] { laneAxes[i], laneSigns[i] });
                }
                clusterIdsByLane[key].Add(i);
            }
            //
            Dictionary<double[], List<int>> lanes = new Dictionary<double[], List<int>>();
            foreach (var entry in clusterIdsByLane) lanes.Add(laneKeys[entry.Key], entry.Value);
            //
            return lanes;
        }
        /// <summary>
        /// The clusters of one lane split into the stacks that stand behind one another along it. Two
        /// clusters belong to the same stack when they cover a common area across the lane, which is what
        /// makes them share the room the lane offers, and the grouping is carried over so that a washer
        /// covering a bolt and a nut covering the washer all end up in one stack.
        /// </summary>
        private static List<List<int>> GetStacks(ExplodedViewCluster[] clusters, List<int> clusterIds, int laneAxis)
        {
            int firstAxis = (laneAxis + 1) % 3;
            int secondAxis = (laneAxis + 2) % 3;
            //
            int[] stackIds = new int[clusterIds.Count];
            for (int i = 0; i < stackIds.Length; i++) stackIds[i] = i;
            //
            for (int i = 0; i < clusterIds.Count; i++)
            {
                for (int j = i + 1; j < clusterIds.Count; j++)
                {
                    if (stackIds[i] == stackIds[j]) continue;
                    //
                    BoundingBox firstBox = clusters[clusterIds[i]].BoundingBox;
                    BoundingBox secondBox = clusters[clusterIds[j]].BoundingBox;
                    if (!Overlaps(firstBox, secondBox, firstAxis)) continue;
                    if (!Overlaps(firstBox, secondBox, secondAxis)) continue;
                    //
                    int oldStackId = stackIds[j];
                    int newStackId = stackIds[i];
                    for (int k = 0; k < stackIds.Length; k++)
                    {
                        if (stackIds[k] == oldStackId) stackIds[k] = newStackId;
                    }
                }
            }
            //
            Dictionary<int, List<int>> stacks = new Dictionary<int, List<int>>();
            for (int i = 0; i < clusterIds.Count; i++)
            {
                if (!stacks.ContainsKey(stackIds[i])) stacks.Add(stackIds[i], new List<int>());
                stacks[stackIds[i]].Add(clusterIds[i]);
            }
            //
            return stacks.Values.ToList();
        }
        private static bool Overlaps(BoundingBox firstBox, BoundingBox secondBox, int axis)
        {
            return GetMin(firstBox, axis) < GetMax(secondBox, axis) && GetMax(firstBox, axis) > GetMin(secondBox, axis);
        }
        /// <summary>
        /// The parts of a stack in the order they came off, the ones already further along the lane first
        /// when they came off together
        /// </summary>
        private static void SortStack(ExplodedViewCluster[] clusters, List<int> stackClusterIds, int laneAxis,
                                      double laneSign, int[] lastSteps)
        {
            stackClusterIds.Sort((first, second) =>
            {
                if (lastSteps[first] != lastSteps[second]) return lastSteps[first].CompareTo(lastSteps[second]);
                //
                double firstPosition = laneSign * GetMin(clusters[first].BoundingBox, laneAxis);
                double secondPosition = laneSign * GetMin(clusters[second].BoundingBox, laneAxis);
                if (firstPosition != secondPosition) return secondPosition.CompareTo(firstPosition);
                //
                return string.CompareOrdinal(clusters[first].GetName(), clusters[second].GetName());
            });
        }
        private static void PackStack(DisassemblyContactGraph graph, ExplodedViewCluster[] clusters,
                                      List<int> stackClusterIds, int laneAxis, double laneSign,
                                      BoundingBox assemblyBoundingBox, double assemblyDiagonal, double[] distances)
        {
            double cursor = GetLaneMax(assemblyBoundingBox, laneAxis, laneSign);
            int previousClusterId = -1;
            //
            foreach (int clusterId in stackClusterIds)
            {
                double gap = GetGap(graph, previousClusterId, clusterId, assemblyDiagonal);
                //
                double near = GetLaneMin(clusters[clusterId].BoundingBox, laneAxis, laneSign);
                double extent = GetLaneMax(clusters[clusterId].BoundingBox, laneAxis, laneSign) - near;
                //
                distances[clusterId] = cursor + gap - near;
                if (distances[clusterId] < gap) distances[clusterId] = gap;
                //
                cursor = near + distances[clusterId] + extent;
                previousClusterId = clusterId;
            }
        }
        /// <summary>
        /// Pushes the parts of a stack apart again after the copies of one part were given a common
        /// distance, without pulling any of them back in
        /// </summary>
        private static void RepackStack(DisassemblyContactGraph graph, ExplodedViewCluster[] clusters,
                                        List<int> stackClusterIds, int laneAxis, double laneSign,
                                        BoundingBox assemblyBoundingBox, double assemblyDiagonal, double[] distances)
        {
            double cursor = GetLaneMax(assemblyBoundingBox, laneAxis, laneSign);
            int previousClusterId = -1;
            //
            foreach (int clusterId in stackClusterIds)
            {
                double gap = GetGap(graph, previousClusterId, clusterId, assemblyDiagonal);
                //
                double near = GetLaneMin(clusters[clusterId].BoundingBox, laneAxis, laneSign);
                double extent = GetLaneMax(clusters[clusterId].BoundingBox, laneAxis, laneSign) - near;
                //
                if (near + distances[clusterId] < cursor + gap) distances[clusterId] = cursor + gap - near;
                //
                cursor = near + distances[clusterId] + extent;
                previousClusterId = clusterId;
            }
        }
        private static double GetGap(DisassemblyContactGraph graph, int previousClusterId, int clusterId,
                                     double assemblyDiagonal)
        {
            if (previousClusterId < 0) return GapFactor * assemblyDiagonal;
            //
            bool touching = graph.GetInterface(previousClusterId, clusterId) != null;
            return (touching ? GapFactor : GroupGapFactor) * assemblyDiagonal;
        }
        /// <summary>
        /// Gives the copies of one part travelling along one lane the same distance, so that the bolts of a
        /// bolt circle end up in one row however deep each of them sat
        /// </summary>
        private static void EqualizeCopies(DisassemblyContactGraph graph, int count, int[] laneAxes,
                                           double[] laneSigns, double[] distances)
        {
            string[] keys = new string[count];
            Dictionary<string, double> distanceByCopy = new Dictionary<string, double>();
            //
            for (int i = 0; i < count; i++)
            {
                if (laneAxes[i] < 0) continue;
                //
                keys[i] = laneAxes[i] + "|" + laneSigns[i] + "|" + graph.Clusters[i].GetShapeSignature();
                //
                double distance;
                if (!distanceByCopy.TryGetValue(keys[i], out distance) || distances[i] > distance)
                    distanceByCopy[keys[i]] = distances[i];
            }
            //
            for (int i = 0; i < count; i++)
            {
                if (laneAxes[i] < 0) continue;
                //
                distances[i] = distanceByCopy[keys[i]];
            }
        }
        private static void WriteOffsets(DirectionalDisassemblyPlan plan, int count, int[] laneAxes,
                                         double[] laneSigns, double[] distances, int[] lastSteps)
        {
            int stepCount = plan.Moves.Length;
            //
            for (int i = 0; i < count; i++)
            {
                double[] offset = new double[3];
                if (laneAxes[i] >= 0) offset[laneAxes[i]] = laneSigns[i] * distances[i];
                //
                plan.ClusterOffsets[i] = offset;
                //
                double[][] stepOffsets = new double[stepCount][];
                for (int step = 0; step < stepCount; step++) stepOffsets[step] = new double[3];
                if (lastSteps[i] >= 0 && lastSteps[i] < stepCount) stepOffsets[lastSteps[i]] = offset.ToArray();
                //
                plan.ClusterStepOffsets[i] = stepOffsets;
            }
        }
        private static double GetLaneMin(BoundingBox box, int laneAxis, double laneSign)
        {
            return laneSign > 0 ? GetMin(box, laneAxis) : -GetMax(box, laneAxis);
        }
        private static double GetLaneMax(BoundingBox box, int laneAxis, double laneSign)
        {
            return laneSign > 0 ? GetMax(box, laneAxis) : -GetMin(box, laneAxis);
        }
        private static double GetMin(BoundingBox box, int axis)
        {
            if (axis == 0) return box.MinX;
            else if (axis == 1) return box.MinY;
            else return box.MinZ;
        }
        private static double GetMax(BoundingBox box, int axis)
        {
            if (axis == 0) return box.MaxX;
            else if (axis == 1) return box.MaxY;
            else return box.MaxZ;
        }
    }
}
