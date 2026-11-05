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
    // Mirrors vtkn_scalar_bar_colors of vtkNative.h. The two pointers are pinned arrays the caller keeps alive for
    // the duration of the call
    [StructLayout(LayoutKind.Sequential)]
    internal struct VtkNativeScalarBarColors
    {
        // Variables
        public IntPtr TableColors;
        public int TableColorCount;
        public int NumberOfColors;
        public int SmoothColors;
        public int AddMinColor;
        public int AddMaxColor;
        public int MinColorVisible;
        public int MaxColorVisible;
        public IntPtr Labels;
    }
}
