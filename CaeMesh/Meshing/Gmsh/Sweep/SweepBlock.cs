// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// The topology of a single one-to-one sweep block: one connected group of source surfaces,
    /// the wall (side) surfaces that carry the structured layer grid, and the target surfaces.
    ///
    /// A many-to-one sweep is an ordered list of these blocks. The blocks are swept one after
    /// another and the target layer of one block becomes the source layer of the next, so the
    /// element builder never needs to know whether it is meshing the first block or the tenth.
    /// The decomposition that produces the list is deliberately kept out of this class.
    /// </summary>
    public class SweepBlock
    {
        // Variables                                                                                                                
        private readonly HashSet<int> _sourceSurfaceIds;
        private readonly HashSet<int> _wallSurfaceIds;
        private readonly HashSet<int> _targetSurfaceIds;


        // Properties                                                                                                               
        public HashSet<int> SourceSurfaceIds { get { return _sourceSurfaceIds; } }
        public HashSet<int> WallSurfaceIds { get { return _wallSurfaceIds; } }
        public HashSet<int> TargetSurfaceIds { get { return _targetSurfaceIds; } }
        /// <summary>
        /// The gmsh volume the generated solid elements are classified on.
        /// </summary>
        public int VolumeId { get; set; }


        // Constructors                                                                                                             
        public SweepBlock(IEnumerable<int> sourceSurfaceIds, IEnumerable<int> wallSurfaceIds,
                          IEnumerable<int> targetSurfaceIds)
        {
            _sourceSurfaceIds = new HashSet<int>(sourceSurfaceIds);
            _wallSurfaceIds = new HashSet<int>(wallSurfaceIds);
            _targetSurfaceIds = new HashSet<int>(targetSurfaceIds);
            VolumeId = 1;
        }
    }
}
