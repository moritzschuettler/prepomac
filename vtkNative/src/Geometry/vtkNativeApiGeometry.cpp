// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The exported C functions of the geometry area. Each one only checks its arguments, finds its object and forwards,
// inside the guard of vtkNativeGuard.h.
#include "vtkNativeGuard.h"
#include "vtkNativeView.h"

#include <vector>

using namespace vtkn;

extern "C"
{
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
}
