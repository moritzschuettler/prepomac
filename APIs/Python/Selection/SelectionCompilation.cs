// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python
{
    internal class SelectionCompilation
    {
        // Properties
        public CaeGlobals.Selection Selection { get; private set; }
        public int[] Ids { get; private set; }


        // Constructors
        public SelectionCompilation(CaeGlobals.Selection selection, int[] ids)
        {
            Selection = selection;
            Ids = ids;
        }
    }
}
