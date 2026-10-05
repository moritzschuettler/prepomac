// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Linq;

namespace CaeMesh
{
    /// <summary>
    /// A face of a geometry part that is meshed by a structured grid: its three or four corners, in the order its
    /// boundary loop runs, and the edges of each side between them. A side is a chain of edges, joined where they
    /// meet at a flatter angle than the transfinite angle.
    /// </summary>
    [Serializable]
    public class TransfiniteFace
    {
        // Variables
        private int _faceId;
        private int[] _cornerNodeIds;
        private double[] _cornerAnglesDeg;
        private int[][] _sideEdgeIds;


        // Properties
        public int FaceId { get { return _faceId; } }
        /// <summary>
        /// The vertex node ids of the corners in loop order. Side k runs from corner k to corner k + 1.
        /// </summary>
        public int[] CornerNodeIds { get { return _cornerNodeIds; } }
        public double[] CornerAnglesDeg { get { return _cornerAnglesDeg; } }
        /// <summary>
        /// The edge ids of every side, in the order the side runs from its first corner.
        /// </summary>
        public int[][] SideEdgeIds { get { return _sideEdgeIds; } }
        public int NumberOfSides { get { return _cornerNodeIds.Length; } }
        /// <summary>
        /// The corner a three-sided face moves its singular point towards, the sharpest one, or -1 when the face has
        /// four sides.
        /// </summary>
        public int SharpestCornerIndex
        {
            get
            {
                if (_cornerNodeIds.Length != 3) return -1;
                int sharpest = 0;
                for (int i = 1; i < _cornerAnglesDeg.Length; i++)
                {
                    if (_cornerAnglesDeg[i] < _cornerAnglesDeg[sharpest]) sharpest = i;
                }
                return sharpest;
            }
        }


        // Constructors
        public TransfiniteFace(int faceId, int[] cornerNodeIds, double[] cornerAnglesDeg, int[][] sideEdgeIds)
        {
            _faceId = faceId;
            _cornerNodeIds = cornerNodeIds;
            _cornerAnglesDeg = cornerAnglesDeg;
            _sideEdgeIds = sideEdgeIds;
        }


        // Methods
        /// <summary>
        /// The corner a polar grid of a three-sided face collapses: the grid maps the other two corners to right angles,
        /// so the corner whose other two lie closest to a right angle. Of corners within a degree of each other the one
        /// between the two sides closest in length is taken, which is the axis of a pie cut from a cylinder. sideLengths
        /// holds the length of every side, in the order of the sides.
        /// </summary>
        public int GetPolarCornerIndex(double[] sideLengths)
        {
            if (_cornerNodeIds.Length != 3) return -1;
            //
            Func<int, double> getDeviationDeg = corner =>
                Math.Max(Math.Abs(_cornerAnglesDeg[(corner + 1) % 3] - 90),
                         Math.Abs(_cornerAnglesDeg[(corner + 2) % 3] - 90));
            Func<int, double> getLengthDifference = corner =>
            {
                double length1 = sideLengths[(corner + 2) % 3];
                double length2 = sideLengths[corner];
                double max = Math.Max(length1, length2);
                return max > 0 ? Math.Abs(length1 - length2) / max : 0;
            };
            int polar = 0;
            for (int corner = 1; corner < 3; corner++)
            {
                double difference = getDeviationDeg(corner) - getDeviationDeg(polar);
                if (difference < -1 || (difference <= 1 && getLengthDifference(corner) < getLengthDifference(polar)))
                    polar = corner;
            }
            return polar;
        }
        /// <summary>
        /// The corners of a three-sided face starting at its polar corner, in loop order, as gmsh takes them: the first
        /// corner of a three-sided transfinite surface is the one it collapses.
        /// </summary>
        public int[] GetPolarCornerNodeIds(int polarCornerIndex)
        {
            return Enumerable.Range(0, 3).Select(i => _cornerNodeIds[(polarCornerIndex + i) % 3]).ToArray();
        }
        /// <summary>
        /// The two sides of a three-sided face that meet at its polar corner, which a polar grid divides equally: the
        /// side that ends at the corner and the side that starts from it.
        /// </summary>
        public TransfiniteSidePair GetPolarSidePair(int polarCornerIndex)
        {
            return new TransfiniteSidePair(_faceId, _sideEdgeIds[(polarCornerIndex + 2) % 3],
                                           _sideEdgeIds[polarCornerIndex]);
        }
    }
}
