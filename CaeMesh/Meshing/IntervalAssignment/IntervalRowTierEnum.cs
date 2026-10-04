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
    /// How firmly a row of the interval assignment holds. A hard row is what the mesher cannot do without - a wall that
    /// is no grid, a sweep whose layers do not line up. A structured row is a face that may still be meshed free when
    /// it cannot be kept, at a high price. A preferred row only improves the grid.
    /// </summary>
    public enum IntervalRowTierEnum
    {
        Hard,
        Structured,
        Preferred
    }
}
