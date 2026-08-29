// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;

namespace CaeMesh
{
    /// <summary>
    /// Geometry inferred between two exploded-view clusters. Several flags can describe one relation; for
    /// example a shaft located by a cylindrical fit and stopped by a plane is Cylindrical | Planar.
    /// </summary>
    [Flags]
    public enum ExplodedViewRelationType
    {
        None = 0,
        PhysicalContact = 1,
        Planar = 2,
        Cylindrical = 4,
        Conical = 8,
        SharedShaft = 16,
        Prismatic = 32
    }
}
