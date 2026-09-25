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

namespace CaeMesh.Sweep
{
    /// <summary>
    /// The mesh QuadWild made of the source surfaces of a group: the nodes inside every edge, the nodes inside the
    /// surfaces and the elements of each surface.
    /// </summary>
    public class SweepQuadWildMesh
    {
        // Variables                                                                                                                
        private Dictionary<int, double[][]> _edgeIdNodeCoors;
        private double[][] _interiorNodeCoors;
        private int[] _interiorNodeSurfaceIds;
        private SweepQuadWildNode[][] _elements;
        private int[] _elementSurfaceIds;


        // Properties                                                                                                               
        /// <summary>
        /// The nodes inside every edge of the group, vertices excluded, in the order of the edge's parameter.
        /// </summary>
        public Dictionary<int, double[][]> EdgeIdNodeCoors { get { return _edgeIdNodeCoors; } }
        public double[][] InteriorNodeCoors { get { return _interiorNodeCoors; } }
        public int[] InteriorNodeSurfaceIds { get { return _interiorNodeSurfaceIds; } }
        public SweepQuadWildNode[][] Elements { get { return _elements; } }
        public int[] ElementSurfaceIds { get { return _elementSurfaceIds; } }


        // Constructors                                                                                                             
        public SweepQuadWildMesh(Dictionary<int, double[][]> edgeIdNodeCoors, double[][] interiorNodeCoors,
                                 int[] interiorNodeSurfaceIds, SweepQuadWildNode[][] elements, int[] elementSurfaceIds)
        {
            _edgeIdNodeCoors = edgeIdNodeCoors;
            _interiorNodeCoors = interiorNodeCoors;
            _interiorNodeSurfaceIds = interiorNodeSurfaceIds;
            _elements = elements;
            _elementSurfaceIds = elementSurfaceIds;
        }


        // Methods                                                                                                                  
        public int GetNumOfElements(int edgeId)
        {
            return _edgeIdNodeCoors[edgeId].Length + 1;
        }
    }
}
