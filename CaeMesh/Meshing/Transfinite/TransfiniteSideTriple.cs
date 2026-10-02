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
    /// The three sides of a three-sided structured face, each a chain of edges, meshed as three blocks of quads around
    /// one inner node. The inner line from that node to side k carries L(k) elements and side k carries
    /// L(k - 1) + L(k + 1), so the sides need an even sum and every one of them fewer elements than the other two
    /// together: then every inner line carries at least one.
    /// </summary>
    public class TransfiniteSideTriple
    {
        // Variables
        public int FaceId;
        public int[][] Sides;


        // Constructors
        public TransfiniteSideTriple(int faceId, int[][] sides)
        {
            FaceId = faceId;
            Sides = sides;
        }


        // Methods
        /// <summary>
        /// The number of elements on the inner line from the inner node to side k, from the numbers on the sides.
        /// </summary>
        public static int GetInnerLineCount(int[] sideCounts, int k)
        {
            return (sideCounts[(k + 2) % 3] + sideCounts[(k + 1) % 3] - sideCounts[k]) / 2;
        }
        public static bool AreCountsValid(int[] sideCounts)
        {
            if (sideCounts.Sum() % 2 != 0) return false;
            for (int k = 0; k < 3; k++)
            {
                if (GetInnerLineCount(sideCounts, k) < 1) return false;
            }
            return true;
        }
    }
}
