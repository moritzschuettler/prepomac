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

namespace CaeMesh
{
    /// <summary>
    /// What the validation of one sweep block found for one part swept from one set of source surfaces: the
    /// side and the target surfaces, the side surfaces ring by ring, the edges following the sweep direction
    /// where the walls were found by following the edges, or the reason the block was refused. The result is
    /// a function of the part and the source alone, so one detection keeps it and reads it again for every
    /// trial that reaches the same source instead of validating the block again.
    /// </summary>
    public class SweepBlockValidation
    {
        // Variables                                                                                                                
        private readonly string _error;
        private readonly int[] _sideSurfaceIds;
        private readonly int[] _targetSurfaceIds;
        private readonly List<int[]> _layerSideSurfaceIds;
        private readonly int[] _directionEdgeIds;


        // Properties                                                                                                               
        public string Error { get { return _error; } }
        public int[] TargetSurfaceIds { get { return _targetSurfaceIds; } }


        // Constructors                                                                                                             
        public SweepBlockValidation(string error, int[] sideSurfaceIds, int[] targetSurfaceIds,
                                    List<int[]> layerSideSurfaceIds, int[] directionEdgeIds)
        {
            _error = error;
            _sideSurfaceIds = sideSurfaceIds;
            _targetSurfaceIds = targetSurfaceIds;
            _layerSideSurfaceIds = layerSideSurfaceIds;
            _directionEdgeIds = directionEdgeIds;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Copies of what was found, so that a caller may keep or renumber them without touching the kept result.
        /// </summary>
        public void CopyTo(out int[] sideSurfaceIds, out int[] targetSurfaceIds, out List<int[]> layerSideSurfaceIds,
                           out int[] directionEdgeIds)
        {
            sideSurfaceIds = _sideSurfaceIds != null ? _sideSurfaceIds.ToArray() : null;
            targetSurfaceIds = _targetSurfaceIds != null ? _targetSurfaceIds.ToArray() : null;
            layerSideSurfaceIds = _layerSideSurfaceIds != null ? _layerSideSurfaceIds.Select(ids => ids.ToArray()).ToList()
                                                               : null;
            directionEdgeIds = _directionEdgeIds != null ? _directionEdgeIds.ToArray() : null;
        }
    }
}
