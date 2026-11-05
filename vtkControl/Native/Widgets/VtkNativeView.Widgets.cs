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
    }
}
