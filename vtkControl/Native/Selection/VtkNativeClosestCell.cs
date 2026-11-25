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
    // Mirrors vtkn_closest_cell of vtkNativeSelection.h; the fixed arrays are copied by the marshaler
    [StructLayout(LayoutKind.Sequential)]
    internal struct VtkNativeClosestCell
    {
        // Variables
        public double PointX;
        public double PointY;
        public double PointZ;
        public int ElementId;
        public int NodeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public int[] NodeIds;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public double[] NodeCoordinates;
    }
}
