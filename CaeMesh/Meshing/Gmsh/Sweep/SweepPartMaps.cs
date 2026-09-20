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

namespace CaeMesh
{
    /// <summary>
    /// The maps of a part's visualization that the sweep detection asks for again on every source it tries.
    /// They depend on the part alone, so one instance of them serves every trial of one detection. They are
    /// read and never written, and one instance belongs to one detection, so nothing is shared between the
    /// detections of two groups running at the same time.
    /// </summary>
    public class SweepPartMaps
    {
        // Variables
        private Dictionary<int, Dictionary<int, HashSet<int>>> _partIdSurfaceIdSurfaceNeighbourIds;
        private Dictionary<int, Dictionary<int, HashSet<int>>> _partIdVertexIdEdgeIds;
        private Dictionary<int, List<HashSet<int>>> _partIdSmoothSurfaceIdGroups;
        private Dictionary<int, bool> _partIdIsTransfiniteBlock;


        // Constructors
        public SweepPartMaps()
        {
            _partIdSurfaceIdSurfaceNeighbourIds = new Dictionary<int, Dictionary<int, HashSet<int>>>();
            _partIdVertexIdEdgeIds = new Dictionary<int, Dictionary<int, HashSet<int>>>();
            _partIdSmoothSurfaceIdGroups = new Dictionary<int, List<HashSet<int>>>();
            _partIdIsTransfiniteBlock = new Dictionary<int, bool>();
        }


        // Methods
        public Dictionary<int, HashSet<int>> GetSurfaceIdSurfaceNeighbourIds(BasePart part)
        {
            Dictionary<int, HashSet<int>> surfaceIdSurfaceNeighbourIds;
            if (_partIdSurfaceIdSurfaceNeighbourIds.TryGetValue(part.PartId, out surfaceIdSurfaceNeighbourIds))
                return surfaceIdSurfaceNeighbourIds;
            //
            surfaceIdSurfaceNeighbourIds = part.Visualization.GetSurfaceIdSurfaceNeighbourIds();
            _partIdSurfaceIdSurfaceNeighbourIds.Add(part.PartId, surfaceIdSurfaceNeighbourIds);
            //
            return surfaceIdSurfaceNeighbourIds;
        }
        public Dictionary<int, HashSet<int>> GetVertexIdEdgeIds(BasePart part)
        {
            Dictionary<int, HashSet<int>> vertexIdEdgeIds;
            if (_partIdVertexIdEdgeIds.TryGetValue(part.PartId, out vertexIdEdgeIds)) return vertexIdEdgeIds;
            //
            vertexIdEdgeIds = part.Visualization.GetVertexIdEdgeIds();
            _partIdVertexIdEdgeIds.Add(part.PartId, vertexIdEdgeIds);
            //
            return vertexIdEdgeIds;
        }
        /// <summary>
        /// Whether the part is a block the transfinite mesher accepts, asked of the caller once per part.
        /// </summary>
        public bool IsTransfiniteBlock(BasePart part, Func<BasePart, bool> isTransfiniteBlock)
        {
            bool isBlock;
            if (_partIdIsTransfiniteBlock.TryGetValue(part.PartId, out isBlock)) return isBlock;
            //
            isBlock = isTransfiniteBlock(part);
            _partIdIsTransfiniteBlock.Add(part.PartId, isBlock);
            //
            return isBlock;
        }
        public List<HashSet<int>> GetSmoothSurfaceIdGroups(FeMesh geometry, BasePart part, double angleDeg)
        {
            List<HashSet<int>> smoothSurfaceIdGroups;
            if (_partIdSmoothSurfaceIdGroups.TryGetValue(part.PartId, out smoothSurfaceIdGroups))
                return smoothSurfaceIdGroups;
            //
            smoothSurfaceIdGroups = geometry.GetSmoothSurfaceIdGroups(part, angleDeg);
            _partIdSmoothSurfaceIdGroups.Add(part.PartId, smoothSurfaceIdGroups);
            //
            return smoothSurfaceIdGroups;
        }
    }
}
