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

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Source surfaces of a sweep that touch each other along an edge and are meshed with QuadWild in one run, so
    /// that an edge between two of them is a feature line of that run rather than a boundary each side meshes on
    /// its own. An edge is fixed when its nodes are set by something other than this run - the user, the layers of
    /// another block, an imprint, or a surface that is not meshed here - and QuadWild is given its nodes; every
    /// other edge is free and takes the count QuadWild chooses.
    /// </summary>
    public class SweepQuadWildGroup
    {
        // Variables                                                                                                                
        private int _index;
        private int[] _surfaceIds;
        private int[] _edgeIds;
        private int[] _vertexIds;
        private HashSet<int> _fixedEdgeIds;
        private SweepQuadWildMesh _mesh;


        // Properties                                                                                                               
        public int Index { get { return _index; } }
        public int[] SurfaceIds { get { return _surfaceIds; } }
        public int[] EdgeIds { get { return _edgeIds; } }
        public int[] VertexIds { get { return _vertexIds; } }
        public HashSet<int> FixedEdgeIds { get { return _fixedEdgeIds; } }
        public IEnumerable<int> FreeEdgeIds { get { return _edgeIds.Where(id => !_fixedEdgeIds.Contains(id)); } }
        /// <summary>
        /// The mesh of the last run, null until the group is meshed or when the counts the walls settled no longer
        /// agree with it.
        /// </summary>
        public SweepQuadWildMesh Mesh { get { return _mesh; } set { _mesh = value; } }


        // Constructors                                                                                                             
        public SweepQuadWildGroup(int index, int[] surfaceIds, int[] edgeIds, int[] vertexIds, HashSet<int> fixedEdgeIds)
        {
            _index = index;
            _surfaceIds = surfaceIds;
            _edgeIds = edgeIds;
            _vertexIds = vertexIds;
            _fixedEdgeIds = fixedEdgeIds;
            _mesh = null;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// The surfaces of the group one by one, by their gmsh tags, which a message of the gmsh interface renames
        /// to the surfaces of the model on its way out.
        /// </summary>
        public string GetDescription()
        {
            return String.Join(", ", _surfaceIds.Select(id => "surface " + id));
        }
    }
}
