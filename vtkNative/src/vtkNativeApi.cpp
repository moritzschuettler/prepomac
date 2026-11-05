// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The exported C functions. Each one only checks its arguments, finds its object and forwards; the guard turns
// every exception into a status and the last error, and fails the call on an error VTK reported meanwhile.
#include "vtkNative.h"
#include "vtkNativeError.h"
#include "vtkNativeOutputWindow.h"
#include "vtkNativeView.h"

#include <vtkVersion.h>

#include <vector>

using namespace vtkn;

namespace
{
    template <typename Function>
    vtkn_status GuardVtk(Function function)
    {
        return Guard([&]() {
            vtkNativeOutputWindow* outputWindow = vtkNativeOutputWindow::Install();
            outputWindow->TakeErrors();
            vtkn_status status = function();
            std::string errors = outputWindow->TakeErrors();
            if (status == VTKN_OK && !errors.empty()) throw Error(VTKN_ERROR_VTK, errors);
            return status;
        });
    }

    std::string ToName(const char* text, const char* argumentName)
    {
        if (text == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, std::string("The argument '") + argumentName +
                                                                       "' must not be null.");
        return std::string(text);
    }
}

extern "C"
{
    int32_t VTKN_CALL vtkn_get_api_version(void)
    {
        return VTKN_API_VERSION;
    }

    vtkn_status VTKN_CALL vtkn_get_vtk_version(char* buffer, int32_t capacity, int32_t* required)
    {
        return Guard([&]() { return CopyOut(vtkVersion::GetVTKVersion(), buffer, capacity, required); });
    }

    // Not guarded: the guard clears the very message this returns
    vtkn_status VTKN_CALL vtkn_get_last_error(char* buffer, int32_t capacity, int32_t* required)
    {
        try
        {
            if (required == nullptr) return VTKN_ERROR_INVALID_ARGUMENT;
            return CopyOut(GetLastError(), buffer, capacity, required);
        }
        catch (...)
        {
            return VTKN_ERROR_INTERNAL;
        }
    }

    vtkn_status VTKN_CALL vtkn_set_log_callback(vtkn_log_callback callback, void* user_data)
    {
        return Guard([&]() {
            vtkNativeOutputWindow::Install()->SetCallback(callback, user_data);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_create(int32_t off_screen, vtkn_view* view)
    {
        return GuardVtk([&]() {
            Require(view != nullptr, "The argument 'view' must not be null.");
            *view = nullptr;
            *view = new View(off_screen != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_destroy(vtkn_view view)
    {
        return GuardVtk([&]() {
            delete View::FromHandle(view);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_parent_window(vtkn_view view, void* parent_window)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetParentWindow(parent_window);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_size(vtkn_view view, int32_t width, int32_t height)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetSize(width, height);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_size(vtkn_view view, int32_t* width, int32_t* height)
    {
        return GuardVtk([&]() {
            Require(width != nullptr && height != nullptr, "The size arguments must not be null.");
            int w = 0;
            int h = 0;
            View::FromHandle(view)->GetSize(w, h);
            *width = w;
            *height = h;
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_event_callback(vtkn_view view, vtkn_event_callback callback,
                                                       void* user_data)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetEventCallback(callback, user_data);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_background(vtkn_view view, int32_t gradient, const double* top_rgb,
                                                   const double* bottom_rgb)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetBackground(gradient != 0, top_rgb, bottom_rgb);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_smoothing(vtkn_view view, int32_t point_smoothing, int32_t line_smoothing)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetSmoothing(point_smoothing != 0, line_smoothing != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_view_parameters(vtkn_view view, const double* values10)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetViewParameters(values10);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_standard_view(vtkn_view view, int32_t orientation, int32_t zoom_to_fit)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetStandardView(static_cast<vtkn_orientation>(orientation), zoom_to_fit != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_axis_view(vtkn_view view, const double* axis, int32_t zoom_to_fit)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetAxisView(axis, zoom_to_fit != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_vertical_axis_view(vtkn_view view, const double* axis, int32_t zoom_to_fit)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetVerticalAxisView(axis, zoom_to_fit != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_zoom_to_fit(vtkn_view view)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->ZoomToFit();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_zoom_factor(vtkn_view view, double parallel_scale)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetZoomFactor(parallel_scale);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_box_zoom(vtkn_view view, int32_t x1, int32_t y1, int32_t x2, int32_t y2)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->BoxZoom(x1, y1, x2, y2);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_part_actor(vtkn_view view, const char* name, const vtkn_part_style* style,
                                                   const vtkn_mesh* geometry, const vtkn_mesh* model_edges,
                                                   const vtkn_mesh* model_edges_to_draw)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            Require(geometry != nullptr, "The argument 'geometry' must not be null.");
            View::FromHandle(view)->AddPartActor(ToName(name, "name"), *style, *geometry, model_edges,
                                                 model_edges_to_draw);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_clear_actors(vtkn_view view)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->ClearActors();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_edges_visibility(vtkn_view view, int32_t edges_visibility)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetEdgesVisibility(static_cast<vtkn_edges_visibility>(edges_visibility));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_lighting(vtkn_view view, double ambient, double diffuse)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetLighting(ambient, diffuse);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_render(vtkn_view view)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->Render();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_render_info(vtkn_view view, char* buffer, int32_t capacity,
                                                    int32_t* required)
    {
        return GuardVtk([&]() { return CopyOut(View::FromHandle(view)->GetRenderInfo(), buffer, capacity, required); });
    }

    vtkn_status VTKN_CALL vtkn_view_get_camera(vtkn_view view, double* values10)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->GetCamera(values10);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_camera(vtkn_view view, const double* values10)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetCamera(values10);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_reset_camera(vtkn_view view)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->ResetCamera();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_mesh_actor(vtkn_view view, const char* name, const double* points,
                                                   int32_t point_count, const int32_t* cells, int32_t cells_length,
                                                   const uint8_t* cell_types, int32_t cell_count,
                                                   const double* color_rgb, int32_t edges_visible)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->AddMeshActor(ToName(name, "name"), points, point_count, cells, cells_length,
                                                 cell_types, cell_count, color_rgb, edges_visible != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_remove_actor(vtkn_view view, const char* name)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->RemoveActor(ToName(name, "name"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_actor_names(vtkn_view view, char* buffer, int32_t capacity,
                                                    int32_t* required)
    {
        return GuardVtk([&]() { return CopyOut(View::FromHandle(view)->GetActorNames(), buffer, capacity, required); });
    }

    vtkn_status VTKN_CALL vtkn_view_read_pixels(vtkn_view view, int32_t magnification, uint8_t* buffer,
                                                int32_t capacity, int32_t* width, int32_t* height)
    {
        return GuardVtk([&]() {
            Require(width != nullptr && height != nullptr, "The size arguments must not be null.");
            int w = 0;
            int h = 0;
            std::vector<uint8_t> pixels = View::FromHandle(view)->ReadPixels(magnification, w, h);
            *width = w;
            *height = h;
            if (buffer == nullptr || capacity < static_cast<int32_t>(pixels.size())) return VTKN_ERROR_BUFFER_TOO_SMALL;
            std::copy(pixels.begin(), pixels.end(), buffer);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_write_png(vtkn_view view, const char* file_name, int32_t magnification)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->WritePng(ToName(file_name, "file_name"), magnification);
            return VTKN_OK;
        });
    }

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
