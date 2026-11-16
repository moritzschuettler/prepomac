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
    /// added and those running back subtracted, must add up to at least the bound. A row that leaves room for holes
    /// between the two sides also subtracts the widths of the holes, whose first edges it names. A corner row has no
    /// second side: the edge of its first side must carry at least the bound of elements, so that the cell between a
    /// corner of the outer loop and the corner of a hole close to it is not a dart. A thin wall row is a row of room
    /// turned round, its bound negative, which keeps the room for a hole between thin walls at a cell on each side.
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
        public int[] HoleEdgeIds;
        public bool IsCornerRow;
        public bool IsThinWallRow;


        // Constructors                                                                                                             
        public SubmapSeparation(int faceId, int[] towardsEdgeIds, int[] backEdgeIds, int bound, int firstSideEdgeId,
                                int secondSideEdgeId)
            : this(faceId, towardsEdgeIds, backEdgeIds, bound, firstSideEdgeId, secondSideEdgeId, null)
        {
        }
        public SubmapSeparation(int faceId, int[] towardsEdgeIds, int[] backEdgeIds, int bound, int firstSideEdgeId,
                                int secondSideEdgeId, int[] holeEdgeIds)
        {
            FaceId = faceId;
            TowardsEdgeIds = towardsEdgeIds;
            BackEdgeIds = backEdgeIds;
            Bound = bound;
            FirstSideEdgeId = firstSideEdgeId;
            SecondSideEdgeId = secondSideEdgeId;
            HoleEdgeIds = holeEdgeIds;
        }
    }
}
