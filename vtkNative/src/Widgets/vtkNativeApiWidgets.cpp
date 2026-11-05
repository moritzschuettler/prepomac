// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The exported C functions of the widgets area. Each one only checks its arguments, finds its object and forwards,
// inside the guard of vtkNativeGuard.h.
#include "vtkNativeGuard.h"
#include "vtkNativeScalarBar.h"
#include "vtkNativeView.h"

#include <algorithm>

using namespace vtkn;

extern "C"
{
    vtkn_status VTKN_CALL vtkn_view_handle_mouse_event(vtkn_view view, int32_t event_id, int32_t x, int32_t y,
                                                       int32_t* handled)
    {
        return GuardVtk([&]() {
            bool widgetHandled = View::FromHandle(view)->HandleMouseEvent(event_id, x, y);
            if (handled != nullptr) *handled = widgetHandled ? 1 : 0;
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_create(vtkn_view view, vtkn_scalar_bar* bar, int32_t* widget_id)
    {
        return GuardVtk([&]() {
            Require(bar != nullptr, "The argument 'bar' must not be null.");
            *bar = nullptr;
            ScalarBar* created = View::FromHandle(view)->CreateScalarBar();
            *bar = created;
            if (widget_id != nullptr) *widget_id = created->Id;
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_destroy(vtkn_scalar_bar bar)
    {
        return GuardVtk([&]() {
            ScalarBar* scalarBar = ScalarBar::FromHandle(bar);
            scalarBar->Owner->DestroyScalarBar(scalarBar);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_set_text_style(vtkn_scalar_bar bar, const vtkn_text_style* style)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            ScalarBar::FromHandle(bar)->Widget.SetTextStyle(*style);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_set_padding(vtkn_scalar_bar bar, int32_t padding)
    {
        return GuardVtk([&]() {
            Require(padding >= 0, "The padding must not be negative.");
            ScalarBar::FromHandle(bar)->Widget.SetPadding(padding);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_set_title(vtkn_scalar_bar bar, const char* title)
    {
        return GuardVtk([&]() {
            ScalarBar::FromHandle(bar)->Widget.SetTitle(ToName(title, "title"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_set_colors(vtkn_scalar_bar bar, const vtkn_scalar_bar_colors* colors)
    {
        return GuardVtk([&]() {
            Require(colors != nullptr, "The argument 'colors' must not be null.");
            ScalarBar::FromHandle(bar)->Widget.SetColors(*colors);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_set_visibility(vtkn_scalar_bar bar, int32_t visible)
    {
        return GuardVtk([&]() {
            ScalarBarWidget& widget = ScalarBar::FromHandle(bar)->Widget;
            if (visible != 0) widget.VisibilityOn();
            else widget.VisibilityOff();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_set_frame(vtkn_scalar_bar bar, int32_t background_visible,
                                                    const double* background_rgb, int32_t border_visible,
                                                    const double* border_rgb)
    {
        return GuardVtk([&]() {
            Require(background_rgb != nullptr && border_rgb != nullptr, "The frame colors must not be null.");
            ScalarBarWidget& widget = ScalarBar::FromHandle(bar)->Widget;
            widget.SetBackgroundColor(background_rgb[0], background_rgb[1], background_rgb[2]);
            widget.SetBorderColor(border_rgb[0], border_rgb[1], border_rgb[2]);
            if (background_visible != 0) widget.BackgroundVisibilityOn();
            else widget.BackgroundVisibilityOff();
            if (border_visible != 0) widget.BorderVisibilityOn();
            else widget.BorderVisibilityOff();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_set_position(vtkn_scalar_bar bar, int32_t anchor, double x, double y)
    {
        return GuardVtk([&]() {
            Require(anchor >= VTKN_ANCHOR_BOTTOM_LEFT && anchor <= VTKN_ANCHOR_HORIZONTALLY_RELATIVE,
                    "The anchor is not one of vtkn_widget_anchor.");
            ScalarBar::FromHandle(bar)->Widget.SetPosition(static_cast<vtkn_widget_anchor>(anchor), x, y);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_get_rectangle(vtkn_scalar_bar bar, int32_t* rectangle4)
    {
        return GuardVtk([&]() {
            Require(rectangle4 != nullptr, "The argument 'rectangle' must not be null.");
            Rect rectangle = ScalarBar::FromHandle(bar)->Widget.GetRectangle();
            const int32_t values[4] = { rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height };
            std::copy(values, values + 4, rectangle4);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_get_value_label_rectangle(vtkn_scalar_bar bar, int32_t minimum,
                                                                    int32_t* rectangle4, int32_t* found)
    {
        return GuardVtk([&]() {
            Require(rectangle4 != nullptr && found != nullptr, "The out arguments must not be null.");
            Rect rectangle;
            *found = ScalarBar::FromHandle(bar)->Widget.GetValueLabelHighlightRectangle(minimum != 0, rectangle);
            const int32_t values[4] = { rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height };
            std::copy(values, values + 4, rectangle4);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_hit_value_label(vtkn_scalar_bar bar, int32_t x, int32_t y, int32_t* label,
                                                          int32_t* rectangle4)
    {
        return GuardVtk([&]() {
            Require(label != nullptr && rectangle4 != nullptr, "The out arguments must not be null.");
            Rect rectangle;
            *label = ScalarBar::FromHandle(bar)->Widget.HitValueLabel(x, y, rectangle);
            const int32_t values[4] = { rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height };
            std::copy(values, values + 4, rectangle4);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scalar_bar_get_highlight_rectangle(vtkn_scalar_bar bar, int32_t* rectangle4,
                                                                  int32_t* visible)
    {
        return GuardVtk([&]() {
            Require(rectangle4 != nullptr && visible != nullptr, "The out arguments must not be null.");
            Rect rectangle;
            *visible = ScalarBar::FromHandle(bar)->Widget.GetShownHighlight(rectangle) ? 1 : 0;
            const int32_t values[4] = { rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height };
            std::copy(values, values + 4, rectangle4);
            return VTKN_OK;
        });
    }
}
