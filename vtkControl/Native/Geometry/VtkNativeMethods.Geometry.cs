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
    // The declarations of vtkNativeGeometry.h
    internal static partial class VtkNativeMethods
    {
        // Variables
        public const int GeometryApiVersion = 1;


        // Methods
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_part_actor(IntPtr view, byte[] name, ref VtkNativePartStyle style,
                                                                      ref VtkNativeMesh geometry, IntPtr modelEdges,
                                                                      IntPtr modelEdgesToDraw);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_clear_actors(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_edges_visibility(IntPtr view, int edgesVisibility);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_lighting(IntPtr view, double ambient, double diffuse);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_mesh_actor(IntPtr view, byte[] name, double[] points,
                                                                      int pointCount, int[] cells, int cellsLength,
                                                                      byte[] cellTypes, int cellCount,
                                                                      double[] colorRgb, int edgesVisible);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_remove_actor(IntPtr view, byte[] name);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_actor_names(IntPtr view, byte[] buffer, int capacity,
                                                                       out int required);
    }
}
