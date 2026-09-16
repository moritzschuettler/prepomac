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
        /// <summary>
        /// The edges that split the target inside its boundary - the imprints of the neighbours standing on
        /// it, and the imprints of the source carried up onto it - mapped to the edges of the source they
        /// stand above, so that the sweep lines starting on the source edge end on the target edge. Null when
        /// neither end is split inside.
        /// </summary>
        public Dictionary<int, int> ImprintEdgeCopyIds { get; set; }
        /// <summary>
        /// The vector that carries a point of the target onto the source. Null for a block that is not a
        /// prism.
        /// </summary>
        public double[] ImprintTranslation { get; set; }
        /// <summary>
        /// Whether an end of the block is split inside its boundary and the split was carried to the other
        /// end, which is what makes the block one whose sweep lines are pinned.
        /// </summary>
        public bool CarriesImprints { get; set; }


        // Constructors                                                                                                             
        public SweepBlock(IEnumerable<int> sourceSurfaceIds, IEnumerable<int> wallSurfaceIds,
                          IEnumerable<int> targetSurfaceIds)
        {
            _sourceSurfaceIds = new HashSet<int>(sourceSurfaceIds);
            _wallSurfaceIds = new HashSet<int>(wallSurfaceIds);
            _targetSurfaceIds = new HashSet<int>(targetSurfaceIds);
            VolumeId = 1;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Replaces the surfaces of the block after a geometry operation changed them.
        /// </summary>
        public void SetSurfaces(IEnumerable<int> sourceSurfaceIds, IEnumerable<int> wallSurfaceIds,
                                IEnumerable<int> targetSurfaceIds)
        {
            _sourceSurfaceIds.Clear();
            _sourceSurfaceIds.UnionWith(sourceSurfaceIds);
            _wallSurfaceIds.Clear();
            _wallSurfaceIds.UnionWith(wallSurfaceIds);
            _targetSurfaceIds.Clear();
            _targetSurfaceIds.UnionWith(targetSurfaceIds);
        }
    }
}
