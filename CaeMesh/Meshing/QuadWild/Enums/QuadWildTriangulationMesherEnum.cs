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
    // The meshers ShellQuadWild.TriangulationMesher accepts, with the values of MesherTypeEnum, so that the
    // Python API can offer and parse exactly these and cast the result
    public enum QuadWildTriangulationMesherEnum
    {
        Netgen = (int)MesherTypeEnum.Netgen,
        Mmg = (int)MesherTypeEnum.Mmg,
        Gmsh = (int)MesherTypeEnum.Gmsh
    }
}
