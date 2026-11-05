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
    // The declarations of vtkNativeSelection.h
    internal static partial class VtkNativeMethods
    {
        // Variables
        public const int SelectionApiVersion = 1;


        // Methods
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_pick_point(IntPtr view, int x, int y, int selectionDistance,
                                                                  int limitDistance, byte[] selectableParts,
                                                                  ref VtkNativePointPick pick, byte[] partName,
                                                                  int capacity, out int required);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_pick_area(IntPtr view, int x1, int y1, int x2, int y2,
                                                                 [Out] double[] planes36, byte[] partNames, int capacity,
                                                                 out int required);
    }
}
