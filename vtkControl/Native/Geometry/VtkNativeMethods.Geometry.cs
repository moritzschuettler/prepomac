// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // The declarations of vtkNativeGeometry.h
    internal static partial class VtkNativeMethods
    {
        // Variables
        public const int GeometryApiVersion = 9;


        // Methods
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_part_actor(IntPtr view, byte[] name, ref VtkNativePartStyle style,
                                                                      ref VtkNativeMesh geometry, IntPtr modelEdges,
                                                                      IntPtr modelEdgesToDraw);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_actor(IntPtr view, byte[] name, byte[] parentName,
                                                                 ref VtkNativePartStyle style, ref VtkNativeMesh geometry,
                                                                 IntPtr modelEdges, IntPtr modelEdgesToDraw);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_actor_visible(IntPtr view, byte[] name, int layer, int visible);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_actor_color(IntPtr view, byte[] name, int layer, double[] rgba);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_rename_actor(IntPtr view, byte[] oldName, byte[] newName);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_highlight_actor(IntPtr view, byte[] name);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_hide_show_actors(IntPtr view, byte[] names, int visible);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_camera_adjustment_suspended(IntPtr view, int suspended);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_update_result_representation_visibility(IntPtr view, byte[] partNames,
                                                                                                int visible, int update);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_symbol_actor(IntPtr view, byte[] name, ref VtkNativePartStyle style,
                                                                        double[] points, int pointCount, double[] normals,
                                                                        int normalCount,
                                                                        ref VtkNativeSymbolParameters parameters);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_glyph_actor(IntPtr view, byte[] name, byte[] parentName,
                                                                       ref VtkNativePartStyle style, float[] coordinates3,
                                                                       int pointCount, float[] values, int[] lines,
                                                                       int lineCount, int[] vertices, int vertexCount,
                                                                       IntPtr extremes);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_caption(IntPtr view, byte[] name, byte[] caption, double[] position3,
                                                                   double[] offset3, int fontSize, int layer,
                                                                   int useSecondaryHighlightColor);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_labels(IntPtr view, byte[] name, double[] positions3, int count,
                                                                  byte[] labels, double[] colorRgb, int fontSize, int layer);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_coordinate_system_symbol(IntPtr view, double[] origin3,
                                                                                    double[] axisDirections9, byte[] labels,
                                                                                    double symbolSize, int layer,
                                                                                    int materialAxes);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_caption_position(IntPtr view, byte[] name, double[] position3);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_actor_points(IntPtr view, byte[] name, int layer,
                                                                        double[] coordinates3, int count, double[] normals3);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_actor_scalars(IntPtr view, byte[] name, float[] values, int count,
                                                                         IntPtr extremes);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_actor_cell_colors(IntPtr view, byte[] name, int layer, float[] values,
                                                                             int count, double[] rgb, int rgbCount);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_actors_color_contours(IntPtr view, byte[] names, int colorContours);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_color_spectrum_get_colors(ref VtkNativeColorSpectrum spectrum,
                                                                            double[] positions, int count,
                                                                            [Out] double[] rgb);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_update_scalar_formatting(IntPtr view, ref VtkNativeColorSpectrum spectrum,
                                                                                int keepScalarBarVisible, int useAllFrames,
                                                                                double allFramesMin, double allFramesMax);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_scalar_range(IntPtr view, double[] range2, out int hasRange);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_section_view_data(IntPtr view, byte[] name, IntPtr volumeGrid,
                                                                             float[] values, int valueCount,
                                                                             double[] sectionColorRgba, int hatchIndex,
                                                                             int hasVolumeCells);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_create_section_view(IntPtr view, ref VtkNativeSectionView sectionView);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_remove_section_view(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_symmetry(IntPtr view, int symmetryPlane, double[] point3);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_linear_pattern(IntPtr view, double[] displacement3, int numberOfItems);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_circular_pattern(IntPtr view, double[] axisPoint3, double[] axisNormal3,
                                                                            double angle, int numberOfItems);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_apply_transforms(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_remove_transformed_actors(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_show_transformed_actors(IntPtr view, int show);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_exploded_view_offsets(IntPtr view, byte[] names, double[] offsets3,
                                                                                 int count);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_animation_frame(IntPtr view, byte[] name, float[] coordinates3,
                                                                           int pointCount, float[] values, int valueCount,
                                                                           IntPtr extremes, float[] modelEdgeCoordinates3,
                                                                           int modelEdgePointCount);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_animation_frame(IntPtr view, int frameNumber);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_bounds(IntPtr view, double[] bounds6, out int hasBounds);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_render_to_png(IntPtr view, byte[] fileName, int transparentBackground,
                                                                     int magnification);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_clear(IntPtr view, int what);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_remove_overlay_actors(IntPtr view, byte[] names);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_highlight_colors(IntPtr view, double[] primaryRgb,
                                                                            double[] secondaryRgb);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_clear_actors(IntPtr view);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_edges_visibility(IntPtr view, int edgesVisibility);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_lighting(IntPtr view, double ambient, double diffuse);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_ambient_occlusion(IntPtr view, int enabled,
                                                                             double relativeRadius, int kernelSize);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_actor_batching(IntPtr view, int enabled);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_actor_batches(IntPtr view, out int batchCount,
                                                                         out int memberCount);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_set_cleared_actor_reuse(IntPtr view, int enabled);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_reused_actor_count(IntPtr view, out int count);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_add_mesh_actor(IntPtr view, byte[] name, double[] points,
                                                                      int pointCount, int[] cells, int cellsLength,
                                                                      byte[] cellTypes, int cellCount,
                                                                      double[] colorRgb, int edgesVisible);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_remove_actor(IntPtr view, byte[] name);
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern VtkNativeStatus vtkn_view_get_actor_names(IntPtr view, byte[] buffer, int capacity,
                                                                       out int required);
    }
}
