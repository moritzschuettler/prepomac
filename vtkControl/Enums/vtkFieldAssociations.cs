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

namespace vtkControl
{
    public enum vtkFieldAssociations
    {
        FIELD_ASSOCIATION_POINTS,
        FIELD_ASSOCIATION_CELLS,
        FIELD_ASSOCIATION_NONE,
        FIELD_ASSOCIATION_POINTS_THEN_CELLS,
        FIELD_ASSOCIATION_VERTICES,
        FIELD_ASSOCIATION_EDGES,
        FIELD_ASSOCIATION_ROWS,
        NUMBER_OF_ASSOCIATIONS
    };
}
