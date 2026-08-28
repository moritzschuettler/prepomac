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
using System.Text;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The third stage of the disassembly direction exploded view: it turns the contact graph with the
    /// resolved directions into the offset of every cluster.
    ///
    /// The cluster that holds the assembly together - the one with the most interfaces, the largest of
    /// them when there are several - stays in place, and the rest is hung onto it as a tree. The tree
    /// follows the strongest interface of each cluster: a part hangs first of all on the shaft that runs
    /// through it, then on the hole it is itself inserted into, and only then on what it merely touches
    /// with a face. A fastener carries only what is no larger than itself, so the parts a bolt holds
    /// together are mounted on each other and the bolt hangs off the largest of them, while the nuts and
    /// the washers on that bolt are mounted on the bolt.
    ///
    /// A cluster inherits the offset of the one it is mounted on and adds its own travel along the
    /// direction of the interface between them, so a subassembly unfolds as a chain instead of every part
    /// flying away from a common centre. The clusters that leave one cluster along the same direction are
    /// placed in the order they are already stacked along it, so the explosion spreads a stack out without
    /// reordering it.
    ///
    /// The travel is the closed form distance at which the moving bounding box stops overlapping every
    /// box already placed. Because each cluster clears all of its predecessors and none of them is moved
    /// again, the finished layout has no overlapping pair and needs no relaxation. A pair that can never
    /// be separated along the direction - which the direction filter can force - is left overlapping
    /// instead of being pushed away without a bound.
    /// </summary>
    public class DisassemblyExplodedViewLayout
    {
        // Variables                                                                                                                
        public static double ClearanceFactor = 0.1;
        public static double MinClearanceFactor = 0.01;
        public static int DirectionGroupDigits = 2;
        public static int MinFastenerAxialInterfaceCount = 2;
        public static double MaxFastenerVolumeFactor = 0.2;
        //
        private const double _tiny = 1E-12;
        //
        private DisassemblyContactGraph _graph;
        private int _rootClusterId;
        private double[][] _clusterOffsets;
        private double[][][] _clusterStepOffsets;
        private int[] _parentClusterIds;
        private bool[] _isFastener;


        // Constructors                                                                                                             
        private DisassemblyExplodedViewLayout(DisassemblyContactGraph graph, int rootClusterId,
                                             double[][] clusterOffsets, double[][][] clusterStepOffsets,
                                             int[] parentClusterIds, bool[] isFastener)
        {
            _graph = graph;
            _rootClusterId = rootClusterId;
            _clusterOffsets = clusterOffsets;
            _clusterStepOffsets = clusterStepOffsets;
            _parentClusterIds = parentClusterIds;
            _isFastener = isFastener;
        }


        // Static methods                                                                                                           
        public static DisassemblyExplodedViewLayout Create(DisassemblyContactGraph graph, int explodedDirection)
        {
            if (graph == null || graph.Clusters.Length == 0) return null;
            //
            ExplodedViewCluster[] clusters = graph.Clusters;
            BoundingBox assemblyBoundingBox = new BoundingBox();
            foreach (ExplodedViewCluster cluster in clusters) assemblyBoundingBox.IncludeBox(cluster.BoundingBox);
            double assemblyDiagonal = assemblyBoundingBox.GetDiagonal();
            Vec3D assemblyCenter = new Vec3D(assemblyBoundingBox.GetCenter());
            //
            int rootClusterId = GetRootClusterId(graph);
            //
            double[][] clusterOffsets = new double[clusters.Length][];
            double[][][] clusterStepOffsets = new double[clusters.Length][][];
            List<BoundingBox> placedBoxes = new List<BoundingBox>();
            //
            // The root does not move and takes no step of its own, so the first step is the one that moves
            // the clusters mounted directly onto it
            clusterOffsets[rootClusterId] = new double[3];
            clusterStepOffsets[rootClusterId] = new double[0][];
            placedBoxes.Add(new BoundingBox(clusters[rootClusterId].BoundingBox));
            //
            Vec3D travelDirection;
            double travel;
            //
            bool[] isFastener = GetFastenerFlags(graph);
            int[] parentClusterIds = GetSpanningTree(graph, rootClusterId, isFastener);
            //
            foreach (int[] parentAndChildId in GetPlacementOrder(graph, rootClusterId, parentClusterIds,
                                                                 explodedDirection, assemblyCenter))
            {
                ExplodedViewCluster parentCluster = clusters[parentAndChildId[0]];
                ExplodedViewCluster childCluster = clusters[parentAndChildId[1]];
                Vec3D parentOffset = new Vec3D(clusterOffsets[parentCluster.Id]);
                //
                travelDirection = GetTravelDirection(graph, parentCluster, childCluster, explodedDirection,
                                                     assemblyCenter);
                travel = GetTravel(childCluster, parentOffset, travelDirection, placedBoxes, assemblyDiagonal);
                //
                Place(childCluster, parentCluster.Id, parentOffset, travel, travelDirection, clusterOffsets,
                      clusterStepOffsets, placedBoxes);
            }
            // The clusters that no interface connects to the root
            foreach (ExplodedViewCluster cluster in GetUnplacedClusters(clusters, clusterOffsets))
            {
                travelDirection = GetFilteredDirection(GetRadialDirection(cluster, assemblyCenter, assemblyCenter),
                                                       explodedDirection);
                travel = GetTravel(cluster, new Vec3D(), travelDirection, placedBoxes, assemblyDiagonal);
                //
                Place(cluster, -1, new Vec3D(), travel, travelDirection, clusterOffsets, clusterStepOffsets,
                      placedBoxes);
            }
            //
            return new DisassemblyExplodedViewLayout(graph, rootClusterId, clusterOffsets, clusterStepOffsets,
                                                    parentClusterIds, isFastener);
        }
        private static int GetRootClusterId(DisassemblyContactGraph graph)
        {
            ExplodedViewCluster rootCluster = null;
            int rootDegree = -1;
            //
            foreach (ExplodedViewCluster cluster in graph.Clusters)
            {
                int degree = graph.GetDegree(cluster.Id);
                if (rootCluster == null || degree > rootDegree ||
                    (degree == rootDegree && IsLarger(cluster, rootCluster)))
                {
                    rootCluster = cluster;
                    rootDegree = degree;
                }
            }
            //
            return rootCluster.Id;
        }
        private static bool IsLarger(ExplodedViewCluster cluster, ExplodedViewCluster otherCluster)
        {
            double volume = Tools.RoundToSignificantDigits(cluster.Volume, 6);
            double otherVolume = Tools.RoundToSignificantDigits(otherCluster.Volume, 6);
            //
            if (volume > otherVolume) return true;
            else if (volume < otherVolume) return false;
            else return string.CompareOrdinal(cluster.GetName(), otherCluster.GetName()) < 0;
        }
        /// <summary>
        /// The clusters that only hold other clusters together instead of being the assembly themselves:
        /// the bolts, pins, bearings and washers that sit on an axis running through larger parts.
        ///
        /// Such a cluster is recognized by having more than one axial interface - it is threaded through
        /// more than one part - while being small next to the largest part it is threaded through. A casing
        /// bored through by several shafts also has many axial interfaces but is not small, so the size
        /// test is what tells the two apart.
        /// </summary>
        private static bool[] GetFastenerFlags(DisassemblyContactGraph graph)
        {
            bool[] isFastener = new bool[graph.Clusters.Length];
            //
            foreach (ExplodedViewCluster cluster in graph.Clusters)
            {
                int axialInterfaceCount = 0;
                double maxConnectedVolume = 0;
                //
                foreach (ExplodedViewInterface contactInterface in graph.GetInterfaces(cluster.Id))
                {
                    if (!contactInterface.IsAxialDirection) continue;
                    //
                    axialInterfaceCount++;
                    //
                    double volume = graph.Clusters[contactInterface.GetOtherClusterId(cluster.Id)].Volume;
                    if (volume > maxConnectedVolume) maxConnectedVolume = volume;
                }
                //
                isFastener[cluster.Id] = axialInterfaceCount >= MinFastenerAxialInterfaceCount &&
                                         cluster.Volume < MaxFastenerVolumeFactor * maxConnectedVolume;
            }
            //
            return isFastener;
        }
        /// <summary>
        /// The cluster every cluster hangs on. The tree is grown from the root one interface at a time and
        /// the strongest interface available is always taken first: an axial one before a planar one, and
        /// the larger before the smaller. A part held by a bore therefore belongs to the subassembly of the
        /// part whose bore holds it, and not to whatever it happens to touch with a face, which is also the
        /// interface whose direction it is then moved along.
        ///
        /// A part larger than a fastener is not mounted on it while any other way of reaching it is left:
        /// two plates held together by a bolt are mounted one on the other and the bolt hangs off the larger
        /// of them, instead of the plates hanging off the bolt. The parts the fastener itself carries - the
        /// nuts and the washers on that bolt - are mounted on it as usual.
        /// </summary>
        private static int[] GetSpanningTree(DisassemblyContactGraph graph, int rootClusterId, bool[] isFastener)
        {
            int[] parentClusterIds = new int[graph.Clusters.Length];
            for (int i = 0; i < parentClusterIds.Length; i++) parentClusterIds[i] = -1;
            //
            HashSet<int> treeClusterIds = new HashSet<int>();
            treeClusterIds.Add(rootClusterId);
            //
            while (true)
            {
                ExplodedViewInterface bestInterface = null;
                int bestParentClusterId = -1;
                int bestClusterId = -1;
                //
                foreach (int treeClusterId in treeClusterIds)
                {
                    foreach (ExplodedViewInterface contactInterface in graph.GetInterfaces(treeClusterId))
                    {
                        int clusterId = contactInterface.GetOtherClusterId(treeClusterId);
                        if (treeClusterIds.Contains(clusterId)) continue;
                        //
                        if (bestInterface == null ||
                            IsStrongerInterface(contactInterface, graph.Clusters[treeClusterId],
                                                graph.Clusters[clusterId], isFastener[treeClusterId],
                                                bestInterface, graph.Clusters[bestParentClusterId],
                                                graph.Clusters[bestClusterId], isFastener[bestParentClusterId]))
                        {
                            bestInterface = contactInterface;
                            bestParentClusterId = treeClusterId;
                            bestClusterId = clusterId;
                        }
                    }
                }
                //
                if (bestInterface == null) break;
                //
                parentClusterIds[bestClusterId] = bestParentClusterId;
                treeClusterIds.Add(bestClusterId);
            }
            //
            return parentClusterIds;
        }
        private static bool IsStrongerInterface(ExplodedViewInterface contactInterface,
                                                ExplodedViewCluster parentCluster, ExplodedViewCluster cluster,
                                                bool parentIsFastener,
                                                ExplodedViewInterface bestInterface,
                                                ExplodedViewCluster bestParentCluster, ExplodedViewCluster bestCluster,
                                                bool bestParentIsFastener)
        {
            int rank = GetInterfaceRank(contactInterface, parentCluster, cluster, parentIsFastener);
            int bestRank = GetInterfaceRank(bestInterface, bestParentCluster, bestCluster, bestParentIsFastener);
            if (rank != bestRank) return rank > bestRank;
            // Of two parts that could hold this one, the larger one is the casing it is mounted to
            double parentVolume = Tools.RoundToSignificantDigits(parentCluster.Volume, 6);
            double bestParentVolume = Tools.RoundToSignificantDigits(bestParentCluster.Volume, 6);
            if (parentVolume != bestParentVolume) return parentVolume > bestParentVolume;
            //
            double area = Tools.RoundToSignificantDigits(contactInterface.Area, 6);
            double bestArea = Tools.RoundToSignificantDigits(bestInterface.Area, 6);
            if (area != bestArea) return area > bestArea;
            //
            return string.CompareOrdinal(cluster.GetName(), bestCluster.GetName()) < 0;
        }
        /// <summary>
        /// How well an interface holds the child on the parent.
        ///
        /// Only a shaft running through a hole mounts one part on the other. Two parts that both present a
        /// hole on the same axis - the bore of a casing and the bore of the nut sitting on it - are both
        /// mounted on the shaft that runs through them and never on each other, so their contact counts as
        /// a planar one however well their bores line up.
        ///
        /// Of the two ways a shaft and a hole can meet, a part belongs first of all to the shaft that runs
        /// through it and only then to the hole it is itself inserted into. A nut sitting in a counterbore
        /// runs through that counterbore, but the bolt runs through the nut, so the nut belongs to the bolt.
        ///
        /// A fastener carries only the parts that are no larger than it is. A nut or a washer on a bolt
        /// belongs to that bolt and comes off it, while a casing the same bolt passes through is only held
        /// together by it and must not hang on it: that one ranks below everything else and is used only
        /// when the casing cannot be reached any other way.
        /// </summary>
        private static int GetInterfaceRank(ExplodedViewInterface contactInterface, ExplodedViewCluster parentCluster,
                                            ExplodedViewCluster cluster, bool parentIsFastener)
        {
            if (parentIsFastener &&
                Tools.RoundToSignificantDigits(cluster.Volume, 6) >
                Tools.RoundToSignificantDigits(parentCluster.Volume, 6)) return 0;
            //
            if (!contactInterface.HasDirection) return 1;
            else if (!contactInterface.IsAxialMount) return 2;
            else if (contactInterface.ShaftClusterId == parentCluster.Id) return 4;
            else return 3;
        }
        /// <summary>
        /// The parent and child cluster id of every edge of the tree, walked level by level so that a parent
        /// is always placed before its children
        /// </summary>
        private static List<int[]> GetPlacementOrder(DisassemblyContactGraph graph, int rootClusterId,
                                                     int[] parentClusterIds, int explodedDirection,
                                                     Vec3D assemblyCenter)
        {
            List<int>[] childClusterIds = new List<int>[parentClusterIds.Length];
            for (int i = 0; i < childClusterIds.Length; i++) childClusterIds[i] = new List<int>();
            for (int i = 0; i < parentClusterIds.Length; i++)
            {
                if (parentClusterIds[i] >= 0) childClusterIds[parentClusterIds[i]].Add(i);
            }
            //
            List<int[]> order = new List<int[]>();
            Queue<int> clusterIdQueue = new Queue<int>();
            clusterIdQueue.Enqueue(rootClusterId);
            //
            while (clusterIdQueue.Count > 0)
            {
                int clusterId = clusterIdQueue.Dequeue();
                //
                foreach (int childClusterId in GetSortedChildIds(graph, clusterId, childClusterIds[clusterId],
                                                                 explodedDirection, assemblyCenter))
                {
                    clusterIdQueue.Enqueue(childClusterId);
                    order.Add(new int[] { clusterId, childClusterId });
                }
            }
            //
            return order;
        }
        /// <summary>
        /// The children of a cluster, grouped by the direction they travel along and inside a group ordered
        /// the way they are already stacked along it.
        ///
        /// Each cluster is placed clear of everything placed before it, so the order in which the children
        /// are placed is the order they end up in. Taking that order from the leading end of each cluster
        /// along the travel direction keeps a stack of rings on a shaft, or of plates on a bolt, in the
        /// order it had before the explosion. When two of them lead at the same point the one that reaches
        /// further back is placed later and so ends up outside: a bolt spans the plate it holds, and comes
        /// out beyond it.
        /// </summary>
        private static List<int> GetSortedChildIds(DisassemblyContactGraph graph, int clusterId,
                                                   List<int> childClusterIds, int explodedDirection,
                                                   Vec3D assemblyCenter)
        {
            ExplodedViewCluster parentCluster = graph.Clusters[clusterId];
            Dictionary<int, double[]> sortKeys = new Dictionary<int, double[]>();
            //
            foreach (int childClusterId in childClusterIds)
            {
                ExplodedViewCluster childCluster = graph.Clusters[childClusterId];
                Vec3D travelDirection = GetTravelDirection(graph, parentCluster, childCluster, explodedDirection,
                                                           assemblyCenter);
                //
                double[] sortKey = new double[5];
                if (travelDirection != null)
                {
                    double[] roundedDirection = travelDirection.CoorRounded(DirectionGroupDigits);
                    sortKey[0] = roundedDirection[0];
                    sortKey[1] = roundedDirection[1];
                    sortKey[2] = roundedDirection[2];
                    //
                    double min;
                    double max;
                    childCluster.BoundingBox.GetProjection(travelDirection.Coor, out min, out max);
                    sortKey[3] = max;
                    sortKey[4] = -min;
                }
                //
                sortKeys.Add(childClusterId, sortKey);
            }
            //
            return childClusterIds.OrderBy(childClusterId => sortKeys[childClusterId][0])
                                  .ThenBy(childClusterId => sortKeys[childClusterId][1])
                                  .ThenBy(childClusterId => sortKeys[childClusterId][2])
                                  .ThenBy(childClusterId => sortKeys[childClusterId][3])
                                  .ThenBy(childClusterId => sortKeys[childClusterId][4])
                                  .ThenBy(childClusterId => graph.Clusters[childClusterId].GetName(),
                                          StringComparer.Ordinal)
                                  .ToList();
        }
        private static Vec3D GetTravelDirection(DisassemblyContactGraph graph, ExplodedViewCluster parentCluster,
                                                ExplodedViewCluster childCluster, int explodedDirection,
                                                Vec3D assemblyCenter)
        {
            ExplodedViewInterface contactInterface = graph.GetInterface(parentCluster.Id, childCluster.Id);
            Vec3D direction = contactInterface != null ? contactInterface.GetDirection(childCluster.Id) : null;
            //
            Vec3D travelDirection = GetFilteredDirection(direction, explodedDirection);
            if (travelDirection != null) return travelDirection;
            //
            Vec3D radialDirection = GetRadialDirection(childCluster, new Vec3D(parentCluster.Center), assemblyCenter);
            return GetFilteredDirection(radialDirection, explodedDirection);
        }
        private static List<ExplodedViewCluster> GetUnplacedClusters(ExplodedViewCluster[] clusters,
                                                                     double[][] clusterOffsets)
        {
            return clusters.Where(cluster => clusterOffsets[cluster.Id] == null)
                           .OrderByDescending(cluster => Tools.RoundToSignificantDigits(cluster.Volume, 6))
                           .ThenBy(cluster => cluster.GetName(), StringComparer.Ordinal)
                           .ToList();
        }
        private static Vec3D GetRadialDirection(ExplodedViewCluster cluster, Vec3D fromCenter, Vec3D assemblyCenter)
        {
            Vec3D direction = new Vec3D(cluster.Center) - fromCenter;
            if (direction.Len2 > _tiny) return direction;
            //
            direction = new Vec3D(cluster.Center) - assemblyCenter;
            if (direction.Len2 > _tiny) return direction;
            //
            return null;
        }
        private static double GetTravel(ExplodedViewCluster cluster, Vec3D parentOffset, Vec3D travelDirection,
                                        List<BoundingBox> placedBoxes, double assemblyDiagonal)
        {
            if (travelDirection == null) return 0;
            //
            BoundingBox movingBox = new BoundingBox(cluster.BoundingBox);
            movingBox.AddOffset(parentOffset.Coor);
            //
            double travel = 0;
            foreach (BoundingBox placedBox in placedBoxes)
            {
                double separation = GetSeparation(movingBox, placedBox, travelDirection);
                if (double.IsInfinity(separation)) continue;
                if (separation > travel) travel = separation;
            }
            //
            return travel + Math.Max(ClearanceFactor * cluster.BoundingBox.GetDiagonal(),
                                     MinClearanceFactor * assemblyDiagonal);
        }
        private static Vec3D GetFilteredDirection(Vec3D direction, int explodedDirection)
        {
            if (direction == null) return null;
            //
            Vec3D filteredDirection = new Vec3D(direction);
            if (explodedDirection == 1) filteredDirection.Y = filteredDirection.Z = 0;
            else if (explodedDirection == 2) filteredDirection.X = filteredDirection.Z = 0;
            else if (explodedDirection == 3) filteredDirection.X = filteredDirection.Y = 0;
            else if (explodedDirection == 4) filteredDirection.Z = 0;
            else if (explodedDirection == 5) filteredDirection.Y = 0;
            else if (explodedDirection == 6) filteredDirection.X = 0;
            //
            if (filteredDirection.Len2 <= _tiny) return null;
            //
            filteredDirection.Normalize();
            return filteredDirection;
        }
        /// <summary>
        /// The smallest distance along the direction at which the moving box stops overlapping the static
        /// one and keeps clear of it, or infinity when the direction can never separate the two
        /// </summary>
        private static double GetSeparation(BoundingBox movingBox, BoundingBox staticBox, Vec3D direction)
        {
            double separation = double.PositiveInfinity;
            //
            for (int axis = 0; axis < 3; axis++)
            {
                double component = GetComponent(direction, axis);
                double movingMin = GetMin(movingBox, axis);
                double movingMax = GetMax(movingBox, axis);
                double staticMin = GetMin(staticBox, axis);
                double staticMax = GetMax(staticBox, axis);
                //
                double axisSeparation;
                if (component > _tiny) axisSeparation = (staticMax - movingMin) / component;
                else if (component < -_tiny) axisSeparation = (staticMin - movingMax) / component;
                else if (movingMax <= staticMin || movingMin >= staticMax) axisSeparation = 0;
                else axisSeparation = double.PositiveInfinity;
                //
                if (axisSeparation < 0) axisSeparation = 0;
                if (axisSeparation < separation) separation = axisSeparation;
            }
            //
            return separation;
        }
        /// <summary>
        /// Stores the offset of the cluster together with the step it belongs to: the steps of its parent
        /// followed by the travel of its own, which is the one step the cluster moves on its own instead of
        /// being carried along by the cluster it hangs on
        /// </summary>
        private static void Place(ExplodedViewCluster cluster, int parentClusterId, Vec3D parentOffset, double travel,
                                  Vec3D travelDirection, double[][] clusterOffsets, double[][][] clusterStepOffsets,
                                  List<BoundingBox> placedBoxes)
        {
            Vec3D offset = parentOffset;
            Vec3D stepOffset = new Vec3D();
            if (travelDirection != null && travel > 0)
            {
                stepOffset = travel * travelDirection;
                offset = parentOffset + stepOffset;
            }
            //
            clusterOffsets[cluster.Id] = offset.Coor;
            //
            double[][] parentStepOffsets = parentClusterId >= 0 ? clusterStepOffsets[parentClusterId] : new double[0][];
            double[][] stepOffsets = new double[parentStepOffsets.Length + 1][];
            for (int i = 0; i < parentStepOffsets.Length; i++) stepOffsets[i] = parentStepOffsets[i];
            stepOffsets[parentStepOffsets.Length] = stepOffset.Coor;
            clusterStepOffsets[cluster.Id] = stepOffsets;
            //
            BoundingBox placedBox = new BoundingBox(cluster.BoundingBox);
            placedBox.AddOffset(offset.Coor);
            placedBoxes.Add(placedBox);
        }
        private static double GetComponent(Vec3D vector, int axis)
        {
            if (axis == 0) return vector.X;
            else if (axis == 1) return vector.Y;
            else return vector.Z;
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


        // Methods                                                                                                                  
        public ExplodedViewStagedOffsets GetStagedOffsets()
        {
            Dictionary<string, double[][]> stepOffsetsByPart = new Dictionary<string, double[][]>();
            //
            foreach (ExplodedViewCluster cluster in _graph.Clusters)
            {
                double[][] stepOffsets = _clusterStepOffsets[cluster.Id];
                foreach (string partName in cluster.PartNames) stepOffsetsByPart.Add(partName, stepOffsets);
            }
            //
            return new ExplodedViewStagedOffsets(stepOffsetsByPart);
        }
        public string GetSummary()
        {
            StringBuilder summary = new StringBuilder();
            summary.Append(_graph.GetSummary());
            summary.AppendLine();
            summary.AppendLine("Disassembly exploded view subassemblies");
            summary.AppendLine("Base: " + _graph.Clusters[_rootClusterId].GetName());
            //
            foreach (ExplodedViewCluster cluster in _graph.Clusters)
            {
                double[] offset = _clusterOffsets[cluster.Id];
                int parentClusterId = _parentClusterIds[cluster.Id];
                //
                string parent;
                if (cluster.Id == _rootClusterId) parent = "-";
                else if (parentClusterId < 0) parent = "not connected";
                else parent = _graph.Clusters[parentClusterId].GetName();
                //
                summary.AppendLine("  [" + cluster.Id + "] " + cluster.GetName() +
                                   (_isFastener[cluster.Id] ? " (fastener)" : "") +
                                   "  level: " + _clusterStepOffsets[cluster.Id].Length +
                                   "  mounted on: " + parent +
                                   "  offset: (" + offset[0].ToString("G4") + ", " + offset[1].ToString("G4") + ", " +
                                   offset[2].ToString("G4") + ")");
            }
            //
            return summary.ToString();
        }
    }
}
