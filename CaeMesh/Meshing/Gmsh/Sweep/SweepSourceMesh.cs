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
using CaeGlobals;
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// The mesh one geometric entity owns, read from one gmsh model so that it can be added to another: the
    /// nodes classified on the entity itself - not the nodes of its boundary, which belong to the entities of
    /// lower dimension - and the elements classified on it.
    ///
    /// The parametric coordinates are carried along with the positions, so that a second order mesh can place
    /// its midside nodes on the geometry once the mesh has been added.
    /// </summary>
    public class SweepEntityMesh
    {
        // Variables                                                                                                                
        private readonly int _dimension;
        private readonly int _tag;
        private readonly IntPtr[] _nodeTags;
        private readonly double[] _coordinates;
        private readonly double[] _parametricCoordinates;
        private readonly int[] _elementTypes;
        private readonly IntPtr[][] _elementTags;
        private readonly IntPtr[][] _elementNodeTags;


        // Properties                                                                                                               
        public int Dimension { get { return _dimension; } }
        public int Tag { get { return _tag; } }
        /// <summary>
        /// The number of elements the entity carries, which for a curve is the number of its mesh segments.
        /// </summary>
        public int NumOfElements
        {
            get
            {
                int numOfElements = 0;
                for (int i = 0; i < _elementTypes.Length; i++) numOfElements += _elementTags[i].Length;
                return numOfElements;
            }
        }


        // Constructors                                                                                                             
        public SweepEntityMesh(int dimension, int tag, IntPtr[] nodeTags, double[] coordinates,
                               double[] parametricCoordinates, int[] elementTypes, IntPtr[][] elementTags,
                               IntPtr[][] elementNodeTags)
        {
            _dimension = dimension;
            _tag = tag;
            _nodeTags = nodeTags;
            _coordinates = coordinates;
            _parametricCoordinates = parametricCoordinates;
            _elementTypes = elementTypes;
            _elementTags = elementTags;
            _elementNodeTags = elementNodeTags;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Reads what the entity owns in the current model and records it under the tag the same entity
        /// carries in the model the mesh is meant for.
        /// </summary>
        public static SweepEntityMesh Read(int dimension, int tag, int tagInTargetModel)
        {
            IntPtr[] nodeTags;
            double[] coordinates;
            double[] parametricCoordinates;
            Gmsh.Model.Mesh.GetNodes(out nodeTags, out coordinates, out parametricCoordinates, dimension, tag,
                                     false, true);
            //
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] elementNodeTags;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out elementNodeTags, dimension, tag);
            //
            // gmsh keeps the nodes of a curve in the order they were added and its transfinite surface mesher
            // pairs the two sides of a wall in that order, so a curve whose nodes arrive out of order would be
            // built into a grid whose columns cross. The quasi-structured quad algorithm moves and renumbers
            // the nodes it meshed, so the order is restored from the curve parameter before the mesh is carried
            if (dimension == 1 && parametricCoordinates.Length == nodeTags.Length)
                SortCurveNodes(nodeTags, coordinates, parametricCoordinates);
            //
            return new SweepEntityMesh(dimension, tagInTargetModel, nodeTags, coordinates, parametricCoordinates,
                                       elementTypes, elementTags, elementNodeTags);
        }
        private static void SortCurveNodes(IntPtr[] nodeTags, double[] coordinates, double[] parametricCoordinates)
        {
            int[] order = new int[nodeTags.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort((double[])parametricCoordinates.Clone(), order);
            //
            IntPtr[] sortedNodeTags = new IntPtr[nodeTags.Length];
            double[] sortedCoordinates = new double[coordinates.Length];
            double[] sortedParametricCoordinates = new double[parametricCoordinates.Length];
            for (int i = 0; i < order.Length; i++)
            {
                sortedNodeTags[i] = nodeTags[order[i]];
                sortedParametricCoordinates[i] = parametricCoordinates[order[i]];
                for (int k = 0; k < 3; k++) sortedCoordinates[3 * i + k] = coordinates[3 * order[i] + k];
            }
            Array.Copy(sortedNodeTags, nodeTags, nodeTags.Length);
            Array.Copy(sortedCoordinates, coordinates, coordinates.Length);
            Array.Copy(sortedParametricCoordinates, parametricCoordinates, parametricCoordinates.Length);
        }
        /// <summary>
        /// Records which node of the current model stands where each of the entity's own nodes stands, so that
        /// an entity added later reaches its boundary through the tags in use rather than through tags that a
        /// meshing pass has since replaced.
        /// </summary>
        public void MapNodesByPosition(IntPtr[] nodeTags, double[] coordinates, Dictionary<IntPtr, IntPtr> nodeTagMap)
        {
            if (nodeTags.Length != _nodeTags.Length)
                throw new CaeException("The mesh of the sweep source boundary was replaced by one of a different " +
                                       "size and its surfaces can no longer be added to it.");
            //
            // One node for one node: two nodes of a quasi-structured mesh can lie close enough together that
            // a nearest match on its own would give both of them the same partner and cross two sweep lines
            int nearest;
            double distance;
            double nearestDistance;
            bool[] taken = new bool[nodeTags.Length];
            for (int i = 0; i < _nodeTags.Length; i++)
            {
                nearest = -1;
                nearestDistance = double.MaxValue;
                for (int j = 0; j < nodeTags.Length; j++)
                {
                    if (taken[j]) continue;
                    distance = Math.Pow(coordinates[3 * j] - _coordinates[3 * i], 2) +
                               Math.Pow(coordinates[3 * j + 1] - _coordinates[3 * i + 1], 2) +
                               Math.Pow(coordinates[3 * j + 2] - _coordinates[3 * i + 2], 2);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = j;
                    }
                }
                taken[nearest] = true;
                nodeTagMap[_nodeTags[i]] = nodeTags[nearest];
            }
        }
        /// <summary>
        /// Adds the mesh to the current model under fresh tags taken from the counters, and records the node
        /// tags, so that an entity added later finds the nodes of its boundary.
        /// </summary>
        public void Add(Dictionary<IntPtr, IntPtr> nodeTagMap, ref long nodeTag, ref long elementTag)
        {
            // gmsh meshes a vertex itself whether it is hidden or not, and does not give that node up, so the
            // node the vertex already carries is taken over rather than a second one added beside it
            if (_dimension == 0)
            {
                IntPtr[] vertexNodeTags;
                double[] vertexCoordinates;
                Gmsh.Model.Mesh.GetNodes(out vertexNodeTags, out vertexCoordinates, 0, _tag, false, false);
                if (vertexNodeTags.Length == _nodeTags.Length)
                {
                    for (int i = 0; i < _nodeTags.Length; i++) nodeTagMap[_nodeTags[i]] = vertexNodeTags[i];
                    return;
                }
            }
            //
            IntPtr[] nodeTags = new IntPtr[_nodeTags.Length];
            for (int i = 0; i < _nodeTags.Length; i++)
            {
                nodeTags[i] = new IntPtr(++nodeTag);
                nodeTagMap[_nodeTags[i]] = nodeTags[i];
            }
            Gmsh.Model.Mesh.AddNodes(_dimension, _tag, nodeTags, _coordinates, _parametricCoordinates);
            //
            if (_elementTypes.Length == 0) return;
            //
            IntPtr[][] elementTags = new IntPtr[_elementTypes.Length][];
            IntPtr[][] elementNodeTags = new IntPtr[_elementTypes.Length][];
            for (int i = 0; i < _elementTypes.Length; i++)
            {
                elementTags[i] = new IntPtr[_elementTags[i].Length];
                for (int j = 0; j < _elementTags[i].Length; j++) elementTags[i][j] = new IntPtr(++elementTag);
                //
                elementNodeTags[i] = new IntPtr[_elementNodeTags[i].Length];
                for (int j = 0; j < _elementNodeTags[i].Length; j++)
                    elementNodeTags[i][j] = nodeTagMap[_elementNodeTags[i][j]];
            }
            Gmsh.Model.Mesh.AddElements(_dimension, _tag, _elementTypes, elementTags, elementNodeTags);
        }
    }
    /// <summary>
    /// The mesh of the source surfaces of a sweep, built with the quasi-structured quad algorithm in a gmsh
    /// model that holds nothing else.
    ///
    /// The algorithm cannot be let loose on the model the sweep is built in. gmsh runs its automatic
    /// transfinite pass over every four sided face before it meshes the curves, and that pass replaces the
    /// element counts pinned on the curves with one count per chord, rounded up to an odd number. The walls of
    /// a sweep are four sided faces, so the counts the sweep pins on them do not survive; and a wall whose one
    /// side is split into several edges while the side opposite it is a single edge can never have its two
    /// sides agree afterwards, since two odd counts never add up to an odd one. gmsh then refuses to mesh that
    /// wall with the structured grid the sweep needs.
    ///
    /// So the mesh is built apart and carried back in two steps. The boundary goes in before the walls are
    /// meshed, so that their sides are reconciled against the counts the algorithm actually produced, and the
    /// surfaces themselves only afterwards, because a Generate empties a face whose mesh was added rather than
    /// meshed, whatever Mesh.MeshOnlyEmpty says. The source entities stay hidden while the walls are meshed,
    /// so that Mesh.MeshOnlyVisible keeps the curve meshes a Generate would otherwise replace.
    /// </summary>
    public class SweepSourceMesh
    {
        // Variables                                                                                                                
        private readonly SweepEntityMesh[] _boundaryMeshes;
        private readonly SweepEntityMesh[] _surfaceMeshes;
        private readonly Dictionary<int, int> _edgeIdNumOfElements;
        private readonly Dictionary<IntPtr, IntPtr> _nodeTagMap;


        // Properties                                                                                                               
        /// <summary>
        /// The number of elements the algorithm left on every edge of the source surfaces.
        /// </summary>
        public Dictionary<int, int> EdgeIdNumOfElements { get { return _edgeIdNumOfElements; } }


        // Constructors                                                                                                             
        public SweepSourceMesh(SweepEntityMesh[] boundaryMeshes, SweepEntityMesh[] surfaceMeshes,
                               Dictionary<int, int> edgeIdNumOfElements)
        {
            _boundaryMeshes = boundaryMeshes;
            _surfaceMeshes = surfaceMeshes;
            _edgeIdNumOfElements = edgeIdNumOfElements;
            _nodeTagMap = new Dictionary<IntPtr, IntPtr>();
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Adds the vertices and the curves of the source surfaces to the current model, dropping whatever an
        /// earlier call recorded: gmsh empties every curve at the start of a meshing pass, visible or not, so
        /// the boundary is added again for each pass and takes new tags every time.
        /// </summary>
        public void AddBoundary()
        {
            _nodeTagMap.Clear();
            Add(_boundaryMeshes);
        }
        /// <summary>
        /// Adds the source surfaces themselves, above whatever the meshing of the walls has left behind.
        ///
        /// Their elements are stitched onto the nodes the boundary carries at that moment, found by position
        /// rather than by a tag recorded earlier: the boundary is added again for every meshing pass and takes
        /// new tags each time, and the walls are built on whichever set was there last.
        /// </summary>
        public void AddSurfaces()
        {
            IntPtr[] nodeTags;
            double[] coordinates;
            _nodeTagMap.Clear();
            foreach (var entityMesh in _boundaryMeshes)
            {
                Gmsh.Model.Mesh.GetNodes(out nodeTags, out coordinates, entityMesh.Dimension, entityMesh.Tag,
                                         false, false);
                entityMesh.MapNodesByPosition(nodeTags, coordinates, _nodeTagMap);
            }
            Add(_surfaceMeshes);
        }
        private void Add(SweepEntityMesh[] entityMeshes)
        {
            // What a pass left on these entities is replaced rather than added to, since a meshing pass keeps
            // the mesh of an entity it is told not to mesh. The vertices are left alone, their nodes being
            // taken over instead
            List<Tuple<int, int>> dimTags = new List<Tuple<int, int>>();
            foreach (var entityMesh in entityMeshes)
            {
                if (entityMesh.Dimension > 0) dimTags.Add(new Tuple<int, int>(entityMesh.Dimension, entityMesh.Tag));
            }
            Gmsh.Model.Mesh.Clear(dimTags.ToArray());
            //
            IntPtr maxNodeTag;
            IntPtr maxElementTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxNodeTag);
            Gmsh.Model.Mesh.GetMaxElementTag(out maxElementTag);
            long nodeTag = maxNodeTag.ToInt64();
            long elementTag = maxElementTag.ToInt64();
            foreach (var entityMesh in entityMeshes) entityMesh.Add(_nodeTagMap, ref nodeTag, ref elementTag);
        }
    }
}
