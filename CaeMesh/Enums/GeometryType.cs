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

namespace CaeMesh
{
    // Used to encode the selected geometry items into ItemTypePart id
    [Serializable]
    public enum GeometryType
    {
        Unknown = -1,
        Vertex = 1,
        Edge = 2,
        SolidSurface = 3,
        ShellFrontSurface = 4,
        ShellBackSurface = 5,
        ShellEdgeSurface = 6,
        Part = 9
    }
}