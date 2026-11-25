// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:


namespace vtkControl.Native
{
    // The widgets area of a native view (vtkNativeWidgets.h)
    public sealed partial class VtkNativeView
    {
        // Methods
        // Forwards a mouse event to the widgets; true when a widget took it
        public bool HandleMouseEvent(VtkNativeEvent mouseEvent, int x, int y)
        {
            int handled;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_handle_mouse_event(Handle(), (int)mouseEvent, x, y,
                                                                                  out handled));
            return handled != 0;
        }
        // The hover highlights of the widgets: off, or the whole widget only instead of the part under the mouse
        public void SetWidgetHighlight(bool enabled, bool localHighlightEnabled)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_widget_highlight(Handle(), enabled ? 1 : 0,
                                                                                    localHighlightEnabled ? 1 : 0));
        }
        // The interaction style (vtkInteractorStyleControl)
        public void SetInteractionSettings(VtkNativeInteractionSettings settings)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_interaction_settings(Handle(), ref settings));
        }
        // The input of an off screen view; an embedded view takes it from its window. keySym is the VTK key symbol
        public void HandleInput(VtkNativeInput input, int x, int y, bool ctrl = false, bool shift = false,
                                int keyCode = 0, string keySym = null)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_handle_input(Handle(), (int)input, x, y, ctrl ? 1 : 0,
                                                                            shift ? 1 : 0, keyCode,
                                                                            VtkNativeLibrary.ToUtf8(keySym)));
        }
        public void Navigate(VtkNativeNavigation navigation, double a = 0, double b = 0, double c = 0)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_navigate(Handle(), (int)navigation, a, b, c));
        }
        // A null point returns the rotation center to the center of the visible bounds
        public void SetRotationPoint(double[] point)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_rotation_point(Handle(), point));
        }
        // The VTKIS state of the style, the box selection, the box zoom, a smooth zoom still to apply
        public int[] GetInteractionState()
        {
            int[] state = new int[4];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_interaction_state(Handle(), state));
            return state;
        }
    }
}
