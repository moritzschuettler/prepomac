// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace vtkControl.Native
{
    // Mirrors vtkn_symbol of vtkNativeGeometry.h: the symbols of the AddOriented* methods and AddSphereActor
    public enum VtkNativeSymbol
    {
        Sphere = 0,
        DisplacementCone = 1,
        Cone = 2,
        RotationalConstraint = 3,
        Joint = 4,
        Arrow = 5,
        DoubleArrow = 6,
        Spring = 7,
        Thermo = 8,
        Flux = 9
    }
}
