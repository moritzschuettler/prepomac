// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The exported C functions of the selection area. Each one only checks its arguments, finds its object and forwards,
// inside the guard of vtkNativeGuard.h.
#include "vtkNativeGuard.h"
#include "vtkNativeView.h"

#include <cstring>
#include <set>
#include <string>

using namespace vtkn;

namespace
{
    std::set<std::string> ToNameSet(const char* list)
    {
        std::set<std::string> names;
        if (list == nullptr) return names;
        for (const char* name = list; *name != '\0'; name += std::strlen(name) + 1) names.insert(name);
        return names;
    }
}

extern "C"
{
    vtkn_status VTKN_CALL vtkn_view_pick_point(vtkn_view view, int32_t x, int32_t y, int32_t selection_distance,
                                               int32_t limit_distance, const char* selectable_parts,
                                               vtkn_point_pick* pick, char* part_name, int32_t capacity,
                                               int32_t* required)
    {
        return GuardVtk([&]() {
            Require(pick != nullptr, "The argument 'pick' must not be null.");
            std::set<std::string> selectable;
            if (selectable_parts != nullptr)
            {
                for (const char* name = selectable_parts; *name != '\0'; name += std::strlen(name) + 1)
                    selectable.insert(name);
            }
            std::string name;
            int cellId = -1;
            bool hit = View::FromHandle(view)->PickPoint(x, y, selection_distance, limit_distance != 0,
                                                         selectable_parts != nullptr ? &selectable : nullptr,
                                                         pick->point, pick->direction, name, cellId);
            pick->hit = hit ? 1 : 0;
            pick->cell_id = cellId;
            return CopyOut(name, part_name, capacity, required);
        });
    }

    vtkn_status VTKN_CALL vtkn_view_pick_area(vtkn_view view, int32_t x1, int32_t y1, int32_t x2, int32_t y2,
                                              double* planes36, char* part_names, int32_t capacity,
                                              int32_t* required)
    {
        return GuardVtk([&]() {
            Require(planes36 != nullptr, "The argument 'planes' must not be null.");
            std::vector<std::string> names = View::FromHandle(view)->PickArea(x1, y1, x2, y2, planes36);
            std::string list;
            for (const std::string& name : names)
            {
                list += name;
                list.push_back('\0');
            }
            return CopyOut(list, part_names, capacity, required);
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_section_view_pick_mode(vtkn_view view, int32_t part_selection,
                                                               int32_t node_or_element_selection)
    {
        return Guard([&]() {
            View::FromHandle(view)->SetSectionViewPickMode(part_selection != 0, node_or_element_selection != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_closest_cell(vtkn_view view, const double* point3, double max_distance,
                                                     const char* selectable_parts, vtkn_closest_cell* cell)
    {
        return GuardVtk([&]() {
            Require(point3 != nullptr, "The argument 'point' must not be null.");
            Require(cell != nullptr, "The argument 'cell' must not be null.");
            std::set<std::string> selectable = ToNameSet(selectable_parts);
            double point[3] = { point3[0], point3[1], point3[2] };
            View::FromHandle(view)->GetClosestCell(point, max_distance,
                                                   selectable_parts != nullptr ? &selectable : nullptr, *cell);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_closest_cells_on_parts(vtkn_view view, const double* point3,
                                                               const char* part_names, int32_t part_count,
                                                               double* distances, double* normals3)
    {
        return GuardVtk([&]() {
            Require(point3 != nullptr, "The argument 'point' must not be null.");
            Require(part_count >= 0, "The part count must not be negative.");
            Require(part_count == 0 || (part_names != nullptr && distances != nullptr && normals3 != nullptr),
                    "The part names, distances and normals must not be null.");
            std::vector<std::string> names;
            const char* name = part_names;
            for (int i = 0; i < part_count; i++, name += std::strlen(name) + 1) names.push_back(name);
            View::FromHandle(view)->GetClosestCellsOnParts(point3, names, distances, normals3);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_selection_precision(vtkn_view view, int32_t selection_distance,
                                                            double* precision)
    {
        return GuardVtk([&]() {
            Require(precision != nullptr, "The argument 'precision' must not be null.");
            *precision = View::FromHandle(view)->GetSelectionPrecisionInModel(selection_distance);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_part_bounds(vtkn_view view, const char* name, double* bounds6)
    {
        return GuardVtk([&]() {
            Require(bounds6 != nullptr, "The argument 'bounds' must not be null.");
            View::FromHandle(view)->GetPartBounds(ToName(name, "name"), bounds6);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_smooth_part_to_stl(vtkn_view view, const char* name, const char* file_name)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SmoothPartToStl(ToName(name, "name"), ToName(file_name, "file_name"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_volume_grid(vtkn_view view, const char* name, const vtkn_mesh* grid)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetVolumeGrid(ToName(name, "name"), grid);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_ids_inside_frustum(vtkn_view view, const char* name, const double* planes,
                                                           int32_t plane_count, int32_t* point_ids,
                                                           int32_t point_capacity, int32_t* point_count,
                                                           int32_t* cell_ids, int32_t cell_capacity,
                                                           int32_t* cell_count)
    {
        return GuardVtk([&]() {
            Require(planes != nullptr, "The argument 'planes' must not be null.");
            Require(point_count != nullptr && cell_count != nullptr, "The counts must not be null.");
            std::vector<int> pointIds;
            std::vector<int> cellIds;
            View::FromHandle(view)->GetIdsInsideFrustum(ToName(name, "name"), planes, plane_count, pointIds, cellIds);
            *point_count = static_cast<int32_t>(pointIds.size());
            *cell_count = static_cast<int32_t>(cellIds.size());
            if (point_ids == nullptr || cell_ids == nullptr || point_capacity < *point_count ||
                cell_capacity < *cell_count) return VTKN_ERROR_BUFFER_TOO_SMALL;
            std::copy(pointIds.begin(), pointIds.end(), point_ids);
            std::copy(cellIds.begin(), cellIds.end(), cell_ids);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_highlight(vtkn_view view, const vtkn_mesh* mesh,
                                                  const vtkn_highlight_style* style)
    {
        return GuardVtk([&]() {
            Require(mesh != nullptr, "The argument 'mesh' must not be null.");
            Require(style != nullptr, "The argument 'style' must not be null.");
            View::FromHandleOfActors(view)->AddHighlight(*mesh, *style);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_part_highlight(vtkn_view view, const char* name,
                                                       const vtkn_highlight_style* style)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            View::FromHandleOfActors(view)->AddPartHighlight(ToName(name, "name"), *style);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_clear_highlights(vtkn_view view, int32_t current_only)
    {
        return GuardVtk([&]() {
            View::FromHandleOfActors(view)->ClearHighlights(current_only != 0);
            return VTKN_OK;
        });
    }
}
