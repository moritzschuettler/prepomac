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
    // The declarations of vtkNativeWidgets.h
    internal static partial class VtkNativeMethods
    {
        // Variables
        public const int WidgetsApiVersion = 1;


        // Methods
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_handle_mouse_event(IntPtr view, int eventId, int x, int y,
                                                                          out int handled);
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
