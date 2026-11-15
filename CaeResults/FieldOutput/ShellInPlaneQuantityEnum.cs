// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace CaeResults
{
    // The in-plane state of a ply: its stresses as the solver computes them in 3D, its plane stresses the elastic
    // constants of the ply give its in-plane strains, as classical lamination theory computes them, or its in-plane strains
    [Serializable]
    [PmxType(5030)]
    public enum ShellInPlaneQuantityEnum
    {
        [StandardValue("Stress", DisplayName = "Stress (3D as solved)")]
        Stress,
        [StandardValue("PlaneStressFromStrain", DisplayName = "Stress (plane stress from strains)")]
        PlaneStressFromStrain,
        [StandardValue("Strain", DisplayName = "Strain (in-plane)")]
        Strain
    }
}
