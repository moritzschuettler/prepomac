// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Gmsh element type ids, as returned by Gmsh.Model.Mesh.GetElementProperties.
    /// </summary>
    public enum GmshElementType
    {
        LinearBeam = 1,
        LinearTriangle = 2,
        LinearQuadrilateral = 3,
        LinearTetra = 4,
        LinearHexa = 5,
        LinearWedge = 6,
        LinearPyramid = 7
    }
}
