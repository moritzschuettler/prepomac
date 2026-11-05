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
#define VTKN_WIDGETS_API_VERSION 1

typedef struct vtkn_scalar_bar_s* vtkn_scalar_bar;

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

#ifdef __cplusplus
}
#endif

#endif
