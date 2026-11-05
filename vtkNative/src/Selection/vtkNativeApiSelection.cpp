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
}
