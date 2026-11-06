// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// The exported C functions of the geometry area. Each one only checks its arguments, finds its object and forwards,
// inside the guard of vtkNativeGuard.h.
#include "vtkNativeGuard.h"
#include "vtkNativeView.h"

#include <vtkFloatArray.h>
#include <vtkNew.h>
#include <vtkPointData.h>

#include <cstring>
#include <set>
#include <vector>

using namespace vtkn;

namespace
{
    std::set<std::string> ToNameSet(const char* names, const char* argumentName)
    {
        if (names == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, std::string("The argument '") + argumentName +
                                                                        "' must not be null.");
        std::set<std::string> set;
        for (const char* name = names; *name != '\0'; name += std::strlen(name) + 1) set.insert(name);
        return set;
    }

    vtkn_layer ToLayer(int32_t layer)
    {
        Require(layer >= VTKN_LAYER_BASE && layer <= VTKN_LAYER_OVERLAY, "The layer is not one of vtkn_layer.");
        return static_cast<vtkn_layer>(layer);
    }
}

extern "C"
{
    vtkn_status VTKN_CALL vtkn_view_add_part_actor(vtkn_view view, const char* name, const vtkn_part_style* style,
                                                   const vtkn_mesh* geometry, const vtkn_mesh* model_edges,
                                                   const vtkn_mesh* model_edges_to_draw)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            Require(geometry != nullptr, "The argument 'geometry' must not be null.");
            View::FromHandle(view)->AddPartActor(ToName(name, "name"), *style, *geometry, model_edges,
                                                 model_edges_to_draw);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_actor(vtkn_view view, const char* name, const char* parent_name,
                                              const vtkn_part_style* style, const vtkn_mesh* geometry,
                                              const vtkn_mesh* model_edges, const vtkn_mesh* model_edges_to_draw)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            Require(geometry != nullptr, "The argument 'geometry' must not be null.");
            View::FromHandle(view)->AddActor(ToName(name, "name"), parent_name != nullptr ? parent_name : "", *style,
                                             *geometry, model_edges, model_edges_to_draw);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_actor_visible(vtkn_view view, const char* name, int32_t layer,
                                                      int32_t visible)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetActorVisible(ToName(name, "name"), ToLayer(layer), visible != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_highlight_actor(vtkn_view view, const char* name)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->HighlightActor(ToName(name, "name"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_hide_show_actors(vtkn_view view, const char* names, int32_t visible)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->HideShowActors(ToNameSet(names, "names"), visible != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_update_result_representation_visibility(vtkn_view view, const char* part_names,
                                                                            int32_t visible, int32_t update)
    {
        return GuardVtk([&]() {
            View* target = View::FromHandle(view);
            target->UpdateResultRepresentationVisibility(ToNameSet(part_names, "part_names"), visible != 0);
            if (update != 0) target->AdjustCameraDistanceAndClipping();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_symbol_actor(vtkn_view view, const char* name, const vtkn_part_style* style,
                                                     const double* points, int32_t point_count, const double* normals,
                                                     int32_t normal_count, const vtkn_symbol_parameters* parameters)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            Require(parameters != nullptr, "The argument 'parameters' must not be null.");
            View::FromHandle(view)->AddSymbolActor(ToName(name, "name"), *style, points, point_count, normals,
                                                   normal_count, *parameters);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_glyph_actor(vtkn_view view, const char* name, const char* parent_name,
                                                    const vtkn_part_style* style, const float* coordinates3,
                                                    int32_t point_count, const float* values, const int32_t* lines,
                                                    int32_t line_count, const int32_t* vertices,
                                                    int32_t vertex_count, const vtkn_extreme_nodes* extremes)
    {
        return GuardVtk([&]() {
            Require(style != nullptr, "The argument 'style' must not be null.");
            View::FromHandle(view)->AddGlyphActor(ToName(name, "name"), parent_name != nullptr ? parent_name : "",
                                                  *style, coordinates3, point_count, values, lines, line_count,
                                                  vertices, vertex_count, extremes);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_caption(vtkn_view view, const char* name, const char* caption,
                                                const double* position3, const double* offset3, int32_t font_size,
                                                int32_t layer, int32_t use_secondary_highlight_color)
    {
        return GuardVtk([&]() {
            Require(caption != nullptr, "The argument 'caption' must not be null.");
            Require(position3 != nullptr, "The argument 'position3' must not be null.");
            View::FromHandle(view)->AddCaption(ToName(name, "name"), caption, position3, offset3, font_size,
                                               ToLayer(layer), use_secondary_highlight_color != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_labels(vtkn_view view, const char* name, const double* positions3,
                                               int32_t count, const char* labels, const double* color_rgb,
                                               int32_t font_size, int32_t layer)
    {
        return GuardVtk([&]() {
            Require(labels != nullptr, "The argument 'labels' must not be null.");
            Require(color_rgb != nullptr, "The argument 'color_rgb' must not be null.");
            std::vector<std::string> labelList;
            const char* label = labels;
            for (int i = 0; i < count; i++)
            {
                labelList.push_back(label);
                label += std::strlen(label) + 1;
            }
            View::FromHandle(view)->AddLabels(ToName(name, "name"), positions3, count, labelList, color_rgb, font_size,
                                              ToLayer(layer));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_caption_position(vtkn_view view, const char* name, const double* position3)
    {
        return GuardVtk([&]() {
            Require(position3 != nullptr, "The argument 'position3' must not be null.");
            View::FromHandle(view)->SetCaptionPosition(ToName(name, "name"), position3);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_actor_points(vtkn_view view, const char* name, int32_t layer,
                                                     const double* coordinates3, int32_t count,
                                                     const double* normals3)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetActorPoints(ToName(name, "name"), ToLayer(layer), coordinates3, count,
                                                   normals3);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_actor_scalars(vtkn_view view, const char* name, const float* values,
                                                      int32_t count, const vtkn_extreme_nodes* extremes)
    {
        return GuardVtk([&]() {
            std::string actorName = ToName(name, "name");
            PartActor* actor = View::FromHandle(view)->FindActor(actorName, VTKN_LAYER_BASE);
            if (actor == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "There is no actor named " + actorName + ".");
            actor->SetPointScalars(values, count, extremes);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_actor_cell_colors(vtkn_view view, const char* name, int32_t layer,
                                                          const float* values, int32_t count, const double* rgb,
                                                          int32_t rgb_count)
    {
        return GuardVtk([&]() {
            Require(rgb_count == 0 || rgb != nullptr, "The argument 'rgb' must not be null.");
            std::string actorName = ToName(name, "name");
            PartActor* actor = View::FromHandle(view)->FindActor(actorName, ToLayer(layer));
            if (actor == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "There is no actor named " + actorName + ".");
            actor->SetCellColors(values, count, rgb, rgb_count);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_actors_color_contours(vtkn_view view, const char* names,
                                                              int32_t color_contours)
    {
        return GuardVtk([&]() {
            View* target = View::FromHandle(view);
            for (const std::string& name : ToNameSet(names, "names"))
            {
                PartActor* actor = target->FindActor(name, VTKN_LAYER_BASE);
                if (actor != nullptr) actor->SetColorContours(color_contours != 0);
            }
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_update_scalar_formatting(vtkn_view view, const vtkn_color_spectrum* spectrum,
                                                             int32_t keep_scalar_bar_visible, int32_t use_all_frames,
                                                             double all_frames_min, double all_frames_max)
    {
        return GuardVtk([&]() {
            Require(spectrum != nullptr, "The argument 'spectrum' must not be null.");
            View::FromHandle(view)->UpdateScalarFormatting(*spectrum, keep_scalar_bar_visible != 0,
                                                           use_all_frames != 0, all_frames_min, all_frames_max);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_scalar_range(vtkn_view view, double* range2, int32_t* has_range)
    {
        return GuardVtk([&]() {
            Require(range2 != nullptr && has_range != nullptr, "The output arguments must not be null.");
            *has_range = View::FromHandle(view)->GetScalarRange(range2) ? 1 : 0;
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_section_view_data(vtkn_view view, const char* name, const vtkn_mesh* volume_grid,
                                                          const float* values, int32_t value_count,
                                                          const double* section_color_rgba, int32_t hatch_index,
                                                          int32_t has_volume_cells)
    {
        return GuardVtk([&]() {
            Require(section_color_rgba != nullptr, "The argument 'section_color_rgba' must not be null.");
            std::string actorName = ToName(name, "name");
            PartActor* actor = View::FromHandle(view)->FindActor(actorName, VTKN_LAYER_BASE);
            if (actor == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "There is no actor named " + actorName + ".");
            vtkSmartPointer<vtkUnstructuredGrid> grid;
            if (volume_grid != nullptr)
            {
                grid = BuildUnstructuredGrid(*volume_grid);
                if (values != nullptr)
                {
                    Require(value_count == volume_grid->point_count, "There must be one value per volume grid point.");
                    vtkNew<vtkFloatArray> scalars;
                    scalars->SetName("scalars");
                    scalars->SetNumberOfValues(value_count);
                    for (int i = 0; i < value_count; i++) scalars->SetValue(i, values[i]);
                    grid->GetPointData()->SetScalars(scalars);
                }
            }
            actor->SetSectionViewData(grid, section_color_rgba, hatch_index, has_volume_cells != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_create_section_view(vtkn_view view, const vtkn_section_view* section_view)
    {
        return GuardVtk([&]() {
            Require(section_view != nullptr, "The argument 'section_view' must not be null.");
            View::FromHandle(view)->CreateSectionView(*section_view);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_remove_section_view(vtkn_view view)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->RemoveSectionView();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_symmetry(vtkn_view view, int32_t symmetry_plane, const double* point3)
    {
        return GuardVtk([&]() {
            Require(point3 != nullptr, "The argument 'point3' must not be null.");
            View::FromHandle(view)->AddSymmetry(symmetry_plane, point3);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_linear_pattern(vtkn_view view, const double* displacement3,
                                                       int32_t number_of_items)
    {
        return GuardVtk([&]() {
            Require(displacement3 != nullptr, "The argument 'displacement3' must not be null.");
            View::FromHandle(view)->AddLinearPattern(displacement3, number_of_items);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_circular_pattern(vtkn_view view, const double* axis_point3,
                                                         const double* axis_normal3, double angle,
                                                         int32_t number_of_items)
    {
        return GuardVtk([&]() {
            Require(axis_point3 != nullptr && axis_normal3 != nullptr, "The axis must not be null.");
            View::FromHandle(view)->AddCircularPattern(axis_point3, axis_normal3, angle, number_of_items);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_apply_transforms(vtkn_view view)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->ApplyTransforms();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_remove_transformed_actors(vtkn_view view)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->RemoveTransformedActors();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_show_transformed_actors(vtkn_view view, int32_t show)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->ShowTransformedActors(show != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_exploded_view_offsets(vtkn_view view, const char* names,
                                                              const double* offsets3, int32_t count)
    {
        return GuardVtk([&]() {
            Require(names != nullptr && (count == 0 || offsets3 != nullptr), "The names and offsets must not be null.");
            std::map<std::string, std::array<double, 3>> offsets;
            const char* name = names;
            for (int i = 0; i < count && *name != '\0'; i++, name += std::strlen(name) + 1)
                offsets[name] = { offsets3[3 * i], offsets3[3 * i + 1], offsets3[3 * i + 2] };
            View::FromHandle(view)->SetExplodedViewOffsets(offsets);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_animation_frame(vtkn_view view, const char* name, const float* coordinates3,
                                                        int32_t point_count, const float* values, int32_t value_count,
                                                        const vtkn_extreme_nodes* extremes,
                                                        const float* model_edge_coordinates3,
                                                        int32_t model_edge_point_count)
    {
        return GuardVtk([&]() {
            std::string actorName = ToName(name, "name");
            PartActor* actor = View::FromHandle(view)->FindActor(actorName, VTKN_LAYER_BASE);
            if (actor == nullptr) throw Error(VTKN_ERROR_INVALID_ARGUMENT, "There is no actor named " + actorName + ".");
            actor->AddAnimationFrame(coordinates3, point_count, values, value_count, extremes,
                                     model_edge_coordinates3, model_edge_point_count);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_animation_frame(vtkn_view view, int32_t frame_number)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetAnimationFrame(frame_number);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_bounds(vtkn_view view, double* bounds6, int32_t* has_bounds)
    {
        return GuardVtk([&]() {
            Require(bounds6 != nullptr && has_bounds != nullptr, "The output arguments must not be null.");
            *has_bounds = View::FromHandle(view)->GetVisibleBounds(bounds6) ? 1 : 0;
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_render_to_png(vtkn_view view, const char* file_name, int32_t transparent_background,
                                                  int32_t magnification)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->RenderToPng(ToName(file_name, "file_name"), transparent_background != 0,
                                                magnification);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_clear(vtkn_view view, int32_t what)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->Clear(static_cast<vtkn_clear>(what));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_remove_overlay_actors(vtkn_view view, const char* names)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->RemoveOverlayActors(ToNameSet(names, "names"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_highlight_colors(vtkn_view view, const double* primary_rgb,
                                                         const double* secondary_rgb)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetHighlightColors(primary_rgb, secondary_rgb);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_clear_actors(vtkn_view view)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->ClearActors();
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_edges_visibility(vtkn_view view, int32_t edges_visibility)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetEdgesVisibility(static_cast<vtkn_edges_visibility>(edges_visibility));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_set_lighting(vtkn_view view, double ambient, double diffuse)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->SetLighting(ambient, diffuse);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_add_mesh_actor(vtkn_view view, const char* name, const double* points,
                                                   int32_t point_count, const int32_t* cells, int32_t cells_length,
                                                   const uint8_t* cell_types, int32_t cell_count,
                                                   const double* color_rgb, int32_t edges_visible)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->AddMeshActor(ToName(name, "name"), points, point_count, cells, cells_length,
                                                 cell_types, cell_count, color_rgb, edges_visible != 0);
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_remove_actor(vtkn_view view, const char* name)
    {
        return GuardVtk([&]() {
            View::FromHandle(view)->RemoveActor(ToName(name, "name"));
            return VTKN_OK;
        });
    }

    vtkn_status VTKN_CALL vtkn_view_get_actor_names(vtkn_view view, char* buffer, int32_t capacity,
                                                    int32_t* required)
    {
        return GuardVtk([&]() { return CopyOut(View::FromHandle(view)->GetActorNames(), buffer, capacity, required); });
    }
}
