/* PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
 *
 * vtkNative: the flat C API between PrePoMax and VTK 9.7.
 *
 * Conventions
 * - Every function returns a vtkn_status. VTKN_OK is 0; anything else is an error whose message is read with
 *   vtkn_get_last_error on the same thread. No C++ exception crosses the API.
 * - Objects are opaque handles created by vtkn_*_create and destroyed by vtkn_*_destroy. A handle is used on the
 *   thread that created it (the user interface thread for a view embedded in a window); the library does not lock.
 * - Arrays in are a pointer and an element count, owned by the caller and copied before the call returns.
 * - Arrays and strings out are written into a caller buffer. The call reports the required size in an out
 *   argument; a buffer that is too small (or null) returns VTKN_ERROR_BUFFER_TOO_SMALL with the size set, so the
 *   caller asks once, allocates, and asks again.
 * - Strings are UTF-8 and zero terminated, in both directions, file names included.
 * - Callbacks are plain function pointers with a user data pointer. A string or array passed to a callback is
 *   valid only during the callback. A callback may call the library, but must not destroy the view that raised
 *   it. Events of an embedded view are raised on the thread that pumps the parent window's messages.
 * - An error VTK reports through its output window during a call fails that call with VTKN_ERROR_VTK.
 * - Ids and counts are int32; coordinates and colors are double; colors are RGB in [0, 1].
 */
#ifndef VTKNATIVE_H
#define VTKNATIVE_H

#include <stdint.h>

#if defined(_WIN32)
#  if defined(VTKNATIVE_EXPORTS)
#    define VTKN_API __declspec(dllexport)
#  else
#    define VTKN_API __declspec(dllimport)
#  endif
#  define VTKN_CALL __cdecl
#else
#  define VTKN_API
#  define VTKN_CALL
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Raised whenever the C API changes incompatibly; the managed side refuses a library with another value */
#define VTKN_API_VERSION 3

typedef enum vtkn_status
{
    VTKN_OK = 0,
    VTKN_ERROR_INVALID_ARGUMENT = 1,
    VTKN_ERROR_INVALID_HANDLE = 2,
    VTKN_ERROR_BUFFER_TOO_SMALL = 3,
    VTKN_ERROR_NOT_FOUND = 4,
    VTKN_ERROR_VTK = 5,
    VTKN_ERROR_INTERNAL = 6
} vtkn_status;

typedef enum vtkn_log_level
{
    VTKN_LOG_TEXT = 0,
    VTKN_LOG_WARNING = 1,
    VTKN_LOG_ERROR = 2
} vtkn_log_level;

typedef enum vtkn_event
{
    VTKN_EVENT_RENDER_END = 1,         /* values: none */
    VTKN_EVENT_LEFT_BUTTON_DOWN = 2,   /* values: x, y (display coordinates, origin bottom left), ctrl, shift */
    VTKN_EVENT_LEFT_BUTTON_UP = 3,     /* values: x, y, ctrl, shift */
    VTKN_EVENT_MOUSE_MOVE = 4,         /* values: x, y, ctrl, shift */
    VTKN_EVENT_CAMERA_MODIFIED = 5,    /* values: the 10 camera values of vtkn_view_get_camera */
    VTKN_EVENT_WIDGET_DOUBLE_CLICK = 6 /* values: the widget id, x, y */
} vtkn_event;

/* Mirrors vtkEdgesVisibility of vtkControl */
typedef enum vtkn_edges_visibility
{
    VTKN_EDGES_WIREFRAME = 0,
    VTKN_EDGES_ELEMENT_EDGES = 1,
    VTKN_EDGES_MODEL_EDGES = 2,
    VTKN_EDGES_NONE = 3
} vtkn_edges_visibility;

/* The standard view directions of pmx.view.set_orientation */
typedef enum vtkn_orientation
{
    VTKN_VIEW_FRONT = 0,
    VTKN_VIEW_BACK = 1,
    VTKN_VIEW_TOP = 2,
    VTKN_VIEW_BOTTOM = 3,
    VTKN_VIEW_LEFT = 4,
    VTKN_VIEW_RIGHT = 5,
    VTKN_VIEW_ISOMETRIC = 6,
    VTKN_VIEW_ISOMETRIC_NEGATIVE = 7,
    VTKN_VIEW_NORMAL = 8,
    VTKN_VIEW_VERTICAL = 9
} vtkn_orientation;

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

