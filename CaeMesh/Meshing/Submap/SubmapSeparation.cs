// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace CaeMesh
{
    /// <summary>
    /// Two parallel sides of a submapped face that face each other across it and must stay apart in its lattice: the
    /// counts of the edges along the loop from the first side to the second, those running towards the second side
    /// added and those running back subtracted, must add up to at least the bound.
    /// </summary>
    public class SubmapSeparation
    {
        // Variables                                                                                                                
        public int FaceId;
        public int[] TowardsEdgeIds;
        public int[] BackEdgeIds;
        public int Bound;
        public int FirstSideEdgeId;
        public int SecondSideEdgeId;


        // Constructors                                                                                                             
        public SubmapSeparation(int faceId, int[] towardsEdgeIds, int[] backEdgeIds, int bound, int firstSideEdgeId,
                                int secondSideEdgeId)
        {
            FaceId = faceId;
            TowardsEdgeIds = towardsEdgeIds;
            BackEdgeIds = backEdgeIds;
            Bound = bound;
            FirstSideEdgeId = firstSideEdgeId;
            SecondSideEdgeId = secondSideEdgeId;
        }
    }
}
