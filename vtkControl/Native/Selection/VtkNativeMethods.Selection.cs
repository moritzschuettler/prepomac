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
        public const int SelectionApiVersion = 5;


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
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_section_view_pick_mode(IntPtr view, int partSelection,
                                                                                  int nodeOrElementSelection);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_closest_cell(IntPtr view, double[] point3, double maxDistance,
                                                                        byte[] selectableParts,
                                                                        ref VtkNativeClosestCell cell);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_closest_cells_on_parts(IntPtr view, double[] point3,
                                                                                  byte[] partNames, int partCount,
                                                                                  [Out] double[] distances,
                                                                                  [Out] double[] normals3);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_selection_precision(IntPtr view, int selectionDistance,
                                                                               out double precision);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_part_bounds(IntPtr view, byte[] name, [Out] double[] bounds6);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_smooth_part_to_stl(IntPtr view, byte[] name, byte[] fileName);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_volume_grid(IntPtr view, byte[] name, IntPtr grid);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_ids_inside_frustum(IntPtr view, byte[] name, double[] planes,
                                                                              int planeCount, [Out] int[] pointIds,
                                                                              int pointCapacity, out int pointCount,
                                                                              [Out] int[] cellIds, int cellCapacity,
                                                                              out int cellCount);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_highlight(IntPtr view, ref VtkNativeMesh mesh,
                                                                     ref VtkNativeHighlightStyle style);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_part_highlight(IntPtr view, byte[] name,
                                                                          ref VtkNativeHighlightStyle style);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_clear_highlights(IntPtr view, int currentOnly);
    }
}
