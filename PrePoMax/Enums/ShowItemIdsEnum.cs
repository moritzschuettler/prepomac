// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax
{
    [Serializable, Flags]
    public enum ShowItemIdsEnum
    {
        None = 0,
        Vertices = 1,
        Edges = 2,
        Surfaces = 4,
        Parts = 8,
        PartNames = 16,
        Nodes = 32,
        Elements = 64
    }
}
