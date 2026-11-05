/* PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
 *
 * vtkNative, the geometry area: parts and mesh actors, the port of vtkMaxActor and the geometry regions of
 * vtkControl. The conventions are those of vtkNativeCore.h.
 */
#ifndef VTKNATIVEGEOMETRY_H
#define VTKNATIVEGEOMETRY_H

#include "vtkNativeCore.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Raised whenever this area changes incompatibly */
#define VTKN_GEOMETRY_API_VERSION 1

/* Mirrors vtkEdgesVisibility of vtkControl */
typedef enum vtkn_edges_visibility
{
    VTKN_EDGES_WIREFRAME = 0,
    VTKN_EDGES_ELEMENT_EDGES = 1,
    VTKN_EDGES_MODEL_EDGES = 2,
    VTKN_EDGES_NONE = 3
} vtkn_edges_visibility;

/* A mesh as arrays the caller owns for the duration of the call. coordinates: point_count * 3. cells: for each
   cell its node count followed by its point indices (the VTK legacy layout), cells_length values in total.
   cell_types: cell_count VTK cell type codes. point_ids and cell_ids: the PrePoMax node and element ids, or null */
typedef struct vtkn_mesh
{
    const double* coordinates;
    int32_t point_count;
    const int32_t* cells;
    int32_t cells_length;
    const uint8_t* cell_types;
    int32_t cell_count;
    const int32_t* point_ids;
    const int32_t* cell_ids;
} vtkn_mesh;

/* How a part is drawn: the drawing fields of vtkMaxActorData. Colors are RGBA in [0, 1] */
typedef struct vtkn_part_style
{
    double color[4];
    double backface_color[4];
    double ambient;
    double diffuse;
    double line_width;
    double node_size;
    int32_t has_backface_color;
    int32_t can_have_element_edges;
    int32_t backface_culling;
    int32_t lighting;
    int32_t use_global_lighting;
    int32_t use_edge_visibility;
    int32_t pickable;
    int32_t visible;
} vtkn_part_style;

/* Parts, the port of the part path of vtkMaxActor and AddCells. Adding a name that exists replaces that part.
   model_edges and model_edges_to_draw may be null */
VTKN_API vtkn_status VTKN_CALL vtkn_view_add_part_actor(vtkn_view view, const char* name,
                                                         const vtkn_part_style* style, const vtkn_mesh* geometry,
                                                         const vtkn_mesh* model_edges,
                                                         const vtkn_mesh* model_edges_to_draw);
VTKN_API vtkn_status VTKN_CALL vtkn_view_clear_actors(vtkn_view view);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_edges_visibility(vtkn_view view, int32_t edges_visibility);
/* The ambient and diffuse light of the parts that use the global lighting */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_lighting(vtkn_view view, double ambient, double diffuse);

/* Actors are named; adding a name that exists replaces that actor.
   points: point_count * 3 coordinates. cells: for each cell its node count followed by its point indices
   (the VTK legacy layout), cells_length values in total. cell_types: cell_count VTK cell type codes. */
VTKN_API vtkn_status VTKN_CALL vtkn_view_add_mesh_actor(vtkn_view view, const char* name, const double* points,
                                                         int32_t point_count, const int32_t* cells,
                                                         int32_t cells_length, const uint8_t* cell_types,
                                                         int32_t cell_count, const double* color_rgb,
                                                         int32_t edges_visible);
VTKN_API vtkn_status VTKN_CALL vtkn_view_remove_actor(vtkn_view view, const char* name);
/* A string list: every name followed by a zero byte, and one more zero byte after the last */
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_actor_names(vtkn_view view, char* buffer, int32_t capacity,
                                                          int32_t* required);

#ifdef __cplusplus
}
#endif

#endif
