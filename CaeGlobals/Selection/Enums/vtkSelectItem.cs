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
using System.Runtime.Serialization;

namespace CaeGlobals
{
    [Serializable]
    public enum vtkSelectItem
    {   
        None = 0,
        Node = 1,               // nodeIds
        Element = 2,            // elementIds
        GeometryEdge = 3,       // geometryEdgeIds
        Surface = 4,            // faceIds
        Geometry = 5,           // geometryIds
        Part = 6,               // partIds
        //
        GeometryVertex = 9,     // geometryVertexIds
        GeometrySurface = 10,   // geometrySurfaceIds
    }
}
