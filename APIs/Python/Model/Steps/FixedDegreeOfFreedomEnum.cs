// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python.Model
{
    // The degrees of freedom fixed_dofs names. The dialog offers Fixed for the displacements only: a rotation stores
    // positive infinity as 0, so it cannot be fixed
    public enum FixedDegreeOfFreedomEnum
    {
        U1,
        U2,
        U3
    }
}
