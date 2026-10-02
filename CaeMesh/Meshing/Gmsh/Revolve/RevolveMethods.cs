// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;
using GmshCommon;

namespace CaeMesh
{
    public static class RevolveMethods
    {
        private enum GmshElementTypeEnum
        {
            LinearBeam = 1,
            LinearTriangle = 2,
            LinearQuadrilateral = 3,
            LinearTetra = 4,
            LinearHexa = 5,
            LinearWedge = 6,
            LinearPyramid = 7
        }

        private struct EntityKey : IEquatable<EntityKey>
        {
            public readonly int Dim;
            public readonly int Tag;

            public EntityKey(int dim, int tag)
            {
                Dim = dim;
                Tag = tag;
            }

            public bool Equals(EntityKey other)
            {
                return Dim == other.Dim && Tag == other.Tag;
            }

            public override bool Equals(object obj)
            {
                return obj is EntityKey && Equals((EntityKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked { return (Dim * 397) ^ Tag; }
            }
        }

        private sealed class TopologyData
        {
            public int VolumeId;
            public int[] VertexIds;
            public int[] EdgeIds;
            public int[] SurfaceIds;
            public Dictionary<int, double[]> VertexCoordinates;
            public Dictionary<int, double[]> EdgeCenters;
            public Dictionary<int, double[]> SurfaceCenters;
            public Dictionary<int, int[]> EdgeVertexIds;
            public Dictionary<int, int[]> EdgeSurfaceIds;
            public Dictionary<int, int[]> SurfaceEdgeIds;
            public Dictionary<int, HashSet<int>> SurfaceVertexIds;
            public double Tolerance;
        }

        private sealed class ElementRecord
        {
            public int EntityId;
            public int ElementType;
            public IntPtr[] NodeIds;
        }

        private sealed class NodeBatch
        {
            public readonly List<IntPtr> NodeIds = new List<IntPtr>();
            public readonly List<double> Coordinates = new List<double>();
        }

        private sealed class ElementBatch
        {
            public readonly int NumNodes;
            public readonly List<IntPtr> ElementIds = new List<IntPtr>();
            public readonly List<IntPtr> NodeIds = new List<IntPtr>();

            public ElementBatch(int numNodes)
            {
                NumNodes = numNodes;
            }
        }

        public static void CreateRevolveMesh(HashSet<int> sourceSurfaceIds, double[] axisCenter,
                                             double[] axisDirection, double angleRad,
                                             int[] numElements, double[] heights, Action meshSourceSurfaces)
        {
            if (sourceSurfaceIds == null || sourceSurfaceIds.Count == 0)
                throw new CaeException("No source surfaces were selected for the revolve mesh.");
            if (axisCenter == null || axisCenter.Length != 3 ||
                axisDirection == null || axisDirection.Length != 3)
                throw new CaeException("The revolve axis could not be determined.");
            if (meshSourceSurfaces == null)
                throw new CaeException("The revolve mesh source-surface meshing step was not provided.");

            double[] unitAxis = Normalize(axisDirection);
            double[] layerFractions = GetLayerFractions(numElements, heights);
            TopologyData topology = GetTopologyData();

            HashSet<int> sourceEdgeIds = new HashSet<int>();
            foreach (int surfaceId in sourceSurfaceIds)
            {
                if (!topology.SurfaceEdgeIds.ContainsKey(surfaceId))
                    throw new CaeException("The selected source surface does not exist in the Gmsh model.");
                sourceEdgeIds.UnionWith(topology.SurfaceEdgeIds[surfaceId]);
            }

            HashSet<int> sourceVertexIds = new HashSet<int>();
            foreach (int edgeId in sourceEdgeIds) sourceVertexIds.UnionWith(topology.EdgeVertexIds[edgeId]);

            Dictionary<int, int> boundaryEdgeUseCount = new Dictionary<int, int>();
            foreach (int surfaceId in sourceSurfaceIds)
            {
                foreach (int edgeId in topology.SurfaceEdgeIds[surfaceId].Distinct())
                {
                    if (boundaryEdgeUseCount.ContainsKey(edgeId)) boundaryEdgeUseCount[edgeId]++;
                    else boundaryEdgeUseCount.Add(edgeId, 1);
                }
            }
            HashSet<int> boundaryEdgeIds = new HashSet<int>(boundaryEdgeUseCount.Where(x => x.Value == 1)
                                                                                .Select(x => x.Key));
            HashSet<int> boundaryVertexIds = new HashSet<int>();
            foreach (int boundaryEdgeId in boundaryEdgeIds)
                boundaryVertexIds.UnionWith(topology.EdgeVertexIds[boundaryEdgeId]);

            // Mesh only the source faces, in isolation. The solid, target and side meshes are built analytically
            // from the source-surface mesh, so the mesh sizing of the other faces and the volume is irrelevant.
            // They are removed before meshing (as the revolve preview does) because a neighbour-aware 2D algorithm
            // (Quasi-structured quad) meshes a source face differently when the adjacent faces are still present.
            int volumeId = IsolateSourceSurfacesAndMesh(topology, sourceSurfaceIds, meshSourceSurfaces);

            // The revolved boundary is built entirely on discrete entities, since the CAD target/side faces were
            // removed. Each rotated boundary source edge keeps its own side surface and each rotated boundary
            // source vertex its own connecting edge; interior source edges/vertices sweep the volume interior and
            // create no side surface, connecting edge or internal partition surface.
            HashSet<int> axisEdgeIds = GetAxisEdgeIds(boundaryEdgeIds, topology, axisCenter, unitAxis);
            HashSet<int> sourceAxisEdgeIds = GetAxisEdgeIds(sourceEdgeIds, topology, axisCenter, unitAxis);
            Dictionary<int, int> sideSurfaceIds = CreateDiscreteSideSurfaceIds(boundaryEdgeIds, axisEdgeIds);
            Dictionary<int, int> targetSurfaceIds = CreateDiscreteTargetSurfaceIds(sourceSurfaceIds);
            Dictionary<int, int> targetVertexIds = CreateDiscreteTargetVertexIds(sourceVertexIds, topology,
                                                                                  axisCenter, unitAxis);
            Dictionary<int, int> targetEdgeIds = CreateDiscreteTargetEdgeIds(sourceEdgeIds, sourceAxisEdgeIds);
            Dictionary<int, int> targetSurfaceBySourceVertexIds = new Dictionary<int, int>();
            Dictionary<int, int> targetSurfaceBySourceEdgeIds = new Dictionary<int, int>();
            Dictionary<int, int> connectingEdgeIds = GetConnectingEdgeIds(sourceVertexIds, boundaryVertexIds,
                                                                           targetVertexIds, sourceEdgeIds,
                                                                           topology, axisCenter, unitAxis,
                                                                           angleRad);
            Dictionary<IntPtr, double[]> sourceNodeCoordinates;
            Dictionary<IntPtr, EntityKey> sourceNodeEntities;
            GetSourceNodes(sourceSurfaceIds, sourceEdgeIds, sourceVertexIds,
                           out sourceNodeCoordinates, out sourceNodeEntities);

            List<ElementRecord> sourceSurfaceElements = GetElements(2, sourceSurfaceIds);
            List<ElementRecord> sourceEdgeElements = GetElements(1, sourceEdgeIds);
            if (sourceSurfaceElements.Count == 0)
                throw new CaeException("Gmsh did not create a mesh on the selected source surfaces.");

            long maxNodeId = GetMaxNodeId();
            long maxElementId = GetMaxElementId();

            ClearStrayNonSourceMesh(topology, sourceEdgeIds, sourceVertexIds);

            Dictionary<IntPtr, IntPtr[]> nodeIdLayerIds;
            AddRevolvedNodes(sourceNodeCoordinates, sourceNodeEntities, layerFractions,
                             targetVertexIds, targetEdgeIds, targetSurfaceIds,
                             targetSurfaceBySourceVertexIds, targetSurfaceBySourceEdgeIds,
                             boundaryEdgeIds, sideSurfaceIds, connectingEdgeIds,
                             volumeId, axisCenter, unitAxis, angleRad, topology.Tolerance,
                             ref maxNodeId, out nodeIdLayerIds);

            AddRevolvedElements(sourceSurfaceElements, sourceEdgeElements, sourceNodeCoordinates,
                                nodeIdLayerIds, layerFractions, targetEdgeIds, targetSurfaceIds,
                                boundaryEdgeIds, sideSurfaceIds, connectingEdgeIds,
                                volumeId, axisCenter, unitAxis, angleRad, topology.Tolerance,
                                ref maxElementId);
        }

        private static double[] GetLayerFractions(int[] numElements, double[] heights)
        {
            if (numElements == null || heights == null || numElements.Length == 0 ||
                numElements.Length != heights.Length)
                throw new CaeException("The revolve mesh layers are not properly defined.");

            List<double> fractions = new List<double>() { 0 };
            double previousHeight = 0;
            for (int i = 0; i < numElements.Length; i++)
            {
                if (numElements[i] < 1 || heights[i] <= previousHeight || heights[i] > 1 + 1E-10)
                    throw new CaeException("The revolve mesh layers are not properly defined.");
                for (int j = 1; j <= numElements[i]; j++)
                {
                    fractions.Add(previousHeight + (heights[i] - previousHeight) * j / numElements[i]);
                }
                previousHeight = heights[i];
            }
            if (Math.Abs(previousHeight - 1) > 1E-8)
                throw new CaeException("The revolve mesh layer thicknesses must span the complete revolve angle.");
            fractions[fractions.Count - 1] = 1;
            return fractions.ToArray();
        }

        private static TopologyData GetTopologyData()
        {
            TopologyData topology = new TopologyData();
            Tuple<int, int>[] volumeDimTags = Gmsh.Model.OCC.GetEntities(3);
            if (volumeDimTags.Length != 1)
                throw new CaeException("The revolve mesh can only be created for a single volume.");
            topology.VolumeId = volumeDimTags[0].Item2;
            topology.VertexIds = Gmsh.Model.OCC.GetEntities(0).Select(x => x.Item2).ToArray();
            topology.EdgeIds = Gmsh.Model.OCC.GetEntities(1).Select(x => x.Item2).ToArray();
            topology.SurfaceIds = Gmsh.Model.OCC.GetEntities(2).Select(x => x.Item2).ToArray();
            topology.VertexCoordinates = new Dictionary<int, double[]>();
            topology.EdgeCenters = new Dictionary<int, double[]>();
            topology.SurfaceCenters = new Dictionary<int, double[]>();
            topology.EdgeVertexIds = new Dictionary<int, int[]>();
            topology.EdgeSurfaceIds = new Dictionary<int, int[]>();
            topology.SurfaceEdgeIds = new Dictionary<int, int[]>();
            topology.SurfaceVertexIds = new Dictionary<int, HashSet<int>>();

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;
            foreach (int vertexId in topology.VertexIds)
            {
                double xMin, yMin, zMin, xMax, yMax, zMax;
                Gmsh.Model.GetBoundingBox(0, vertexId, out xMin, out yMin, out zMin,
                                          out xMax, out yMax, out zMax);
                double[] coordinate = new double[] { 0.5 * (xMin + xMax),
                                                     0.5 * (yMin + yMax),
                                                     0.5 * (zMin + zMax) };
                topology.VertexCoordinates.Add(vertexId, coordinate);
                minX = Math.Min(minX, coordinate[0]);
                minY = Math.Min(minY, coordinate[1]);
                minZ = Math.Min(minZ, coordinate[2]);
                maxX = Math.Max(maxX, coordinate[0]);
                maxY = Math.Max(maxY, coordinate[1]);
                maxZ = Math.Max(maxZ, coordinate[2]);
            }
            double diagonal = Math.Sqrt(Math.Pow(maxX - minX, 2) +
                                        Math.Pow(maxY - minY, 2) +
                                        Math.Pow(maxZ - minZ, 2));
            topology.Tolerance = 1E-10 + 1E-8 * Math.Max(diagonal, 1);

            foreach (int edgeId in topology.EdgeIds)
            {
                int[] surfaceIds;
                int[] vertexIds;
                Gmsh.Model.GetAdjacencies(1, edgeId, out surfaceIds, out vertexIds);
                topology.EdgeSurfaceIds.Add(edgeId, surfaceIds);
                topology.EdgeVertexIds.Add(edgeId, vertexIds);
                double x, y, z;
                Gmsh.Model.OCC.GetCenterOfMass(1, edgeId, out x, out y, out z);
                topology.EdgeCenters.Add(edgeId, new double[] { x, y, z });
            }

            foreach (int surfaceId in topology.SurfaceIds)
            {
                int[] edgeIds;
                Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
                topology.SurfaceEdgeIds.Add(surfaceId, edgeIds);
                HashSet<int> vertexIds = new HashSet<int>();
                foreach (int edgeId in edgeIds) vertexIds.UnionWith(topology.EdgeVertexIds[edgeId]);
                topology.SurfaceVertexIds.Add(surfaceId, vertexIds);
                double x, y, z;
                Gmsh.Model.OCC.GetCenterOfMass(2, surfaceId, out x, out y, out z);
                topology.SurfaceCenters.Add(surfaceId, new double[] { x, y, z });
            }
            return topology;
        }

        private static HashSet<int> GetAxisEdgeIds(HashSet<int> boundaryEdgeIds, TopologyData topology,
                                                   double[] axisCenter, double[] axisDirection)
        {
            HashSet<int> axisEdgeIds = new HashSet<int>();
            foreach (int edgeId in boundaryEdgeIds)
            {
                if (IsEdgeOnAxis(edgeId, topology, axisCenter, axisDirection)) axisEdgeIds.Add(edgeId);
            }
            return axisEdgeIds;
        }

        private static bool IsEdgeOnAxis(int edgeId, TopologyData topology,
                                         double[] axisCenter, double[] axisDirection)
        {
            if (DistanceFromAxis(topology.EdgeCenters[edgeId], axisCenter, axisDirection) > topology.Tolerance)
                return false;
            IntPtr[] nodeIds;
            double[] coordinates;
            Gmsh.Model.Mesh.GetNodes(out nodeIds, out coordinates, 1, edgeId, true, false);
            for (int i = 0; i < nodeIds.Length; i++)
            {
                double[] coordinate = new double[] { coordinates[3 * i], coordinates[3 * i + 1],
                                                     coordinates[3 * i + 2] };
                if (DistanceFromAxis(coordinate, axisCenter, axisDirection) > topology.Tolerance) return false;
            }
            return true;
        }

        private static Dictionary<int, int> CreateDiscreteSideSurfaceIds(
            HashSet<int> boundaryEdgeIds, HashSet<int> axisEdgeIds)
        {
            // One side surface per rotated boundary edge. Boundary edges on the axis sweep nothing, and
            // interior edges are excluded (they are not in boundaryEdgeIds), so no internal surface appears.
            Dictionary<int, int> sideSurfaceIds = new Dictionary<int, int>();
            foreach (int boundaryEdgeId in boundaryEdgeIds.OrderBy(x => x))
            {
                if (axisEdgeIds.Contains(boundaryEdgeId)) continue;
                sideSurfaceIds.Add(boundaryEdgeId, Gmsh.Model.AddDiscreteEntity(2, -1));
            }
            return sideSurfaceIds;
        }

        private static Dictionary<int, int> CreateDiscreteTargetSurfaceIds(
            IEnumerable<int> sourceSurfaceIds)
        {
            Dictionary<int, int> targetSurfaceIds = new Dictionary<int, int>();
            foreach (int sourceSurfaceId in sourceSurfaceIds.OrderBy(x => x))
                targetSurfaceIds.Add(sourceSurfaceId, Gmsh.Model.AddDiscreteEntity(2, -1));
            return targetSurfaceIds;
        }

        private static Dictionary<int, int> CreateDiscreteTargetVertexIds(
            IEnumerable<int> sourceVertexIds, TopologyData topology,
            double[] axisCenter, double[] axisDirection)
        {
            Dictionary<int, int> targetVertexIds = new Dictionary<int, int>();
            foreach (int sourceVertexId in sourceVertexIds.OrderBy(x => x))
            {
                if (DistanceFromAxis(topology.VertexCoordinates[sourceVertexId], axisCenter,
                                     axisDirection) <= topology.Tolerance)
                    targetVertexIds.Add(sourceVertexId, sourceVertexId);
                else
                    targetVertexIds.Add(sourceVertexId, Gmsh.Model.AddDiscreteEntity(0, -1));
            }
            return targetVertexIds;
        }

        private static Dictionary<int, int> CreateDiscreteTargetEdgeIds(
            IEnumerable<int> sourceEdgeIds, HashSet<int> sourceAxisEdgeIds)
        {
            Dictionary<int, int> targetEdgeIds = new Dictionary<int, int>();
            foreach (int sourceEdgeId in sourceEdgeIds.OrderBy(x => x))
            {
                if (sourceAxisEdgeIds.Contains(sourceEdgeId))
                    targetEdgeIds.Add(sourceEdgeId, sourceEdgeId);
                else
                    targetEdgeIds.Add(sourceEdgeId, Gmsh.Model.AddDiscreteEntity(1, -1));
            }
            return targetEdgeIds;
        }

        private static Dictionary<int, int> GetConnectingEdgeIds(HashSet<int> sourceVertexIds,
                                                                  HashSet<int> boundaryVertexIds,
                                                                  Dictionary<int, int> targetVertexIds,
                                                                  HashSet<int> sourceEdgeIds,
                                                                  TopologyData topology, double[] axisCenter,
                                                                  double[] axisDirection, double angleRad)
        {
            Dictionary<int, int> connectingEdgeIds = new Dictionary<int, int>();
            foreach (int sourceVertexId in sourceVertexIds)
            {
                // An interior source vertex (not shared with the source boundary) sweeps an arc through the
                // volume interior, so it must not create a connecting edge.
                if (!boundaryVertexIds.Contains(sourceVertexId)) continue;
                double radius = DistanceFromAxis(topology.VertexCoordinates[sourceVertexId],
                                                 axisCenter, axisDirection);
                if (radius <= topology.Tolerance) continue;

                int targetVertexId;
                int connectingEdgeId = -1;
                if (targetVertexIds.TryGetValue(sourceVertexId, out targetVertexId))
                {
                    if (targetVertexId == sourceVertexId)
                        throw new CaeException("A direct 360 degree revolve is not supported. Create the full model " +
                                               "from the generated partial revolve mesh.");
                    int[] expectedVertexIds = new int[] { sourceVertexId, targetVertexId };
                    Array.Sort(expectedVertexIds);
                    List<int> candidates = topology.EdgeIds.Where(x => !sourceEdgeIds.Contains(x) &&
                        SameIds(expectedVertexIds,
                        topology.EdgeVertexIds[x].OrderBy(y => y).ToArray())).ToList();
                    if (candidates.Count > 0)
                    {
                        double[] expectedCenter = GetArcCenter(topology.VertexCoordinates[sourceVertexId],
                                                               axisCenter, axisDirection, angleRad);
                        connectingEdgeId = candidates.OrderBy(x => Distance2(expectedCenter,
                                                             topology.EdgeCenters[x])).First();
                    }
                }
                if (connectingEdgeId < 0) connectingEdgeId = Gmsh.Model.AddDiscreteEntity(1, -1);
                connectingEdgeIds.Add(sourceVertexId, connectingEdgeId);
            }
            return connectingEdgeIds;
        }
        private static void GetSourceNodes(HashSet<int> sourceSurfaceIds, HashSet<int> sourceEdgeIds,
                                           HashSet<int> sourceVertexIds,
                                           out Dictionary<IntPtr, double[]> nodeCoordinates,
                                           out Dictionary<IntPtr, EntityKey> nodeEntities)
        {
            nodeCoordinates = new Dictionary<IntPtr, double[]>();
            Dictionary<IntPtr, int> fallbackSurfaceIds = new Dictionary<IntPtr, int>();
            foreach (int surfaceId in sourceSurfaceIds)
            {
                IntPtr[] nodeIds;
                double[] coordinates;
                Gmsh.Model.Mesh.GetNodes(out nodeIds, out coordinates, 2, surfaceId, true, false);
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    nodeCoordinates[nodeIds[i]] = new double[] { coordinates[3 * i], coordinates[3 * i + 1],
                                                                 coordinates[3 * i + 2] };
                    if (!fallbackSurfaceIds.ContainsKey(nodeIds[i])) fallbackSurfaceIds.Add(nodeIds[i], surfaceId);
                }
            }

            nodeEntities = new Dictionary<IntPtr, EntityKey>();
            AddNodeEntities(sourceVertexIds, 0, nodeEntities);
            AddNodeEntities(sourceEdgeIds, 1, nodeEntities);
            AddNodeEntities(sourceSurfaceIds, 2, nodeEntities);
            foreach (IntPtr nodeId in nodeCoordinates.Keys)
            {
                if (!nodeEntities.ContainsKey(nodeId))
                    nodeEntities.Add(nodeId, new EntityKey(2, fallbackSurfaceIds[nodeId]));
            }
        }

