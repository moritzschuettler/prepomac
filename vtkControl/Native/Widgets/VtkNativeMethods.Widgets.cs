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
        public const int WidgetsApiVersion = 4;


        // Callbacks
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int NumberFormatter(IntPtr userData, double value, IntPtr buffer, int bufferSize);


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

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_create(IntPtr view, int kind, out IntPtr widget, out int widgetId);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_destroy(IntPtr widget);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_set_text_style(IntPtr widget, ref VtkNativeTextStyle style);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_set_padding(IntPtr widget, int padding);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_set_visibility(IntPtr widget, int visible);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_set_frame(IntPtr widget, int backgroundVisible, double[] backgroundRgb,
                                                                   int borderVisible, double[] borderRgb);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_set_position(IntPtr widget, int anchor, double x, double y);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_get_geometry(IntPtr widget, [Out] double[] positionSize4,
                                                                      out int visible);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_get_rectangle(IntPtr widget, [Out] int[] rectangle4);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_get_highlight(IntPtr widget, [Out] double[] corners8, out int visible);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_widget_set_text(IntPtr widget, byte[] text);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_status_block_set_lines(IntPtr widget, byte[] line1, byte[] line2,
                                                                         byte[] line3Fixed, byte[] line3Factor, byte[] line4);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_status_block_get_scale_factor_hit(IntPtr widget, out int clicked,
                                                                                    [Out] int[] rectangle4);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scale_bar_set_width(IntPtr widget, int width);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scale_bar_set_unit(IntPtr widget, byte[] unit);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scale_bar_set_number_formatter(IntPtr widget, NumberFormatter formatter,
                                                                                 IntPtr userData);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_scale_bar_get_labels(IntPtr widget, byte[] buffer, int bufferSize,
                                                                       out int requiredSize);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_color_bar_set_colors_and_labels(IntPtr widget, double[] colorsRgb,
                                                                                  byte[] labels, int count);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_color_bar_get_title_rectangle(IntPtr widget, [Out] int[] rectangle4,
                                                                                out int found);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_arrow_set_anchor_point(IntPtr widget, double x, double y, double z);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_arrow_reset_position_from_anchor(IntPtr widget);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_arrow_reset_initial_position(IntPtr widget);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_arrow_offset_position(IntPtr widget, double dx, double dy);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_arrow_get_leader_line(IntPtr widget, [Out] double[] line4);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_triad_get_target(IntPtr widget, int x, int y, out int target);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_triad_update_geometry(IntPtr widget);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_widget_highlight(IntPtr view, int enabled, int localEnabled);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_interaction_settings(IntPtr view,
                                                                                ref VtkNativeInteractionSettings settings);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_handle_input(IntPtr view, int inputEvent, int x, int y, int ctrl,
                                                                    int shift, int keyCode, byte[] keySym);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_navigate(IntPtr view, int navigation, double a, double b, double c);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_rotation_point(IntPtr view, double[] point3);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_interaction_state(IntPtr view, [Out] int[] state4);
    }
}
