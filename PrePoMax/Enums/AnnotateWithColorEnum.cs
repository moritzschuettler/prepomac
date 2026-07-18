// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace PrePoMax
{
    [Serializable]
    [Flags]
    public enum AnnotateWithColorEnum
    {
        None = 1,
        FaceOrientation = 2,
        Parts = 4,
        Materials = 8,
        Sections = 16,
        SectionThicknesses = 32,
        ReferencePoints = 64,
        Constraints = 128,
        ContactPairs = 256,
        InitialConditions = 512,
        BoundaryConditions = 1024,
        Loads = 2048,
        DefinedFields = 4096
    }
}
