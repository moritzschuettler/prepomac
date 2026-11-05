// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // Mirrors vtkn_mesh of vtkNative.h. The pointers are those of arrays pinned by VtkNativeMeshData for the call
    [StructLayout(LayoutKind.Sequential)]
    internal struct VtkNativeMesh
    {
        public IntPtr Coordinates;
        public int PointCount;
        public IntPtr Cells;
        public int CellsLength;
        public IntPtr CellTypes;
        public int CellCount;
        public IntPtr PointIds;
        public IntPtr CellIds;
    }
}
