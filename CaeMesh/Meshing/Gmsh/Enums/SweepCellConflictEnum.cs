// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace CaeMesh
{
    /// <summary>
    /// Why two cells of a compound, each swept one way or left to tetrahedra, do not agree on a surface they share.
    /// </summary>
    public enum SweepCellConflictEnum
    {
        None,
        TargetSplitInside,
        TargetsMeet,
        CapAgainstWall,
        WallSidesDiffer,
        UnsortedSurface
    }
}
