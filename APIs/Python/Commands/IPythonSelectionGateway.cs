// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;

namespace APIs.Python
{
    public interface IPythonSelectionGateway
    {
        int GetViewId(string view);
        FeMesh GetMesh(string view);
        void Highlight(Selection selection);
        void ClearHighlight();
    }
}