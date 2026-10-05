// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Stops the preview of a sweep whose counts no assignment keeps, once the counts closest to its rows are set on the
    /// curves, so that the preview shows them with the surfaces and edges of the rows that cannot be kept. It is not a
    /// CaeException, so that no retry of the sweep takes it for a failed attempt.
    /// </summary>
    public class SweepCountConflictException : Exception
    {
        // Variables
        /// <summary>Added to the name of the mesh file of the preview for the file listing the surfaces and edges.</summary>
        public const string FileExtension = ".conflicts";
        private readonly int[] _surfaceIds;
        private readonly int[] _edgeIds;


        // Properties
        /// <summary>The gmsh tags of the surfaces of the rows that cannot be kept.</summary>
        public int[] SurfaceIds { get { return _surfaceIds; } }
        /// <summary>The gmsh tags of the edges of the rows that cannot be kept.</summary>
        public int[] EdgeIds { get { return _edgeIds; } }


        // Constructors
        public SweepCountConflictException(string message, int[] surfaceIds, int[] edgeIds)
            : base(message)
        {
            _surfaceIds = surfaceIds;
            _edgeIds = edgeIds;
        }
    }
}
