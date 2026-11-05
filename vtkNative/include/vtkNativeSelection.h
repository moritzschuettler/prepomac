/* PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
 *
 * vtkNative, the selection area: picking, the screen to model step of a selection. The conventions are those
 * of vtkNativeCore.h.
 */
#ifndef VTKNATIVESELECTION_H
#define VTKNATIVESELECTION_H

#include "vtkNativeCore.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Raised whenever this area changes incompatibly */
#define VTKN_SELECTION_API_VERSION 1

/* A point pick. hit is 0 when nothing was picked; the point is then not set. cell_id is the element id of the
   surface cell the point lies on */
typedef struct vtkn_point_pick
{
    double point[3];
    double direction[3];
    int32_t hit;
    int32_t cell_id;
} vtkn_point_pick;

/* Picking, the screen to model step of a selection. A point pick names the part under a display position and the
   point on its surface: the hardware pick of the pickable parts, drawn opaque, and else a cell pick within
   selection_distance pixels; the depth point is moved onto the closest surface cell. With limit_distance the point
   must lie within the selection distance (as a model length) of a surface, as for a pick by the user; without it
   the closest surface is taken, as for a regeneration. selectable_parts: a string list of the parts that may be
   picked, or null for all. direction is the direction of projection of the camera, set also without a hit.
   part_name follows the buffer convention; the pick is made again when the buffer was too small */
VTKN_API vtkn_status VTKN_CALL vtkn_view_pick_point(vtkn_view view, int32_t x, int32_t y, int32_t selection_distance,
                                                     int32_t limit_distance, const char* selectable_parts,
                                                     vtkn_point_pick* pick, char* part_name, int32_t capacity,
                                                     int32_t* required);
/* An area pick from (x1, y1) to (x2, y2): the six planes of the frustum, each as origin and normal (36 values), and
   the parts rendered inside it as a string list */
VTKN_API vtkn_status VTKN_CALL vtkn_view_pick_area(vtkn_view view, int32_t x1, int32_t y1, int32_t x2, int32_t y2,
                                                    double* planes36, char* part_names, int32_t capacity,
                                                    int32_t* required);

#ifdef __cplusplus
}
#endif

#endif
