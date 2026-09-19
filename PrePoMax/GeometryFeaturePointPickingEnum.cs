// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    /// <summary>
    /// What a geometry feature does with an edge that is picked. An edge can stand for the point it
    /// carries or for the direction it runs in, and only the operation knows which readings it accepts.
    /// </summary>
    public enum GeometryFeaturePointPickingEnum
    {
        None,
        PointOnly,
        PointOrEntity
    }
}