/* The corner a widget position is measured from (vtkMaxWidgetPosition). A horizontally relative widget keeps the
   relative position of its center when the window width changes */
typedef enum vtkn_widget_anchor
{
    VTKN_ANCHOR_BOTTOM_LEFT = 0,
    VTKN_ANCHOR_TOP_LEFT = 1,
    VTKN_ANCHOR_HORIZONTALLY_RELATIVE = 2
} vtkn_widget_anchor;

typedef enum vtkn_font_family
{
    VTKN_FONT_ARIAL = 0,
    VTKN_FONT_COURIER = 1,
    VTKN_FONT_TIMES = 2
} vtkn_font_family;

typedef enum vtkn_value_label
{
    VTKN_VALUE_LABEL_NONE = -1,
    VTKN_VALUE_LABEL_MAXIMUM = 0,
    VTKN_VALUE_LABEL_MINIMUM = 1
} vtkn_value_label;

/* The text of a widget. The font size is in pixels; the line offset and spacing are those of vtkTextProperty */
typedef struct vtkn_text_style
{
    double color[3];
    double line_offset;
    double line_spacing;
    int32_t font_size;
    int32_t font_family;
    int32_t bold;
    int32_t italic;
} vtkn_text_style;

/* The colors and labels of a scalar bar. table_colors: table_color_count * 3, the lookup table of the legend from
   the bottom: the added minimum color, the gradient colors (number_of_colors of them, or 256 when smooth) and the
   added maximum color. labels: one label per box border from the top, every label followed by a zero byte and one
   more zero byte after the last; the added colors that are not visible have no label */
typedef struct vtkn_scalar_bar_colors
{
    const double* table_colors;
    int32_t table_color_count;
    int32_t number_of_colors;
    int32_t smooth_colors;
    int32_t add_min_color;
    int32_t add_max_color;
    int32_t min_color_visible;
    int32_t max_color_visible;
    const char* labels;
} vtkn_scalar_bar_colors;

typedef struct vtkn_view_s* vtkn_view;
typedef struct vtkn_scalar_bar_s* vtkn_scalar_bar;

typedef void (VTKN_CALL* vtkn_log_callback)(void* user_data, int32_t level, const char* message);
typedef void (VTKN_CALL* vtkn_event_callback)(void* user_data, int32_t event_id, const double* values,
                                              int32_t value_count);

/* Library */
VTKN_API int32_t VTKN_CALL vtkn_get_api_version(void);
VTKN_API vtkn_status VTKN_CALL vtkn_get_vtk_version(char* buffer, int32_t capacity, int32_t* required);
VTKN_API vtkn_status VTKN_CALL vtkn_get_last_error(char* buffer, int32_t capacity, int32_t* required);
/* Routes the VTK output window (warnings and errors that would open a console or a message box) */
VTKN_API vtkn_status VTKN_CALL vtkn_set_log_callback(vtkn_log_callback callback, void* user_data);

/* View: one render window with one renderer, either embedded in a parent window or off screen */
VTKN_API vtkn_status VTKN_CALL vtkn_view_create(int32_t off_screen, vtkn_view* view);
VTKN_API vtkn_status VTKN_CALL vtkn_view_destroy(vtkn_view view);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_parent_window(vtkn_view view, void* parent_window);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_size(vtkn_view view, int32_t width, int32_t height);
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_size(vtkn_view view, int32_t* width, int32_t* height);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_event_callback(vtkn_view view, vtkn_event_callback callback,
                                                             void* user_data);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_background(vtkn_view view, int32_t gradient, const double* top_rgb,
                                                         const double* bottom_rgb);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_smoothing(vtkn_view view, int32_t point_smoothing,
                                                        int32_t line_smoothing);
VTKN_API vtkn_status VTKN_CALL vtkn_view_render(vtkn_view view);
/* The OpenGL vendor, renderer and version the view's context reports, as text */
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_render_info(vtkn_view view, char* buffer, int32_t capacity,
                                                          int32_t* required);

/* Camera: position, focal point, view up and parallel scale - the 10 values of pmx.view.get_parameters */
VTKN_API vtkn_status VTKN_CALL vtkn_view_get_camera(vtkn_view view, double* values10);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_camera(vtkn_view view, const double* values10);
VTKN_API vtkn_status VTKN_CALL vtkn_view_reset_camera(vtkn_view view);

