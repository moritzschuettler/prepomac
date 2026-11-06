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
    // Mirrors vtkn_symbol_parameters of vtkNativeGeometry.h
    [StructLayout(LayoutKind.Sequential)]
    public struct VtkNativeSymbolParameters
    {
        // Variables
        public int Kind;
        public int Joint;
        public double SymbolSize;
        public double RelativeSize;
        public int Invert;
        public int Center;
        public int DrawEdges;
    }
}
