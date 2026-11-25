// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// One opposite-side pair, one submapped face or the source and target sides of one sweep wall in the spacing
    /// graph. A wall follows the faces it joins and starts no chain.
    /// </summary>
    internal class GmshShellSpacingPatch
    {
        // Variables                                                                                                    
        private readonly TransfiniteFace _face;
        private readonly SubmapFace _submap;
        private readonly int _side;
        private readonly int _wallId;
        private readonly int[][] _wallSideEdgeIds;
        private readonly int[][] _wallSideVertexIds;
        private readonly int[] _edgeIds;
        private GmshShellSpacingSide _first;
        private GmshShellSpacingSide _second;
        private double _length;
        private List<(GmshShellSpacingNode First, GmshShellSpacingNode Second)> _pairs;


        // Properties                                                                                                   
        public int FaceId { get { return _face != null ? _face.FaceId : _submap != null ? _submap.FaceId : _wallId; } }
        public TransfiniteFace Transfinite { get { return _face; } }
        public SubmapFace Submap { get { return _submap; } }
        public int[] EdgeIds { get { return _edgeIds; } }
        public bool IsSeed { get { return _submap != null || (_face != null && _edgeIds.Length > 2); } }
        public List<(GmshShellSpacingNode First, GmshShellSpacingNode Second)> Pairs { get { return _pairs; } }


        // Constructors                                                                                                 
        public GmshShellSpacingPatch(TransfiniteFace face, int side)
        {
            _face = face;
            _side = side;
            _edgeIds = face.SideEdgeIds[side].Concat(face.SideEdgeIds[side + 2]).ToArray();
        }
        public GmshShellSpacingPatch(SubmapFace face)
        {
            _submap = face;
            _edgeIds = face.Loops.Where(loop => !loop.IsRound).SelectMany(loop => loop.EdgeIds).ToArray();
        }
        /// <summary>
        /// The source and the target side of a sweep wall, each an ordered chain of edges between its two vertices,
        /// the target side running back from the vertex across from the end of the source side.
        /// </summary>
        public GmshShellSpacingPatch(int wallId, int[][] sideEdgeIds, int[][] sideVertexIds)
        {
            _wallId = wallId;
            _wallSideEdgeIds = sideEdgeIds;
            _wallSideVertexIds = sideVertexIds;
            _edgeIds = sideEdgeIds[0].Concat(sideEdgeIds[1]).ToArray();
        }


        // Methods                                                                                                      
        public bool Prepare(Dictionary<int, GmshShellSpacingEdge> edges, int samples, double shapeTolerance)
        {
            if (_submap != null)
            {
                _pairs = GmshShellSubmapSpacing.GetPairs(_submap, edges);
                return _pairs != null && _pairs.Count > 0;
            }
            if (_face != null)
            {
                _first = GmshShellSpacingSide.Create(_face, _side, edges);
                _second = GmshShellSpacingSide.Create(_face, _side + 2, edges);
            }
            else
            {
                _first = GmshShellSpacingSide.Create(_wallSideEdgeIds[0], _wallSideVertexIds[0][0],
                                                     _wallSideVertexIds[0][1], edges);
                _second = GmshShellSpacingSide.Create(_wallSideEdgeIds[1], _wallSideVertexIds[1][0],
                                                      _wallSideVertexIds[1][1], edges);
            }
            if (_first == null || _second == null || _first.NumOfNodes != _second.NumOfNodes) return false;
            //
            double[][] a = _first.GetShapePoints(samples);
            double[][] b = _second.GetShapePoints(samples);
            int lastSample = samples - 1;
            for (int i = 0; i < samples; i++)
            {
                for (int j = i + 1; j < samples; j++)
                {
                    double first = GmshPoleChart.GetDistance(a[i], a[j]) / _first.Length;
                    double second = GmshPoleChart.GetDistance(b[lastSample - i], b[lastSample - j]) / _second.Length;
                    if (Math.Abs(first - second) > shapeTolerance) return false;
                }
            }
            //
            _length = (_first.Length + _second.Length) / 2;
            int last = _first.NumOfNodes - 1;
            _pairs = new List<(GmshShellSpacingNode First, GmshShellSpacingNode Second)>(last - 1);
            for (int k = 1; k < last; k++)
                _pairs.Add((_first.GetNode(k, _length, false), _second.GetNode(last - k, _length, true)));
            return _pairs.Count > 0;
        }
        public bool HasMismatch(double tolerance)
        {
            return _pairs.Any(pair => Math.Abs(pair.First.Coordinate - pair.Second.Coordinate) >
                                      _length * tolerance);
        }
        public void MarkSplitEdges(HashSet<GmshShellSpacingEdge> known)
        {
            _first?.MarkSplitEdges(known);
            _second?.MarkSplitEdges(known);
        }
        public void CopyKnownSpacing(HashSet<GmshShellSpacingEdge> known)
        {
            _first?.CopyKnownSpacing(_second, known);
            _second?.CopyKnownSpacing(_first, known);
        }
    }
}