/* Views, the port of the Views region of vtkControl without the animation. Every change ends the way vtkControl
   ends one: the camera is moved to the distance of the visible geometry, the clipping range is reset, and the view
   is rendered. view_parameters is SetViewParameters: the 10 camera values followed by that same finish */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_view_parameters(vtkn_view view, const double* values10);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_standard_view(vtkn_view view, int32_t orientation, int32_t zoom_to_fit);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_axis_view(vtkn_view view, const double* axis, int32_t zoom_to_fit);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_vertical_axis_view(vtkn_view view, const double* axis,
                                                                 int32_t zoom_to_fit);
VTKN_API vtkn_status VTKN_CALL vtkn_view_zoom_to_fit(vtkn_view view);
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_zoom_factor(vtkn_view view, double parallel_scale);
VTKN_API vtkn_status VTKN_CALL vtkn_view_box_zoom(vtkn_view view, int32_t x1, int32_t y1, int32_t x2, int32_t y2);

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

/* Pictures. The pixels are RGB, bottom row first as OpenGL reads them, width * height * 3 bytes */
VTKN_API vtkn_status VTKN_CALL vtkn_view_read_pixels(vtkn_view view, int32_t magnification, uint8_t* buffer,
                                                      int32_t capacity, int32_t* width, int32_t* height);
VTKN_API vtkn_status VTKN_CALL vtkn_view_write_png(vtkn_view view, const char* file_name, int32_t magnification);

/* Mouse events for the widgets of a view: VTKN_EVENT_LEFT_BUTTON_DOWN, _LEFT_BUTTON_UP or _MOUSE_MOVE at a display
   position. An embedded view routes its own mouse through the same path; a host that draws the view off screen
   forwards the events here. handled tells whether a widget took the event (the camera must then not move); the
   view is rendered when a widget changed. A double click on a widget raises VTKN_EVENT_WIDGET_DOUBLE_CLICK */
VTKN_API vtkn_status VTKN_CALL vtkn_view_handle_mouse_event(vtkn_view view, int32_t event_id, int32_t x, int32_t y,
                                                             int32_t* handled);

/* Scalar bar: the port of vtkMaxScalarBarWidget in the widget layer of a view. It belongs to its view: destroying
   the view destroys it and makes the handle invalid. Rectangles are x, y, width, height in display pixels with
   the origin at the bottom left */
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_create(vtkn_view view, vtkn_scalar_bar* bar, int32_t* widget_id);
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_destroy(vtkn_scalar_bar bar);
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_set_text_style(vtkn_scalar_bar bar, const vtkn_text_style* style);
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_set_padding(vtkn_scalar_bar bar, int32_t padding);
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_set_title(vtkn_scalar_bar bar, const char* title);
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_set_colors(vtkn_scalar_bar bar, const vtkn_scalar_bar_colors* colors);
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_set_visibility(vtkn_scalar_bar bar, int32_t visible);
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_set_frame(vtkn_scalar_bar bar, int32_t background_visible,
                                                          const double* background_rgb, int32_t border_visible,
                                                          const double* border_rgb);
/* x, y measured from the anchor corner, as SetPosition / SetTopLeftPosition / SetHorizontallyRelativePosition */
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_set_position(vtkn_scalar_bar bar, int32_t anchor, double x, double y);
/* GetRectangle: the position measured from the anchor corner and the size, rounded */
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_get_rectangle(vtkn_scalar_bar bar, int32_t* rectangle4);
/* The highlight box of the minimum or maximum label, where a value edit box is placed; found is 0 when the bar
   is hidden or has no such label */
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_get_value_label_rectangle(vtkn_scalar_bar bar, int32_t minimum,
                                                                          int32_t* rectangle4, int32_t* found);
/* The value label at a display position: a vtkn_value_label, with its highlight box */
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_hit_value_label(vtkn_scalar_bar bar, int32_t x, int32_t y,
                                                                int32_t* label, int32_t* rectangle4);
/* The hover highlight shown now; visible is 0 when there is none */
VTKN_API vtkn_status VTKN_CALL vtkn_scalar_bar_get_highlight_rectangle(vtkn_scalar_bar bar, int32_t* rectangle4,
                                                                        int32_t* visible);

#ifdef __cplusplus
}
#endif

#endif
