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
        public const int ApiVersion = 3;
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
        public static extern VtkNativeStatus vtkn_view_set_background(IntPtr view, int gradient, double[] topRgb,
                                                                      double[] bottomRgb);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_smoothing(IntPtr view, int pointSmoothing, int lineSmoothing);
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
        public static extern VtkNativeStatus vtkn_view_set_view_parameters(IntPtr view, double[] values10);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_standard_view(IntPtr view, int orientation, int zoomToFit);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_axis_view(IntPtr view, double[] axis, int zoomToFit);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_vertical_axis_view(IntPtr view, double[] axis, int zoomToFit);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_zoom_to_fit(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_zoom_factor(IntPtr view, double parallelScale);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_box_zoom(IntPtr view, int x1, int y1, int x2, int y2);
        //
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
        //
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_handle_mouse_event(IntPtr view, int eventId, int x, int y,
                                                                          out int handled);
        //
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_create(IntPtr view, out IntPtr bar, out int widgetId);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_destroy(IntPtr bar);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_set_text_style(IntPtr bar, ref VtkNativeTextStyle style);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_set_padding(IntPtr bar, int padding);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_set_title(IntPtr bar, byte[] title);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_set_colors(IntPtr bar, ref VtkNativeScalarBarColors colors);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_set_visibility(IntPtr bar, int visible);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_set_frame(IntPtr bar, int backgroundVisible,
                                                                       double[] backgroundRgb, int borderVisible,
                                                                       double[] borderRgb);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_set_position(IntPtr bar, int anchor, double x, double y);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_get_rectangle(IntPtr bar, [Out] int[] rectangle4);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_get_value_label_rectangle(IntPtr bar, int minimum,
                                                                                       [Out] int[] rectangle4,
                                                                                       out int found);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_hit_value_label(IntPtr bar, int x, int y, out int label,
                                                                             [Out] int[] rectangle4);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scalar_bar_get_highlight_rectangle(IntPtr bar, [Out] int[] rectangle4,
                                                                                     out int visible);
    }
}
