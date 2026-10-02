// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Linq;

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
        /// it, the imprints of the source carried up onto it, and the edges a bent source puts there of its
        /// own - mapped to the edges of the source they stand above, so that the sweep lines starting on the
        /// source edge end on the target edge. Null when neither end is split inside, and when the two ends
        /// are split in ways that cannot be matched.
        /// </summary>
        public Dictionary<int, int> ImprintEdgeCopyIds { get; set; }
        /// <summary>
        /// The vector that carries a point of the target onto the source. Null for a block that is not a
        /// prism.
        /// </summary>
        public double[] ImprintTranslation { get; set; }
        /// <summary>
        /// Whether the block closes a ring of blocks: its targets are the sources of blocks swept before it, so they
        /// are meshed already, and the block is swept between two meshed caps rather than onto its target.
        /// </summary>
        public bool ClosesRing { get; set; }
        /// <summary>
        /// The axis a revolved block turns its source about and the angle it turns it through: a point on the axis,
        /// its direction and the angle in degrees, x, y, z, dx, dy, dz, angle. Turning the source through the angle
        /// about the direction carries it onto the target. Null for a block that is swept, not revolved.
        /// </summary>
        public double[] RevolveAxis { get; set; }
        public bool IsRevolved { get { return RevolveAxis != null; } }


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
        /// <summary>
        /// The block the way a message names it, by the gmsh tags of its source surfaces, which the gmsh interface
        /// renames to the surfaces of the model on the way out.
        /// </summary>
        public string GetDescription()
        {
            return (IsRevolved ? "the sweep block revolved from " : "the sweep block swept from ") +
                   string.Join(", ", _sourceSurfaceIds.OrderBy(id => id).Select(id => "surface " + id));
        }
    }
}
