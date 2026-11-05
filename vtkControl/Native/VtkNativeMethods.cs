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
    // The declarations of vtkNative.h, one to one. Strings go in as zero terminated UTF-8 byte arrays and come out
    // through byte buffers, because .NET Framework has no UTF-8 string marshalling that .NET 10 shares
    internal static class VtkNativeMethods
    {
        // Variables
        public const string LibraryName = "vtkNative.dll";
        public const int ApiVersion = 1;
        public const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;


        // Callbacks
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogCallback(IntPtr userData, int level, IntPtr message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EventCallback(IntPtr userData, int eventId, IntPtr values, int valueCount);


        // Methods
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string fileName, IntPtr reserved, uint flags);
        //
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vtkn_get_api_version();
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_get_vtk_version(byte[] buffer, int capacity, out int required);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_get_last_error(byte[] buffer, int capacity, out int required);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_set_log_callback(LogCallback callback, IntPtr userData);
        //
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_create(int offScreen, out IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_destroy(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_parent_window(IntPtr view, IntPtr parentWindow);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_size(IntPtr view, int width, int height);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_size(IntPtr view, out int width, out int height);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_event_callback(IntPtr view, EventCallback callback,
                                                                          IntPtr userData);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_background(IntPtr view, double[] topRgb, double[] bottomRgb);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_render(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_render_info(IntPtr view, byte[] buffer, int capacity,
                                                                       out int required);
        //
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_camera(IntPtr view, [Out] double[] values10);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_camera(IntPtr view, double[] values10);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_reset_camera(IntPtr view);
        //
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
        //
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_read_pixels(IntPtr view, int magnification, [Out] byte[] buffer,
                                                                   int capacity, out int width, out int height);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_write_png(IntPtr view, byte[] fileName, int magnification);
    }
}
