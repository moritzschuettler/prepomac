/* PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
 *
 * vtkNative, the widgets area: the 2D widgets of the widget layer and the mouse events that drive them. The
 * conventions are those of vtkNativeCore.h.
 */
#ifndef VTKNATIVEWIDGETS_H
#define VTKNATIVEWIDGETS_H

#include "vtkNativeCore.h"

#ifdef __cplusplus
extern "C" {
#endif

/* Raised whenever this area changes incompatibly */
#define VTKN_WIDGETS_API_VERSION 2

typedef struct vtkn_scalar_bar_s* vtkn_scalar_bar;
typedef struct vtkn_widget_s* vtkn_widget;

/* The widgets of the widget layer besides the scalar bar: a text box (the probe), the status block, the scale bar,
   the color bar, a text box with an arrow (the probe and min/max annotations) and the triad of the global axes */
typedef enum vtkn_widget_kind
{
    VTKN_WIDGET_TEXT = 0,
    VTKN_WIDGET_STATUS_BLOCK = 1,
    VTKN_WIDGET_SCALE_BAR = 2,
    VTKN_WIDGET_COLOR_BAR = 3,
    VTKN_WIDGET_TEXT_WITH_ARROW = 4,
    VTKN_WIDGET_TRIAD = 5
} vtkn_widget_kind;

/* The click targets of the triad (vtkMaxCoorSysTarget): a cone or a negative end turns the view to look along
   that axis, the origin to the isometric view, a shaft turns its axis to the vertical */
typedef enum vtkn_triad_target
{
    VTKN_TRIAD_NONE = 0,
    VTKN_TRIAD_ORIGIN = 1,
    VTKN_TRIAD_X_POSITIVE = 2,
    VTKN_TRIAD_X_NEGATIVE = 3,
    VTKN_TRIAD_Y_POSITIVE = 4,
    VTKN_TRIAD_Y_NEGATIVE = 5,
    VTKN_TRIAD_Z_POSITIVE = 6,
    VTKN_TRIAD_Z_NEGATIVE = 7,
    VTKN_TRIAD_X_POSITIVE_VERTICAL = 8,
    VTKN_TRIAD_X_NEGATIVE_VERTICAL = 9,
    VTKN_TRIAD_Y_POSITIVE_VERTICAL = 10,
    VTKN_TRIAD_Y_NEGATIVE_VERTICAL = 11,
    VTKN_TRIAD_Z_POSITIVE_VERTICAL = 12,
    VTKN_TRIAD_Z_NEGATIVE_VERTICAL = 13
} vtkn_triad_target;

/* The events of this area, raised through the event callback of the view after the core events */
#define VTKN_EVENT_WIDGET_TARGET_CLICKED 20 /* values: the widget id, the vtkn_triad_target */
#define VTKN_EVENT_WIDGET_HOVER_CHANGED 21  /* values: the widget id, 1 when the mouse is over a target */

/* Formats a number for a label with a .NET number format: writes the text and its zero byte into buffer when it
   fits and returns the size the text needs with its zero byte. Called on the thread that renders the view */
typedef int32_t(VTKN_CALL* vtkn_number_formatter)(void* user_data, double value, char* buffer, int32_t buffer_size);

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

/* Widgets: the ports of the vtkMax widgets in the widget layer of a view, created by kind. A widget belongs to its
   view: destroying the view destroys it and makes the handle invalid. A call that a kind does not take fails with
   VTKN_ERROR_INVALID_ARGUMENT. Display coordinates have the origin at the bottom left */
VTKN_API vtkn_status VTKN_CALL vtkn_widget_create(vtkn_view view, int32_t kind, vtkn_widget* widget,
                                                  int32_t* widget_id);
VTKN_API vtkn_status VTKN_CALL vtkn_widget_destroy(vtkn_widget widget);
VTKN_API vtkn_status VTKN_CALL vtkn_widget_set_text_style(vtkn_widget widget, const vtkn_text_style* style);
VTKN_API vtkn_status VTKN_CALL vtkn_widget_set_padding(vtkn_widget widget, int32_t padding);
VTKN_API vtkn_status VTKN_CALL vtkn_widget_set_visibility(vtkn_widget widget, int32_t visible);
VTKN_API vtkn_status VTKN_CALL vtkn_widget_set_frame(vtkn_widget widget, int32_t background_visible,
                                                     const double* background_rgb, int32_t border_visible,
                                                     const double* border_rgb);
/* x, y measured from the anchor corner, as SetPosition / SetTopLeftPosition / SetHorizontallyRelativePosition */
VTKN_API vtkn_status VTKN_CALL vtkn_widget_set_position(vtkn_widget widget, int32_t anchor, double x, double y);
/* The position measured from the anchor corner and the size, as GetBoundingBox; visible is the visibility */
VTKN_API vtkn_status VTKN_CALL vtkn_widget_get_geometry(vtkn_widget widget, double* position_size4,
                                                        int32_t* visible);
/* GetRectangle: the position and the size, rounded */
VTKN_API vtkn_status VTKN_CALL vtkn_widget_get_rectangle(vtkn_widget widget, int32_t* rectangle4);
/* The hover highlight shown now as four corners, x and y each; visible is 0 when there is none */
VTKN_API vtkn_status VTKN_CALL vtkn_widget_get_highlight(vtkn_widget widget, double* corners8, int32_t* visible);
/* The text of a text, status block, color bar (the title) or arrow widget */
VTKN_API vtkn_status VTKN_CALL vtkn_widget_set_text(vtkn_widget widget, const char* text);
/* The status block: the lines as the C# widget makes them; line4, the animation line, may be empty */
VTKN_API vtkn_status VTKN_CALL vtkn_status_block_set_lines(vtkn_widget widget, const char* line1, const char* line2,
                                                           const char* line3_fixed, const char* line3_factor,
                                                           const char* line4);
/* Whether the last press on the status block was on the deformation scale factor, and that box */
VTKN_API vtkn_status VTKN_CALL vtkn_status_block_get_scale_factor_hit(vtkn_widget widget, int32_t* clicked,
                                                                      int32_t* rectangle4);
/* The scale bar: its width in pixels, the unit after the last label, and the formatter of its labels */
VTKN_API vtkn_status VTKN_CALL vtkn_scale_bar_set_width(vtkn_widget widget, int32_t width);
VTKN_API vtkn_status VTKN_CALL vtkn_scale_bar_set_unit(vtkn_widget widget, const char* unit);
VTKN_API vtkn_status VTKN_CALL vtkn_scale_bar_set_number_formatter(vtkn_widget widget,
                                                                   vtkn_number_formatter formatter,
                                                                   void* user_data);
/* The labels as drawn, each followed by a zero byte and one more zero byte after the last; required_size is the
   size the list needs */
VTKN_API vtkn_status VTKN_CALL vtkn_scale_bar_get_labels(vtkn_widget widget, char* buffer, int32_t buffer_size,
                                                         int32_t* required_size);
/* The color bar: colors_rgb holds count * 3 values from 0 to 1; labels one label per color, each followed by a zero
   byte */
VTKN_API vtkn_status VTKN_CALL vtkn_color_bar_set_colors_and_labels(vtkn_widget widget, const double* colors_rgb,
                                                                    const char* labels, int32_t count);
/* The arrow widget: the anchor point in world coordinates, the position reset from it, a move of the box, and the
   leader line x0, y0 (at the box), x1, y1 (at the head) */
VTKN_API vtkn_status VTKN_CALL vtkn_arrow_set_anchor_point(vtkn_widget widget, double x, double y, double z);
VTKN_API vtkn_status VTKN_CALL vtkn_arrow_reset_position_from_anchor(vtkn_widget widget);
VTKN_API vtkn_status VTKN_CALL vtkn_arrow_reset_initial_position(vtkn_widget widget);
VTKN_API vtkn_status VTKN_CALL vtkn_arrow_offset_position(vtkn_widget widget, double dx, double dy);
VTKN_API vtkn_status VTKN_CALL vtkn_arrow_get_leader_line(vtkn_widget widget, double* line4);
/* The triad: the target at a display position, and the update after a camera change made without the view */
VTKN_API vtkn_status VTKN_CALL vtkn_triad_get_target(vtkn_widget widget, int32_t x, int32_t y, int32_t* target);
VTKN_API vtkn_status VTKN_CALL vtkn_triad_update_geometry(vtkn_widget widget);
/* The widget highlights of a view: off, or the whole widget only instead of the part under the mouse */
VTKN_API vtkn_status VTKN_CALL vtkn_view_set_widget_highlight(vtkn_view view, int32_t enabled,
                                                              int32_t local_highlight_enabled);

#ifdef __cplusplus
}
#endif

#endif