        private static void AddNodeEntities(IEnumerable<int> entityIds, int dim,
                                            Dictionary<IntPtr, EntityKey> nodeEntities)
        {
            foreach (int entityId in entityIds)
            {
                IntPtr[] nodeIds;
                double[] coordinates;
                Gmsh.Model.Mesh.GetNodes(out nodeIds, out coordinates, dim, entityId, false, false);
                foreach (IntPtr nodeId in nodeIds)
                {
                    if (!nodeEntities.ContainsKey(nodeId)) nodeEntities.Add(nodeId, new EntityKey(dim, entityId));
                }
            }
        }

        private static List<ElementRecord> GetElements(int dim, IEnumerable<int> entityIds)
        {
            List<ElementRecord> elements = new List<ElementRecord>();
            foreach (int entityId in entityIds)
            {
                int[] elementTypes;
                IntPtr[][] elementTags;
                IntPtr[][] nodeTags;
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, dim, entityId);
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    int numNodes = GetNumNodes(elementTypes[i]);
                    if ((dim == 1 && elementTypes[i] != (int)GmshElementTypeEnum.LinearBeam) ||
                        (dim == 2 && elementTypes[i] != (int)GmshElementTypeEnum.LinearTriangle &&
                                     elementTypes[i] != (int)GmshElementTypeEnum.LinearQuadrilateral))
                        throw new CaeException("Only first-order line, triangle and quadrilateral source elements " +
                                               "are supported by the revolve mesh.");
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        IntPtr[] elementNodeIds = new IntPtr[numNodes];
                        Array.Copy(nodeTags[i], j * numNodes, elementNodeIds, 0, numNodes);
                        elements.Add(new ElementRecord() { EntityId = entityId,
                                                           ElementType = elementTypes[i],
                                                           NodeIds = elementNodeIds });
                    }
                }
            }
            return elements;
        }

        private static int IsolateSourceSurfacesAndMesh(TopologyData topology, HashSet<int> sourceSurfaceIds,
                                                        Action meshSourceSurfaces)
        {
            // Remove the volume and the non-source faces so that only the source faces (and the edges/vertices
            // they share) remain, then mesh the source faces in isolation. A discrete volume is added afterwards
            // to receive the revolved solid elements in place of the removed CAD volume.
            List<Tuple<int, int>> toRemove = new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(3, topology.VolumeId)
            };
            foreach (int surfaceId in topology.SurfaceIds)
                if (!sourceSurfaceIds.Contains(surfaceId)) toRemove.Add(new Tuple<int, int>(2, surfaceId));
            Gmsh.Model.OCC.Remove(toRemove.ToArray(), false);
            Gmsh.Model.OCC.Synchronize();
            //
            meshSourceSurfaces();
            //
            return Gmsh.Model.AddDiscreteEntity(3, -1);
        }

        private static void ClearStrayNonSourceMesh(TopologyData topology, HashSet<int> sourceEdgeIds,
                                                    HashSet<int> sourceVertexIds)
        {
            // The volume and the non-source faces were removed, but the non-source edges/vertices they leave
            // behind still get a 1D/0D mesh. Clear that stray mesh so only the source-face mesh remains.
            ClearEntities(topology.EdgeIds.Where(x => !sourceEdgeIds.Contains(x))
                                           .Select(x => new Tuple<int, int>(1, x)).ToArray());
            ClearEntities(topology.VertexIds.Where(x => !sourceVertexIds.Contains(x))
                                             .Select(x => new Tuple<int, int>(0, x)).ToArray());
        }

        private static void ClearEntities(Tuple<int, int>[] dimTags)
        {
            if (dimTags.Length > 0) Gmsh.Model.Mesh.Clear(dimTags);
        }

        private static void AddRevolvedNodes(Dictionary<IntPtr, double[]> sourceNodeCoordinates,
                                             Dictionary<IntPtr, EntityKey> sourceNodeEntities,
                                             double[] layerFractions,
                                             Dictionary<int, int> targetVertexIds,
                                             Dictionary<int, int> targetEdgeIds,
                                             Dictionary<int, int> targetSurfaceIds,
                                             Dictionary<int, int> targetSurfaceBySourceVertexIds,
                                             Dictionary<int, int> targetSurfaceBySourceEdgeIds,
                                             HashSet<int> boundaryEdgeIds,
                                             Dictionary<int, int> sideSurfaceIds,
                                             Dictionary<int, int> connectingEdgeIds,
                                             int volumeId, double[] axisCenter, double[] axisDirection,
                                             double angleRad, double axisTolerance, ref long maxNodeId,
                                             out Dictionary<IntPtr, IntPtr[]> nodeIdLayerIds)
        {
            Dictionary<EntityKey, NodeBatch> batches = new Dictionary<EntityKey, NodeBatch>();
            nodeIdLayerIds = new Dictionary<IntPtr, IntPtr[]>();
            foreach (IntPtr sourceNodeId in sourceNodeCoordinates.Keys.OrderBy(x => x.ToInt64()))
            {
                EntityKey sourceEntity = sourceNodeEntities[sourceNodeId];
                IntPtr[] layerNodeIds = new IntPtr[layerFractions.Length];
                layerNodeIds[0] = sourceNodeId;
                if (DistanceFromAxis(sourceNodeCoordinates[sourceNodeId], axisCenter, axisDirection) <= axisTolerance)
                {
                    for (int layerId = 1; layerId < layerFractions.Length; layerId++)
                        layerNodeIds[layerId] = sourceNodeId;
                    nodeIdLayerIds.Add(sourceNodeId, layerNodeIds);
                    continue;
                }
                for (int layerId = 1; layerId < layerFractions.Length; layerId++)
                {
                    IntPtr nodeId = new IntPtr(++maxNodeId);
                    layerNodeIds[layerId] = nodeId;
                    double[] coordinate = Rotate(sourceNodeCoordinates[sourceNodeId], axisCenter, axisDirection,
                                                 angleRad * layerFractions[layerId]);
                    EntityKey targetEntity;
                    if (layerId == layerFractions.Length - 1)
                        targetEntity = GetTargetEntity(sourceEntity, targetVertexIds, targetEdgeIds,
                                                       targetSurfaceIds, targetSurfaceBySourceVertexIds,
                                                       targetSurfaceBySourceEdgeIds);
                    else
                        targetEntity = GetIntermediateEntity(sourceEntity, boundaryEdgeIds, sideSurfaceIds,
                                                             connectingEdgeIds, volumeId);
                    AddNode(batches, targetEntity, nodeId, coordinate);
                }
                nodeIdLayerIds.Add(sourceNodeId, layerNodeIds);
            }
            foreach (KeyValuePair<EntityKey, NodeBatch> entry in batches)
            {
                Gmsh.Model.Mesh.AddNodes(entry.Key.Dim, entry.Key.Tag, entry.Value.NodeIds.ToArray(),
                                         entry.Value.Coordinates.ToArray());
            }
        }

        private static EntityKey GetTargetEntity(EntityKey sourceEntity,
                                                  Dictionary<int, int> targetVertexIds,
                                                  Dictionary<int, int> targetEdgeIds,
                                                  Dictionary<int, int> targetSurfaceIds,
                                                  Dictionary<int, int> targetSurfaceBySourceVertexIds,
                                                  Dictionary<int, int> targetSurfaceBySourceEdgeIds)
        {
            int targetId;
            if (sourceEntity.Dim == 0)
            {
                if (targetVertexIds.TryGetValue(sourceEntity.Tag, out targetId)) return new EntityKey(0, targetId);
                return new EntityKey(2, targetSurfaceBySourceVertexIds[sourceEntity.Tag]);
            }
            if (sourceEntity.Dim == 1)
            {
                if (targetEdgeIds.TryGetValue(sourceEntity.Tag, out targetId)) return new EntityKey(1, targetId);
                return new EntityKey(2, targetSurfaceBySourceEdgeIds[sourceEntity.Tag]);
            }
            if (sourceEntity.Dim == 2) return new EntityKey(2, targetSurfaceIds[sourceEntity.Tag]);
            throw new NotSupportedException();
        }
        private static EntityKey GetIntermediateEntity(EntityKey sourceEntity, HashSet<int> boundaryEdgeIds,
                                                        Dictionary<int, int> sideSurfaceIds,
                                                        Dictionary<int, int> connectingEdgeIds, int volumeId)
        {
            int entityId;
            if (sourceEntity.Dim == 0 && connectingEdgeIds.TryGetValue(sourceEntity.Tag, out entityId))
                return new EntityKey(1, entityId);
            if (sourceEntity.Dim == 1 && boundaryEdgeIds.Contains(sourceEntity.Tag) &&
                sideSurfaceIds.TryGetValue(sourceEntity.Tag, out entityId))
                return new EntityKey(2, entityId);
            return new EntityKey(3, volumeId);
        }

        private static void AddNode(Dictionary<EntityKey, NodeBatch> batches, EntityKey entity,
                                    IntPtr nodeId, double[] coordinate)
        {
            NodeBatch batch;
            if (!batches.TryGetValue(entity, out batch))
            {
                batch = new NodeBatch();
                batches.Add(entity, batch);
            }
            batch.NodeIds.Add(nodeId);
            batch.Coordinates.AddRange(coordinate);
        }

        private static void AddRevolvedElements(List<ElementRecord> sourceSurfaceElements,
                                                List<ElementRecord> sourceEdgeElements,
                                                Dictionary<IntPtr, double[]> sourceNodeCoordinates,
                                                Dictionary<IntPtr, IntPtr[]> nodeIdLayerIds,
                                                double[] layerFractions,
                                                Dictionary<int, int> targetEdgeIds,
                                                Dictionary<int, int> targetSurfaceIds,
                                                HashSet<int> boundaryEdgeIds,
                                                Dictionary<int, int> sideSurfaceIds,
                                                Dictionary<int, int> connectingEdgeIds,
                                                int volumeId, double[] axisCenter,
                                                double[] axisDirection, double angleRad,
                                                double axisTolerance, ref long maxElementId)
        {
            Dictionary<Tuple<EntityKey, int>, ElementBatch> batches =
                new Dictionary<Tuple<EntityKey, int>, ElementBatch>();
            int numLayers = layerFractions.Length - 1;

            foreach (ElementRecord sourceElement in sourceSurfaceElements)
            {
                IntPtr[] sourceNodeIds = GetOutwardSourceNodeIds(sourceElement.NodeIds,
                                                                 sourceNodeCoordinates,
                                                                 axisCenter, axisDirection,
                                                                 angleRad * layerFractions[1]);
                int numAxisNodes;
                sourceNodeIds = RotateSourceNodeIdsToAxis(sourceNodeIds, sourceNodeCoordinates,
                                                          axisCenter, axisDirection, axisTolerance,
                                                          out numAxisNodes);
                int numNodes = sourceNodeIds.Length;
                for (int layerId = 0; layerId < numLayers; layerId++)
                {
                    IntPtr[] volumeNodeIds;
                    int volumeElementType;
                    if (numNodes == 3)
                    {
                        if (numAxisNodes == 0)
                        {
                            volumeElementType = (int)GmshElementTypeEnum.LinearWedge;
                            volumeNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceNodeIds[0]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[1]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[0]][layerId],
                                nodeIdLayerIds[sourceNodeIds[1]][layerId],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId]
                            };
                        }
                        else if (numAxisNodes == 1)
                        {
                            volumeElementType = (int)GmshElementTypeEnum.LinearPyramid;
                            volumeNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceNodeIds[1]][layerId],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[1]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[0]][layerId]
                            };
                        }
                        else if (numAxisNodes == 2)
                        {
                            volumeElementType = (int)GmshElementTypeEnum.LinearTetra;
                            volumeNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceNodeIds[0]][layerId],
                                nodeIdLayerIds[sourceNodeIds[1]][layerId],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId]
                            };
                        }
                        else throw new NotSupportedException();
                    }
                    else if (numNodes == 4)
                    {
                        if (numAxisNodes == 0)
                        {
                            volumeElementType = (int)GmshElementTypeEnum.LinearHexa;
                            volumeNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceNodeIds[0]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[1]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[3]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[0]][layerId],
                                nodeIdLayerIds[sourceNodeIds[1]][layerId],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId],
                                nodeIdLayerIds[sourceNodeIds[3]][layerId]
                            };
                        }
                        else if (numAxisNodes == 1)
                        {
                            volumeElementType = (int)GmshElementTypeEnum.LinearPyramid;
                            volumeNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceNodeIds[1]][layerId],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[1]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[0]][layerId]
                            };
                            AddElement(batches, new EntityKey(3, volumeId), volumeElementType,
                                       volumeNodeIds, ref maxElementId);
                            volumeNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceNodeIds[2]][layerId],
                                nodeIdLayerIds[sourceNodeIds[3]][layerId],
                                nodeIdLayerIds[sourceNodeIds[3]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[0]][layerId]
                            };
                        }
                        else if (numAxisNodes == 2)
                        {
                            volumeElementType = (int)GmshElementTypeEnum.LinearWedge;
                            volumeNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceNodeIds[3]][layerId],
                                nodeIdLayerIds[sourceNodeIds[0]][layerId],
                                nodeIdLayerIds[sourceNodeIds[3]][layerId + 1],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId],
                                nodeIdLayerIds[sourceNodeIds[1]][layerId],
                                nodeIdLayerIds[sourceNodeIds[2]][layerId + 1]
                            };
                        }
                        else throw new NotSupportedException();
                    }
                    else throw new NotSupportedException();
                    AddElement(batches, new EntityKey(3, volumeId), volumeElementType,
                               volumeNodeIds, ref maxElementId);
                }

                IntPtr[] targetNodeIds;
                if (numNodes == 3)
                {
                    targetNodeIds = new IntPtr[]
                    {
                        nodeIdLayerIds[sourceNodeIds[0]][numLayers],
                        nodeIdLayerIds[sourceNodeIds[2]][numLayers],
                        nodeIdLayerIds[sourceNodeIds[1]][numLayers]
                    };
                }
                else
                {
                    targetNodeIds = new IntPtr[]
                    {
                        nodeIdLayerIds[sourceNodeIds[0]][numLayers],
                        nodeIdLayerIds[sourceNodeIds[3]][numLayers],
                        nodeIdLayerIds[sourceNodeIds[2]][numLayers],
                        nodeIdLayerIds[sourceNodeIds[1]][numLayers]
                    };
                }
                AddElement(batches, new EntityKey(2, targetSurfaceIds[sourceElement.EntityId]),
                           sourceElement.ElementType, targetNodeIds, ref maxElementId);
            }

            foreach (ElementRecord sourceElement in sourceEdgeElements)
            {
                int sourceEdgeId = sourceElement.EntityId;
                int targetEdgeId;
                if (targetEdgeIds.TryGetValue(sourceEdgeId, out targetEdgeId) &&
                    targetEdgeId != sourceEdgeId)
                {
                    IntPtr[] targetNodeIds = new IntPtr[]
                    {
                        nodeIdLayerIds[sourceElement.NodeIds[1]][numLayers],
                        nodeIdLayerIds[sourceElement.NodeIds[0]][numLayers]
                    };
                    AddElement(batches, new EntityKey(1, targetEdgeId),
                               (int)GmshElementTypeEnum.LinearBeam, targetNodeIds, ref maxElementId);
                }

                int sideSurfaceId;
                if (boundaryEdgeIds.Contains(sourceEdgeId) &&
                    sideSurfaceIds.TryGetValue(sourceEdgeId, out sideSurfaceId))
                {
                    bool node0OnAxis = DistanceFromAxis(sourceNodeCoordinates[sourceElement.NodeIds[0]],
                                                        axisCenter, axisDirection) <= axisTolerance;
                    bool node1OnAxis = DistanceFromAxis(sourceNodeCoordinates[sourceElement.NodeIds[1]],
                                                        axisCenter, axisDirection) <= axisTolerance;
                    // A side-surface edge can still contain a single element with both ends on the axis: the
                    // edge only touches the axis, but a border element the mesher places at the axis corner
                    // sits on it. Revolving that element sweeps zero area, so skip the degenerate side element.
                    if (node0OnAxis && node1OnAxis) continue;
                    for (int layerId = 0; layerId < numLayers; layerId++)
                    {
                        IntPtr[] sideNodeIds;
                        int sideElementType;
                        if (node0OnAxis)
                        {
                            sideElementType = (int)GmshElementTypeEnum.LinearTriangle;
                            sideNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceElement.NodeIds[0]][layerId],
                                nodeIdLayerIds[sourceElement.NodeIds[1]][layerId],
                                nodeIdLayerIds[sourceElement.NodeIds[1]][layerId + 1]
                            };
                        }
                        else if (node1OnAxis)
                        {
                            sideElementType = (int)GmshElementTypeEnum.LinearTriangle;
                            sideNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceElement.NodeIds[0]][layerId],
                                nodeIdLayerIds[sourceElement.NodeIds[1]][layerId],
                                nodeIdLayerIds[sourceElement.NodeIds[0]][layerId + 1]
                            };
                        }
                        else
                        {
                            sideElementType = (int)GmshElementTypeEnum.LinearQuadrilateral;
                            sideNodeIds = new IntPtr[]
                            {
                                nodeIdLayerIds[sourceElement.NodeIds[0]][layerId],
                                nodeIdLayerIds[sourceElement.NodeIds[1]][layerId],
                                nodeIdLayerIds[sourceElement.NodeIds[1]][layerId + 1],
                                nodeIdLayerIds[sourceElement.NodeIds[0]][layerId + 1]
                            };
                        }
                        AddElement(batches, new EntityKey(2, sideSurfaceId), sideElementType,
                                   sideNodeIds, ref maxElementId);
                    }
                }
            }

            foreach (KeyValuePair<int, int> entry in connectingEdgeIds)
            {
                IntPtr sourceNodeId = GetVertexNodeId(entry.Key, nodeIdLayerIds.Keys);
                for (int layerId = 0; layerId < numLayers; layerId++)
                {
                    IntPtr[] edgeNodeIds = new IntPtr[]
                    {
                        nodeIdLayerIds[sourceNodeId][layerId],
                        nodeIdLayerIds[sourceNodeId][layerId + 1]
                    };
                    AddElement(batches, new EntityKey(1, entry.Value),
                               (int)GmshElementTypeEnum.LinearBeam, edgeNodeIds, ref maxElementId);
                }
            }

            foreach (KeyValuePair<Tuple<EntityKey, int>, ElementBatch> entry in batches)
            {
                EntityKey entity = entry.Key.Item1;
                int elementType = entry.Key.Item2;
                ElementBatch batch = entry.Value;
                Gmsh.Model.Mesh.AddElements(entity.Dim, entity.Tag, new int[] { elementType },
                    new IntPtr[][] { batch.ElementIds.ToArray() },
                    new IntPtr[][] { batch.NodeIds.ToArray() });
            }
        }

        internal static IntPtr[] GetOutwardSourceNodeIds(IntPtr[] sourceNodeIds,
                                                        Dictionary<IntPtr, double[]> sourceNodeCoordinates,
                                                        double[] axisCenter, double[] axisDirection,
                                                        double firstLayerAngleRad)
        {
            IntPtr[] orientedNodeIds = sourceNodeIds.ToArray();
            double[] center = new double[3];
            double[] normal = new double[3];
            double[] point0 = sourceNodeCoordinates[sourceNodeIds[0]];
            for (int i = 0; i < sourceNodeIds.Length; i++)
            {
                double[] point = sourceNodeCoordinates[sourceNodeIds[i]];
                center[0] += point[0];
                center[1] += point[1];
                center[2] += point[2];
            }
            center[0] /= sourceNodeIds.Length;
            center[1] /= sourceNodeIds.Length;
            center[2] /= sourceNodeIds.Length;

            for (int i = 1; i < sourceNodeIds.Length - 1; i++)
            {
                double[] point1 = sourceNodeCoordinates[sourceNodeIds[i]];
                double[] point2 = sourceNodeCoordinates[sourceNodeIds[i + 1]];
                double x1 = point1[0] - point0[0];
                double y1 = point1[1] - point0[1];
                double z1 = point1[2] - point0[2];
                double x2 = point2[0] - point0[0];
                double y2 = point2[1] - point0[1];
                double z2 = point2[2] - point0[2];
                normal[0] += y1 * z2 - z1 * y2;
                normal[1] += z1 * x2 - x1 * z2;
                normal[2] += x1 * y2 - y1 * x2;
            }

            double[] nextCenter = Rotate(center, axisCenter, axisDirection, firstLayerAngleRad);
            double travelX = nextCenter[0] - center[0];
            double travelY = nextCenter[1] - center[1];
            double travelZ = nextCenter[2] - center[2];
            double normalLength = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] +
                                            normal[2] * normal[2]);
            double travelLength = Math.Sqrt(travelX * travelX + travelY * travelY + travelZ * travelZ);
            if (normalLength <= 1E-30 || travelLength <= 1E-30)
                throw new CaeException("The orientation of a revolved source element could not be determined.");

            // The generated volume lies in the direction of travel, so the source-face normal must point
            // in the opposite direction. Gmsh does not guarantee that a standalone surface mesh has that order.
            double normalTravel = normal[0] * travelX + normal[1] * travelY + normal[2] * travelZ;
            if (Math.Abs(normalTravel) <= 1E-12 * normalLength * travelLength)
                throw new CaeException("The orientation of a revolved source element could not be determined.");
            if (normalTravel > 0)
            {
                if (orientedNodeIds.Length == 3)
                {
                    IntPtr tmp = orientedNodeIds[1];
                    orientedNodeIds[1] = orientedNodeIds[2];
                    orientedNodeIds[2] = tmp;
                }
                else if (orientedNodeIds.Length == 4)
                {
                    IntPtr tmp = orientedNodeIds[1];
                    orientedNodeIds[1] = orientedNodeIds[3];
                    orientedNodeIds[3] = tmp;
                }
                else throw new NotSupportedException();
            }
            return orientedNodeIds;
        }

        internal static IntPtr[] RotateSourceNodeIdsToAxis(IntPtr[] sourceNodeIds,
                                                          Dictionary<IntPtr, double[]> sourceNodeCoordinates,
                                                          double[] axisCenter, double[] axisDirection,
                                                          double axisTolerance, out int numAxisNodes)
        {
            bool[] nodeOnAxis = new bool[sourceNodeIds.Length];
            numAxisNodes = 0;
            for (int i = 0; i < sourceNodeIds.Length; i++)
            {
                nodeOnAxis[i] = DistanceFromAxis(sourceNodeCoordinates[sourceNodeIds[i]],
                                                 axisCenter, axisDirection) <= axisTolerance;
                if (nodeOnAxis[i]) numAxisNodes++;
            }
            if (numAxisNodes == 0) return sourceNodeIds.ToArray();
            if (numAxisNodes > 2)
                throw new CaeException("A revolved source element has more than one edge on the revolution axis.");

            int firstAxisNode = -1;
            if (numAxisNodes == 1)
            {
                firstAxisNode = Array.IndexOf(nodeOnAxis, true);
            }
            else
            {
                for (int i = 0; i < sourceNodeIds.Length; i++)
                {
                    if (nodeOnAxis[i] && nodeOnAxis[(i + 1) % sourceNodeIds.Length])
                    {
                        firstAxisNode = i;
                        break;
                    }
                }
                if (firstAxisNode == -1)
                    throw new CaeException("The axis nodes of a revolved source element do not form an edge.");
            }

            IntPtr[] rotatedNodeIds = new IntPtr[sourceNodeIds.Length];
            for (int i = 0; i < sourceNodeIds.Length; i++)
                rotatedNodeIds[i] = sourceNodeIds[(firstAxisNode + i) % sourceNodeIds.Length];
            return rotatedNodeIds;
        }

        private static IntPtr GetVertexNodeId(int vertexId, IEnumerable<IntPtr> sourceNodeIds)
        {
            IntPtr[] nodeIds;
            double[] coordinates;
            Gmsh.Model.Mesh.GetNodes(out nodeIds, out coordinates, 0, vertexId, false, false);
            HashSet<IntPtr> validNodeIds = new HashSet<IntPtr>(sourceNodeIds);
            foreach (IntPtr nodeId in nodeIds)
            {
                if (validNodeIds.Contains(nodeId)) return nodeId;
            }
            throw new CaeException("A source vertex node of the revolved mesh could not be identified.");
        }

        private static void AddElement(Dictionary<Tuple<EntityKey, int>, ElementBatch> batches,
                                       EntityKey entity, int elementType, IntPtr[] nodeIds,
                                       ref long maxElementId)
        {
            Tuple<EntityKey, int> key = new Tuple<EntityKey, int>(entity, elementType);
            ElementBatch batch;
            if (!batches.TryGetValue(key, out batch))
            {
                batch = new ElementBatch(nodeIds.Length);
                batches.Add(key, batch);
            }
            if (batch.NumNodes != nodeIds.Length) throw new NotSupportedException();
            batch.ElementIds.Add(new IntPtr(++maxElementId));
            batch.NodeIds.AddRange(nodeIds);
        }

        private static int GetNumNodes(int elementType)
        {
            if (elementType == (int)GmshElementTypeEnum.LinearBeam) return 2;
            if (elementType == (int)GmshElementTypeEnum.LinearTriangle) return 3;
            if (elementType == (int)GmshElementTypeEnum.LinearQuadrilateral) return 4;
            throw new CaeException("An unsupported source element type was created by Gmsh.");
        }

        private static long GetMaxNodeId()
        {
            IntPtr[] nodeIds;
            double[] coordinates;
            Gmsh.Model.Mesh.GetNodes(out nodeIds, out coordinates, -1, -1, false, false);
            return nodeIds.Length == 0 ? 0 : nodeIds.Max(x => x.ToInt64());
        }

        private static long GetMaxElementId()
        {
            IntPtr maxElementId;
            Gmsh.Model.Mesh.GetMaxElementTag(out maxElementId);
            return maxElementId.ToInt64();
        }

        private static bool SameIds(int[] ids1, int[] ids2)
        {
            if (ids1.Length != ids2.Length) return false;
            for (int i = 0; i < ids1.Length; i++)
            {
                if (ids1[i] != ids2[i]) return false;
            }
            return true;
        }

        private static double[] Normalize(double[] vector)
        {
            double length = Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
            if (length <= 0) throw new CaeException("The revolve axis direction has zero length.");
            return new double[] { vector[0] / length, vector[1] / length, vector[2] / length };
        }

        internal static double[] Rotate(double[] point, double[] axisCenter, double[] axisDirection, double angle)
        {
            double x = point[0] - axisCenter[0];
            double y = point[1] - axisCenter[1];
            double z = point[2] - axisCenter[2];
            double dot = axisDirection[0] * x + axisDirection[1] * y + axisDirection[2] * z;
            double crossX = axisDirection[1] * z - axisDirection[2] * y;
            double crossY = axisDirection[2] * x - axisDirection[0] * z;
            double crossZ = axisDirection[0] * y - axisDirection[1] * x;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double oneMinusCos = 1 - cos;
            return new double[]
            {
                axisCenter[0] + x * cos + crossX * sin + axisDirection[0] * dot * oneMinusCos,
                axisCenter[1] + y * cos + crossY * sin + axisDirection[1] * dot * oneMinusCos,
                axisCenter[2] + z * cos + crossZ * sin + axisDirection[2] * dot * oneMinusCos
            };
        }

        private static double[] GetArcCenter(double[] point, double[] axisCenter,
                                             double[] axisDirection, double angle)
        {
            if (Math.Abs(angle) < 1E-14) return point.ToArray();
            double x = point[0] - axisCenter[0];
            double y = point[1] - axisCenter[1];
            double z = point[2] - axisCenter[2];
            double dot = axisDirection[0] * x + axisDirection[1] * y + axisDirection[2] * z;
            double radialX = x - dot * axisDirection[0];
            double radialY = y - dot * axisDirection[1];
            double radialZ = z - dot * axisDirection[2];
            double crossX = axisDirection[1] * radialZ - axisDirection[2] * radialY;
            double crossY = axisDirection[2] * radialX - axisDirection[0] * radialZ;
            double crossZ = axisDirection[0] * radialY - axisDirection[1] * radialX;
            double radialFactor = Math.Sin(angle) / angle;
            double crossFactor = (1 - Math.Cos(angle)) / angle;
            return new double[]
            {
                axisCenter[0] + dot * axisDirection[0] + radialFactor * radialX + crossFactor * crossX,
                axisCenter[1] + dot * axisDirection[1] + radialFactor * radialY + crossFactor * crossY,
                axisCenter[2] + dot * axisDirection[2] + radialFactor * radialZ + crossFactor * crossZ
            };
        }

        internal static double DistanceFromAxis(double[] point, double[] axisCenter, double[] axisDirection)
        {
            double x = point[0] - axisCenter[0];
            double y = point[1] - axisCenter[1];
            double z = point[2] - axisCenter[2];
            double dot = axisDirection[0] * x + axisDirection[1] * y + axisDirection[2] * z;
            x -= dot * axisDirection[0];
            y -= dot * axisDirection[1];
            z -= dot * axisDirection[2];
            return Math.Sqrt(x * x + y * y + z * z);
        }

        private static double Distance2(double[] point1, double[] point2)
        {
            return Math.Pow(point1[0] - point2[0], 2) +
                   Math.Pow(point1[1] - point2[1], 2) +
                   Math.Pow(point1[2] - point2[2], 2);
        }
    }
}
