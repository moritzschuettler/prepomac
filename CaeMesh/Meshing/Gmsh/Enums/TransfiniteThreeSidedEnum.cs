// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using DynamicTypeDescriptor;

namespace CaeMesh
{
    /// <summary>
    /// How a 3-sided face is structured. Polar collapses one corner of a grid, with triangles at that corner and arcs
    /// around it, as a revolution about it would; Mapped is three blocks of quads around one inner node.
    /// </summary>
    [Serializable]
    public enum TransfiniteThreeSidedEnum
    {
        [StandardValue("Polar", DisplayName = "Polar")]
        Polar,
        //
        [StandardValue("Mapped", DisplayName = "Mapped")]
        Mapped,
        //
        [StandardValue("No", DisplayName = "No")]
        No
    }
}
