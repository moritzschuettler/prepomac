// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // Mirrors vtkn_point_pick of vtkNative.h, field by field so that the struct stays blittable
    [StructLayout(LayoutKind.Sequential)]
    internal struct VtkNativePointPick
    {
        // Variables
        public double PointX;
        public double PointY;
        public double PointZ;
        public double DirectionX;
        public double DirectionY;
        public double DirectionZ;
        public int Hit;
        public int CellId;
    }
}
