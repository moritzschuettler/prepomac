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
#define VTKN_GEOMETRY_API_VERSION 2

/* Mirrors vtkRendererLayer of vtkControl: the base layer, the selection layer drawn over it, the overlay layer */
typedef enum vtkn_layer
{
    VTKN_LAYER_BASE = 0,
    VTKN_LAYER_SELECTION = 1,
    VTKN_LAYER_OVERLAY = 2
} vtkn_layer;

/* Mirrors vtkMaxActorRepresentation */
typedef enum vtkn_actor_representation
{
    VTKN_REPRESENTATION_SOLID = 0,
    VTKN_REPRESENTATION_SOLID_AS_SHELL = 1,
    VTKN_REPRESENTATION_SHELL = 2,
    VTKN_REPRESENTATION_WIRE = 3,
    VTKN_REPRESENTATION_UNKNOWN = 4
} vtkn_actor_representation;

/* What vtkn_view_clear removes: Clear, ClearButKeepParts, ClearSelection and ClearOverlay of vtkControl */
typedef enum vtkn_clear
{
    VTKN_CLEAR_ALL = 0,
    VTKN_CLEAR_BUT_KEEP_PARTS = 1,
    VTKN_CLEAR_SELECTION = 2,
    VTKN_CLEAR_OVERLAY = 3
} vtkn_clear;

/* Mirrors vtkEdgesVisibility of vtkControl */
typedef enum vtkn_edges_visibility
{
    VTKN_EDGES_WIREFRAME = 0,
    VTKN_EDGES_ELEMENT_EDGES = 1,
    VTKN_EDGES_MODEL_EDGES = 2,
    VTKN_EDGES_NONE = 3
} vtkn_edges_visibility;

/* The symbols of the AddOriented* methods and AddSphereActor of vtkControl */
typedef enum vtkn_symbol
{
    VTKN_SYMBOL_SPHERE = 0,
    VTKN_SYMBOL_DISPLACEMENT_CONE = 1,
    VTKN_SYMBOL_CONE = 2,
    VTKN_SYMBOL_ROTATIONAL_CONSTRAINT = 3,
    VTKN_SYMBOL_JOINT = 4,
    VTKN_SYMBOL_ARROW = 5,
    VTKN_SYMBOL_DOUBLE_ARROW = 6,
    VTKN_SYMBOL_SPRING = 7,
    VTKN_SYMBOL_THERMO = 8,
    VTKN_SYMBOL_FLUX = 9
} vtkn_symbol;

/* Mirrors vtkJointSymbolEnum */
typedef enum vtkn_joint
{
    VTKN_JOINT_HINGE = 0,
    VTKN_JOINT_CYLINDRICAL = 1,
    VTKN_JOINT_SLIDER = 2,
    VTKN_JOINT_BALL = 3,
    VTKN_JOINT_WELD = 4
} vtkn_joint;

/* How a symbol is drawn: its kind (vtkn_symbol), the joint (vtkn_joint) of a joint symbol, the screen size in pixels,
   the relative size of an arrow, whether it points into its point (invert) or is centered on it (center, a flux),
   and whether its silhouette edges are drawn as their own actor (the name followed by ":silhouette") */
typedef struct vtkn_symbol_parameters
{
    int32_t kind;
    int32_t joint;
    double symbol_size;
    double relative_size;
    int32_t invert;
    int32_t center;
    int32_t draw_edges;
} vtkn_symbol_parameters;

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
    /* vtkn_layer */
    int32_t layer;
    /* vtkn_actor_representation */
    int32_t representation;
    int32_t is_a_part;
    int32_t draw_on_geometry;
    int32_t use_secondary_highlight_color;
    int32_t keep_own_color;
    int32_t color_contours;
    int32_t section_view_possible;
    /* The actor is the points of the geometry as vertices, as vtkControl.AddPoints draws them */
    int32_t nodal;
} vtkn_part_style;

/* Parts, the port of the part path of vtkMaxActor and AddCells. Adding a name that exists replaces that part.
   model_edges and model_edges_to_draw may be null */
VTKN_API vtkn_status VTKN_CALL vtkn_view_add_part_actor(vtkn_view view, const char* name,
                                                         const vtkn_part_style* style, const vtkn_mesh* geometry,
                                                         const vtkn_mesh* model_edges,
                                                         const vtkn_mesh* model_edges_to_draw);
/* Any actor of vtkMaxActorData, in the layer its style names: AddCells and AddPoints of vtkControl. A base or overlay
   actor replaces the one of the same name; a selection actor is added to the others. parent_name may be null */
VTKN_API vtkn_status VTKN_CALL vtkn_view_add_actor(vtkn_view view, const char* name, const char* parent_name,
                                                    const vtkn_part_style* style, const vtkn_mesh* geometry,
                                                    const vtkn_mesh* model_edges, const vtkn_mesh* model_edges_to_draw);
/* The visibility of an existing actor of a layer, as setting vtkMaxActor.VtkMaxActorVisible */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_actor_visible(vtkn_view view, const char* name, int32_t layer,
                                                            int32_t visible);
/* HighlightActor: a copy of the model edges of a base actor, or its silhouette, in the selection layer */
VTKN_API vtkn_status VTKN_CALL vtkn_view_highlight_actor(vtkn_view view, const char* name);
/* HideActors and ShowActors; names is a string list: every name followed by a zero byte, and one more zero byte */
VTKN_API vtkn_status VTKN_CALL vtkn_view_hide_show_actors(vtkn_view view, const char* names, int32_t visible);
VTKN_API vtkn_status VTKN_CALL vtkn_view_update_result_representation_visibility(vtkn_view view,
                                                                                  const char* part_names,
                                                                                  int32_t visible, int32_t update);
/* A symbol glyphed onto points at a constant screen size, oriented along the point normals: point_count * 3
   coordinates and normal_count * 3 normal components, normals null for a symbol without orientation */
VTKN_API vtkn_status VTKN_CALL vtkn_view_add_symbol_actor(vtkn_view view, const char* name,
                                                           const vtkn_part_style* style, const double* points,
                                                           int32_t point_count, const double* normals,
                                                           int32_t normal_count,
                                                           const vtkn_symbol_parameters* parameters);
/* vtkn_clear */
VTKN_API vtkn_status VTKN_CALL vtkn_view_clear(vtkn_view view, int32_t what);
VTKN_API vtkn_status VTKN_CALL vtkn_view_remove_overlay_actors(vtkn_view view, const char* names);
/* The primary and secondary highlight colors, RGB in [0, 1] */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_highlight_colors(vtkn_view view, const double* primary_rgb,
                                                               const double* secondary_rgb);
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
