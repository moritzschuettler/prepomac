/* PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
 *
 * vtkNative: the flat C API between PrePoMax and VTK 9.7 - the core: library, views, cameras and
 * pictures. vtkNative.h includes it with the geometry, widgets and selection areas.
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
#ifndef VTKNATIVECORE_H
#define VTKNATIVECORE_H

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
#define VTKN_API_VERSION 7

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

typedef struct vtkn_view_s* vtkn_view;
typedef struct vtkn_scalar_bar_s* vtkn_scalar_bar;

typedef void (VTKN_CALL* vtkn_log_callback)(void* user_data, int32_t level, const char* message);
typedef void (VTKN_CALL* vtkn_event_callback)(void* user_data, int32_t event_id, const double* values,
                                              int32_t value_count);

/* The areas of the API, each with its own version that is raised when that area changes incompatibly. The core
   version is VTKN_API_VERSION; the others are defined in the area headers */
typedef enum vtkn_area
{
    VTKN_AREA_CORE = 0,
    VTKN_AREA_GEOMETRY = 1,
    VTKN_AREA_WIDGETS = 2,
    VTKN_AREA_SELECTION = 3
} vtkn_area;

/* Library */
VTKN_API int32_t VTKN_CALL vtkn_get_api_version(void);
/* The version of an area, -1 for an area the library does not know */
VTKN_API int32_t VTKN_CALL vtkn_get_area_api_version(int32_t area);
VTKN_API vtkn_status VTKN_CALL vtkn_get_vtk_version(char* buffer, int32_t capacity, int32_t* required);
VTKN_API vtkn_status VTKN_CALL vtkn_get_last_error(char* buffer, int32_t capacity, int32_t* required);
/* Routes the VTK output window (warnings and errors that would open a console or a message box) */
VTKN_API vtkn_status VTKN_CALL vtkn_set_log_callback(vtkn_log_callback callback, void* user_data);
/* The font files of the text of every view, regular, bold, italic and bold italic, as UTF-8 paths. A null or empty
   path draws its style with the regular file, and without a regular file the text is drawn with the Arial VTK
   embeds. Text drawn before the call keeps its font until it is styled again */
VTKN_API vtkn_status VTKN_CALL vtkn_set_font_files(const char* regular, const char* bold, const char* italic,
                                                    const char* bold_italic);

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
/* While deferred, the view changes above set the camera, its distance and its clipping range, and do not render:
   the owner reads the camera they reach and animates to it */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_view_change_deferred(vtkn_view view, int32_t deferred);
/* The display position of a world point, and the world point at a display position on the near plane, with the
   current camera (vtkInteractorObserver::ComputeWorldToDisplay and ComputeDisplayToWorld) */
VTKN_API vtkn_status VTKN_CALL vtkn_view_world_to_display(vtkn_view view, const double* world3, double* display2);
VTKN_API vtkn_status VTKN_CALL vtkn_view_display_to_world(vtkn_view view, const double* display2, double* world3);

/* Pictures. The pixels are RGB, bottom row first as OpenGL reads them, width * height * 3 bytes */
VTKN_API vtkn_status VTKN_CALL vtkn_view_read_pixels(vtkn_view view, int32_t magnification, uint8_t* buffer,
                                                      int32_t capacity, int32_t* width, int32_t* height);
VTKN_API vtkn_status VTKN_CALL vtkn_view_write_png(vtkn_view view, const char* file_name, int32_t magnification);

#ifdef __cplusplus
}
#endif

#endif
