// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python
{
    // Resolves the mesh of a selection view. It exists so that the selection service does not have to
    // know about results.
    internal interface IMeshResolver
    {
        FeMesh GetMesh(string view, string resultName);
    }
}
