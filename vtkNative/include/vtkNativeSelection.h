/* PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
 *
 * vtkNative, the selection area: picking, the screen to model step of a selection, the id resolution of the
 * picks and the selection layer. The conventions are those of vtkNativeCore.h.
 */
#ifndef VTKNATIVESELECTION_H
#define VTKNATIVESELECTION_H

#include "vtkNativeCore.h"
#include "vtkNativeGeometry.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Raised whenever this area changes incompatibly */
#define VTKN_SELECTION_API_VERSION 3

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
/* How a point pick finds its point. Exact (the default, slice C of the pilot): the ray of the pixel is intersected
   with the picked part, and where the pixel misses, a cell pick catches geometry within the selection distance.
   Not exact, as vtkControl picked with ActiViz: the point is rebuilt from the depth of the hardware pick and there is
   no pick within the distance, which VTK 5.8 made but never caught anything with */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_exact_picking(vtkn_view view, int32_t exact);
/* What a point pick on a section view resolves to (ResolveSectionViewPick): with part_selection a cut face resolves
   to its part; the faces with element ids (the element-wise section) also resolve to nodes and elements with
   node_or_element_selection */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_section_view_pick_mode(vtkn_view view, int32_t part_selection,
                                                                     int32_t node_or_element_selection);
/* An area pick from (x1, y1) to (x2, y2): the six planes of the frustum, each as origin and normal (36 values), and
   the parts rendered inside it as a string list */
VTKN_API vtkn_status VTKN_CALL vtkn_view_pick_area(vtkn_view view, int32_t x1, int32_t y1, int32_t x2, int32_t y2,
                                                    double* planes36, char* part_names, int32_t capacity,
                                                    int32_t* required);

/* The surface cell closest to a point: the point moved onto it, the element id, and the PrePoMax node ids and the
   coordinates of the cell's nodes (a triangle of the surface, a line or a vertex). node_count is 0 when no part
   was searched; element_id is -1 when the closest cell is beyond the distance, and the point is then not moved */
typedef struct vtkn_closest_cell
{
    double point[3];
    int32_t element_id;
    int32_t node_count;
    int32_t node_ids[8];
    double node_coordinates[24];
} vtkn_closest_cell;

/* How a selection highlight is drawn: the vtkProperty of the selection layer of vtkControl, colors as RGB in
   [0, 1]. element_edges draws the element edges of the surface; points draws the points of the mesh instead of its
   cells; on_geometry draws on the base layer, where the parts hide it, instead of over them; current marks the
   highlight of the mouse selection of the moment, which vtkn_view_clear_highlights can remove alone */
typedef struct vtkn_highlight_style
{
    double color[3];
    double ambient;
    double diffuse;
    double specular;
    double point_size;
    double line_width;
    int32_t element_edges;
    int32_t points;
    int32_t on_geometry;
    int32_t current;
} vtkn_highlight_style;

/* The id resolution of a selection. The surface cell closest to a point among the visible parts, or among the
   selectable_parts (a string list, or null); a non negative max_distance limits the distance, as for a pick by the
   user, a negative one takes the closest cell however far, as for a regeneration */
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_closest_cell(vtkn_view view, const double* point3, double max_distance,
                                                           const char* selectable_parts, vtkn_closest_cell* cell);
/* For each of the part_count parts of the string list part_names, visible or not, the distance from the point to
   its closest surface cell and the unit normal of that cell; the distance is -1 for a name that is no part and the
   normal is zero for a cell that has none */
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_closest_cells_on_parts(vtkn_view view, const double* point3,
                                                                     const char* part_names, int32_t part_count,
                                                                     double* distances, double* normals3);
/* The selection distance in pixels as a model length at the focal plane */
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_selection_precision(vtkn_view view, int32_t selection_distance,
                                                                  double* precision);
/* The bounds of a part's surface: x min, x max, y min, y max, z min, z max */
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_part_bounds(vtkn_view view, const char* name, double* bounds6);
/* The volume grid of a part with its node and element ids, which the frustum selection searches; null removes it */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_volume_grid(vtkn_view view, const char* name, const vtkn_mesh* grid);
/* The node ids of the part's volume grid inside the frustum and the element ids of its cells inside or crossing
   it. planes: plane_count planes as origin and normal. Buffers of the grid's point and cell counts always suffice;
   a smaller one returns VTKN_ERROR_BUFFER_TOO_SMALL with both counts set */
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_ids_inside_frustum(vtkn_view view, const char* name,
                                                                 const double* planes, int32_t plane_count,
                                                                 int32_t* point_ids, int32_t point_capacity,
                                                                 int32_t* point_count, int32_t* cell_ids,
                                                                 int32_t cell_capacity, int32_t* cell_count);

/* The selection layer, drawn over the parts. The highlight of a mesh: the outer faces of its volume cells, its
   surface cells, lines and points */
VTKN_API vtkn_status VTKN_CALL vtkn_view_add_highlight(vtkn_view view, const vtkn_mesh* mesh,
                                                        const vtkn_highlight_style* style);
/* The highlight of a whole part: its model edges, or its silhouette when it has none */
VTKN_API vtkn_status VTKN_CALL vtkn_view_add_part_highlight(vtkn_view view, const char* name,
                                                             const vtkn_highlight_style* style);
/* Removes the highlights, or only those marked current */
VTKN_API vtkn_status VTKN_CALL vtkn_view_clear_highlights(vtkn_view view, int32_t current_only);
/* A trial: the faces of the highlights and of the selection layer are not drawn in the highlight color; a glowing
   outline of that color is drawn around them (vtkOutlineGlowPass), of the given intensity (3 when not positive).
   Lines and points are drawn as before. Switching removes the highlights there are */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_highlight_glow(vtkn_view view, int32_t enabled, double intensity);

#ifdef __cplusplus
}
#endif

#endif
