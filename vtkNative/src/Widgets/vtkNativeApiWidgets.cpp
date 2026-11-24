// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The exported C functions of the widgets area. Each one only checks its arguments, finds its object and forwards,
// inside the guard of vtkNativeGuard.h.
#include "vtkNativeGuard.h"
#include "vtkNativeArrowWidget.h"
#include "vtkNativeColorBarWidget.h"
#include "vtkNativeCoorSysWidget.h"
#include "vtkNativeScalarBar.h"
#include "vtkNativeScaleBarWidget.h"
#include "vtkNativeStatusBlockWidget.h"
#include "vtkNativeView.h"
#include "vtkNativeWidget.h"

#include <algorithm>
#include <cstring>
#include <vector>

using namespace vtkn;

namespace
{
    void CopyRectangle(const Rect& rectangle, int32_t* rectangle4)
    {
        const int32_t values[4] = { rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height };
        std::copy(values, values + 4, rectangle4);
    }

    std::vector<std::string> SplitList(const char* list, int32_t count)
    {
        std::vector<std::string> items;
        const char* item = list;
        for (int32_t i = 0; i < count; i++)
        {
            items.push_back(item);
            item += std::strlen(item) + 1;
        }
        return items;
    }

    std::string OptionalText(const char* text)
    {
        return text == nullptr ? std::string() : std::string(text);
    }
}

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

    vtkn_status VTKN_CALL vtkn_scalar_bar_get_value_label_text_origin(vtkn_scalar_bar bar, int32_t minimum,
                                                                      int32_t* point2, int32_t* found)
    {
        return GuardVtk([&]() {
            Require(point2 != nullptr && found != nullptr, "The out arguments must not be null.");
            int origin[2] = { 0, 0 };
            *found = ScalarBar::FromHandle(bar)->Widget.GetValueLabelTextOrigin(minimum != 0, origin) ? 1 : 0;
            point2[0] = origin[0];
            point2[1] = origin[1];
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

    vtkn_status VTKN_CALL vtkn_widget_create(vtkn_view view, int32_t kind, vtkn_widget* widget, int32_t* widget_id)
    {
        return GuardVtk([&]() {
            Require(widget != nullptr, "The argument 'widget' must not be null.");
            *widget = nullptr;
            Require(kind >= VTKN_WIDGET_TEXT && kind <= VTKN_WIDGET_TRIAD, "The kind is not one of vtkn_widget_kind.");
            Widget* created = View::FromHandle(view)->CreateWidget(static_cast<vtkn_widget_kind>(kind));
            *widget = created;
            if (widget_id != nullptr) *widget_id = created->Id;
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_destroy(vtkn_widget widget)
    {
        return GuardVtk([&]() {
            Widget* areaWidget = Widget::FromHandle(widget);
            areaWidget->Owner->DestroyWidget(areaWidget);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_set_text_style(vtkn_widget widget, const vtkn_text_style* style)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            Widget* areaWidget = Widget::FromHandle(widget);
            switch (areaWidget->Kind)
            {
                case VTKN_WIDGET_SCALE_BAR: areaWidget->As<ScaleBarWidget>(VTKN_WIDGET_SCALE_BAR).SetTextStyle(*style); break;
                case VTKN_WIDGET_TRIAD: areaWidget->As<CoorSysWidget>(VTKN_WIDGET_TRIAD).SetTextStyle(*style); break;
                default: static_cast<TextWidget&>(*areaWidget->Content).SetTextStyle(*style); break;
            }
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_text_style_get_line_pitch(const vtkn_text_style* style, double* line_pitch)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            Require(line_pitch != nullptr, "The argument 'line_pitch' must not be null.");
            *line_pitch = TextWidget::GetLinePitch(*style);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_get_text_origin(vtkn_widget widget, int32_t* point2, int32_t* found)
    {
        return GuardVtk([&]() {
            Require(point2 != nullptr && found != nullptr, "The out arguments must not be null.");
            Widget* areaWidget = Widget::FromHandle(widget);
            int origin[2] = { 0, 0 };
            bool textWidget = areaWidget->Kind != VTKN_WIDGET_SCALE_BAR && areaWidget->Kind != VTKN_WIDGET_TRIAD;
            *found = textWidget && static_cast<TextWidget&>(*areaWidget->Content).GetTextOrigin(origin) ? 1 : 0;
            point2[0] = origin[0];
            point2[1] = origin[1];
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_set_padding(vtkn_widget widget, int32_t padding)
    {
        return GuardVtk([&]() {
            Require(padding >= 0, "The padding must not be negative.");
            Widget* areaWidget = Widget::FromHandle(widget);
            if (areaWidget->Kind == VTKN_WIDGET_SCALE_BAR)
                areaWidget->As<ScaleBarWidget>(VTKN_WIDGET_SCALE_BAR).SetPadding(padding);
            else
            {
                Require(areaWidget->Kind != VTKN_WIDGET_TRIAD, "The triad has no padding.");
                static_cast<TextWidget&>(*areaWidget->Content).SetPadding(padding);
            }
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_set_visibility(vtkn_widget widget, int32_t visible)
    {
        return GuardVtk([&]() {
            BorderWidget& content = *Widget::FromHandle(widget)->Content;
            if (visible != 0) content.VisibilityOn();
            else content.VisibilityOff();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_set_frame(vtkn_widget widget, int32_t background_visible,
                                                const double* background_rgb, int32_t border_visible,
                                                const double* border_rgb)
    {
        return GuardVtk([&]() {
            Require(background_rgb != nullptr && border_rgb != nullptr, "The frame colors must not be null.");
            BorderWidget& content = *Widget::FromHandle(widget)->Content;
            content.SetBackgroundColor(background_rgb[0], background_rgb[1], background_rgb[2]);
            content.SetBorderColor(border_rgb[0], border_rgb[1], border_rgb[2]);
            if (background_visible != 0) content.BackgroundVisibilityOn();
            else content.BackgroundVisibilityOff();
            if (border_visible != 0) content.BorderVisibilityOn();
            else content.BorderVisibilityOff();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_set_position(vtkn_widget widget, int32_t anchor, double x, double y)
    {
        return GuardVtk([&]() {
            Require(anchor >= VTKN_ANCHOR_BOTTOM_LEFT && anchor <= VTKN_ANCHOR_HORIZONTALLY_RELATIVE,
                    "The anchor is not one of vtkn_widget_anchor.");
            Widget::FromHandle(widget)->Content->SetPosition(static_cast<vtkn_widget_anchor>(anchor), x, y);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_get_geometry(vtkn_widget widget, double* position_size4, int32_t* visible)
    {
        return GuardVtk([&]() {
            Require(position_size4 != nullptr && visible != nullptr, "The out arguments must not be null.");
            BorderWidget& content = *Widget::FromHandle(widget)->Content;
            content.GetPositionAndSize(position_size4);
            *visible = content.GetVisibility() ? 1 : 0;
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_get_rectangle(vtkn_widget widget, int32_t* rectangle4)
    {
        return GuardVtk([&]() {
            Require(rectangle4 != nullptr, "The argument 'rectangle' must not be null.");
            CopyRectangle(Widget::FromHandle(widget)->Content->GetRectangle(), rectangle4);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_get_highlight(vtkn_widget widget, double* corners8, int32_t* visible)
    {
        return GuardVtk([&]() {
            Require(corners8 != nullptr && visible != nullptr, "The out arguments must not be null.");
            double corners[4][2];
            *visible = Widget::FromHandle(widget)->Content->GetShownHighlightCorners(corners) ? 1 : 0;
            for (int i = 0; i < 4; i++)
            {
                corners8[2 * i] = corners[i][0];
                corners8[2 * i + 1] = corners[i][1];
            }
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_widget_set_text(vtkn_widget widget, const char* text)
    {
        return GuardVtk([&]() {
            Widget* areaWidget = Widget::FromHandle(widget);
            Require(areaWidget->Kind != VTKN_WIDGET_SCALE_BAR && areaWidget->Kind != VTKN_WIDGET_TRIAD,
                    "The widget has no text.");
            static_cast<TextWidget&>(*areaWidget->Content).SetText(ToName(text, "text"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_status_block_set_lines(vtkn_widget widget, const char* line1, const char* line2,
                                                      const char* line3_fixed, const char* line3_factor,
                                                      const char* line4)
    {
        return GuardVtk([&]() {
            Widget::FromHandle(widget)->As<StatusBlockWidget>(VTKN_WIDGET_STATUS_BLOCK)
                .SetLines(ToName(line1, "line1"), ToName(line2, "line2"), ToName(line3_fixed, "line3_fixed"),
                          ToName(line3_factor, "line3_factor"), OptionalText(line4));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_status_block_get_scale_factor_hit(vtkn_widget widget, int32_t* clicked,
                                                                 int32_t* rectangle4)
    {
        return GuardVtk([&]() {
            Require(clicked != nullptr && rectangle4 != nullptr, "The out arguments must not be null.");
            StatusBlockWidget& block = Widget::FromHandle(widget)->As<StatusBlockWidget>(VTKN_WIDGET_STATUS_BLOCK);
            *clicked = block.GetDeformationScaleFactorTextClicked() ? 1 : 0;
            CopyRectangle(block.GetDeformationScaleFactorRectangle(), rectangle4);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scale_bar_set_width(vtkn_widget widget, int32_t width)
    {
        return GuardVtk([&]() {
            Require(width > 0, "The width must be positive.");
            Widget::FromHandle(widget)->As<ScaleBarWidget>(VTKN_WIDGET_SCALE_BAR).SetWidth(width);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scale_bar_set_unit(vtkn_widget widget, const char* unit)
    {
        return GuardVtk([&]() {
            Widget::FromHandle(widget)->As<ScaleBarWidget>(VTKN_WIDGET_SCALE_BAR).SetUnit(ToName(unit, "unit"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scale_bar_set_number_formatter(vtkn_widget widget, vtkn_number_formatter formatter,
                                                              void* user_data)
    {
        return GuardVtk([&]() {
            ScaleBarWidget& bar = Widget::FromHandle(widget)->As<ScaleBarWidget>(VTKN_WIDGET_SCALE_BAR);
            if (formatter == nullptr)
            {
                bar.SetNumberFormatter(nullptr);
                return VTKN_OK;
            }
            bar.SetNumberFormatter([formatter, user_data](double value) {
                std::vector<char> buffer(64);
                int32_t required = formatter(user_data, value, buffer.data(), static_cast<int32_t>(buffer.size()));
                if (required > static_cast<int32_t>(buffer.size()))
                {
                    buffer.resize(required);
                    formatter(user_data, value, buffer.data(), static_cast<int32_t>(buffer.size()));
                }
                buffer.back() = 0;
                return std::string(buffer.data());
            });
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_scale_bar_get_labels(vtkn_widget widget, char* buffer, int32_t buffer_size,
                                                    int32_t* required_size)
    {
        return GuardVtk([&]() {
            Require(required_size != nullptr, "The argument 'required_size' must not be null.");
            std::string list;
            for (const std::string& label : Widget::FromHandle(widget)->As<ScaleBarWidget>(VTKN_WIDGET_SCALE_BAR)
                                                .GetLabels())
            {
                list += label;
                list.push_back('\0');
            }
            list.push_back('\0');
            *required_size = static_cast<int32_t>(list.size());
            if (buffer != nullptr && buffer_size >= *required_size) std::copy(list.begin(), list.end(), buffer);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_color_bar_set_colors_and_labels(vtkn_widget widget, const double* colors_rgb,
                                                               const char* labels, int32_t count)
    {
        return GuardVtk([&]() {
            Require(count >= 0, "The count must not be negative.");
            Require(count == 0 || (colors_rgb != nullptr && labels != nullptr), "The colors and labels must not be null.");
            static const double noColor[3] = { 0, 0, 0 };
            Widget::FromHandle(widget)->As<ColorBarWidget>(VTKN_WIDGET_COLOR_BAR)
                .SetColorsAndLabels(count == 0 ? noColor : colors_rgb, SplitList(labels, count));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_color_bar_get_title_rectangle(vtkn_widget widget, int32_t* rectangle4, int32_t* found)
    {
        return GuardVtk([&]() {
            Require(rectangle4 != nullptr && found != nullptr, "The out arguments must not be null.");
            Rect rectangle;
            *found = Widget::FromHandle(widget)->As<ColorBarWidget>(VTKN_WIDGET_COLOR_BAR)
                         .GetTitleRectangle(rectangle) ? 1 : 0;
            CopyRectangle(rectangle, rectangle4);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_arrow_set_anchor_point(vtkn_widget widget, double x, double y, double z)
    {
        return GuardVtk([&]() {
            Widget::FromHandle(widget)->As<ArrowWidget>(VTKN_WIDGET_TEXT_WITH_ARROW).SetAnchorPoint(x, y, z);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_arrow_reset_position_from_anchor(vtkn_widget widget)
    {
        return GuardVtk([&]() {
            Widget::FromHandle(widget)->As<ArrowWidget>(VTKN_WIDGET_TEXT_WITH_ARROW).ResetPositionFromAnchor();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_arrow_reset_initial_position(vtkn_widget widget)
    {
        return GuardVtk([&]() {
            Widget::FromHandle(widget)->As<ArrowWidget>(VTKN_WIDGET_TEXT_WITH_ARROW).ResetInitialPosition();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_arrow_offset_position(vtkn_widget widget, double dx, double dy)
    {
        return GuardVtk([&]() {
            Widget::FromHandle(widget)->As<ArrowWidget>(VTKN_WIDGET_TEXT_WITH_ARROW).OffsetPosition(dx, dy);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_arrow_get_leader_line(vtkn_widget widget, double* line4)
    {
        return GuardVtk([&]() {
            Require(line4 != nullptr, "The argument 'line' must not be null.");
            Widget::FromHandle(widget)->As<ArrowWidget>(VTKN_WIDGET_TEXT_WITH_ARROW).GetLeaderLine(line4);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_triad_get_target(vtkn_widget widget, int32_t x, int32_t y, int32_t* target)
    {
        return GuardVtk([&]() {
            Require(target != nullptr, "The argument 'target' must not be null.");
            *target = Widget::FromHandle(widget)->As<CoorSysWidget>(VTKN_WIDGET_TRIAD).GetTargetAtPosition(x, y);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_triad_update_geometry(vtkn_widget widget)
    {
        return GuardVtk([&]() {
            Widget::FromHandle(widget)->As<CoorSysWidget>(VTKN_WIDGET_TRIAD).UpdateGeometry();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_widget_highlight(vtkn_view view, int32_t enabled,
                                                         int32_t local_highlight_enabled)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetWidgetHighlight(enabled != 0, local_highlight_enabled != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_interaction_settings(vtkn_view view, const vtkn_interaction_settings* settings)
    {
        return GuardVtk([&]() {
            Require(settings != nullptr, "The argument 'settings' must not be null.");
            View::FromHandle(view)->GetInteractorStyle()->SetSettings(*settings);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_handle_input(vtkn_view view, int32_t input_event, int32_t x, int32_t y,
                                                 int32_t ctrl, int32_t shift, int32_t key_code, const char* key_sym)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->HandleInput(input_event, x, y, ctrl != 0, shift != 0, key_code, key_sym);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_navigate(vtkn_view view, int32_t navigation, double a, double b, double c)
    {
        return GuardVtk([&]() {
            InteractorStyle* style = View::FromHandle(view)->GetInteractorStyle();
            switch (navigation)
            {
                case VTKN_NAVIGATE_ROTATE_BY_DISPLAY_DELTA: style->RotateByDisplayDelta(a, b, true); break;
                case VTKN_NAVIGATE_PAN_BY_DISPLAY_DELTA: style->PanByDisplayDelta(a, b, true); break;
                case VTKN_NAVIGATE_ZOOM_AT_POINT:
                    style->ZoomAtDisplayPoint(a, static_cast<int>(b), static_cast<int>(c), true);
                    break;
                case VTKN_NAVIGATE_ZOOM_AT_POINT_SMOOTH:
                    style->ZoomAtDisplayPointSmooth(a, static_cast<int>(b), static_cast<int>(c));
                    break;
                case VTKN_NAVIGATE_ROTATE:
                    style->UpdateRotationCenterDisplay();
                    style->Rotate(a, b, c, true);
                    break;
                case VTKN_NAVIGATE_START_GESTURE:
                    style->StartMultiTouchGesture(static_cast<int>(a), static_cast<int>(b), c != 0);
                    break;
                case VTKN_NAVIGATE_END_GESTURE: style->EndMultiTouchGesture(); break;
                case VTKN_NAVIGATE_FINISH_SMOOTH_ZOOM: style->FinishSmoothZoom(); break;
                case VTKN_NAVIGATE_STEP_SMOOTH_ZOOM: style->StepSmoothZoom(); break;
                case VTKN_NAVIGATE_RESET_ROTATION_CENTER: style->ResetRotationCenter(); break;
                case VTKN_NAVIGATE_FIRE_SELECTION_TIMER: style->FireSelectionTimer(); break;
                case VTKN_NAVIGATE_SET_BOX_ZOOM: style->SetBoxZoomEnabled(a != 0); break;
                case VTKN_NAVIGATE_SET_ANIMATING: style->SetAnimating(a != 0); break;
                case VTKN_NAVIGATE_SET_OWNER_BOX_ZOOM: style->SetOwnerBoxZoom(a != 0); break;
                default: throw Error(VTKN_ERROR_INVALID_ARGUMENT, "The navigation is not one of vtkn_navigation.");
            }
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_rotation_point(vtkn_view view, const double* point3)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->GetInteractorStyle()->SetRotationPoint(point3);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_interaction_state(vtkn_view view, int32_t* state4)
    {
        return GuardVtk([&]() {
            Require(state4 != nullptr, "The argument 'state' must not be null.");
            InteractorStyle* style = View::FromHandle(view)->GetInteractorStyle();
            state4[0] = style->GetState();
            state4[1] = style->IsBoxSelectionActive() ? 1 : 0;
            state4[2] = style->GetBoxZoomEnabled() ? 1 : 0;
            state4[3] = style->HasSmoothZoomPending() ? 1 : 0;
            return VTKN_OK;
        });
    }
}
