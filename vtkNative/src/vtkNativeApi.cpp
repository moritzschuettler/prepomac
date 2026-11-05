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
}
