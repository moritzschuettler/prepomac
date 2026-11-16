// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // The geometry area of a native view (vtkNativeGeometry.h)
    public sealed partial class VtkNativeView
    {
        // Methods
        public void AddActor(string name, string parentName, VtkNativePartStyle style, VtkNativeMeshData geometry,
                             VtkNativeMeshData modelEdges, VtkNativeMeshData modelEdgesToDraw)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (geometry == null) throw new ArgumentNullException("geometry");
            IntPtr handle = Handle();
            //
            using (VtkNativePinnedMesh pinnedGeometry = geometry.Pin())
            using (VtkNativePinnedMesh pinnedModelEdges = modelEdges == null ? null : modelEdges.Pin())
            using (VtkNativePinnedMesh pinnedModelEdgesToDraw = modelEdgesToDraw == null ? null : modelEdgesToDraw.Pin())
            {
                VtkNativeMesh geometryMesh = pinnedGeometry.Mesh;
                IntPtr modelEdgesPointer = IntPtr.Zero;
                IntPtr modelEdgesToDrawPointer = IntPtr.Zero;
                try
                {
                    if (pinnedModelEdges != null) modelEdgesPointer = ToUnmanaged(pinnedModelEdges.Mesh);
                    if (pinnedModelEdgesToDraw != null) modelEdgesToDrawPointer = ToUnmanaged(pinnedModelEdgesToDraw.Mesh);
                    byte[] parent = parentName == null ? null : VtkNativeLibrary.ToUtf8(parentName);
                    VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_actor(handle, VtkNativeLibrary.ToUtf8(name), parent,
                                                                                 ref style, ref geometryMesh,
                                                                                 modelEdgesPointer, modelEdgesToDrawPointer));
                }
                finally
                {
                    if (modelEdgesPointer != IntPtr.Zero) Marshal.FreeHGlobal(modelEdgesPointer);
                    if (modelEdgesToDrawPointer != IntPtr.Zero) Marshal.FreeHGlobal(modelEdgesToDrawPointer);
                }
            }
        }
        //
        public void AddSymbolActor(string name, VtkNativePartStyle style, double[] points, double[] normals,
                                   VtkNativeSymbolParameters parameters)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (points == null || points.Length % 3 != 0)
                throw new ArgumentException("The points must be a multiple of 3 coordinates.", "points");
            if (normals != null && normals.Length % 3 != 0)
                throw new ArgumentException("The normals must be a multiple of 3 components.", "normals");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_symbol_actor(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                                ref style, points, points.Length / 3,
                                                                                normals, normals == null ? 0 :
                                                                                normals.Length / 3, ref parameters));
        }
        //
        public void AddGlyphActor(string name, string parentName, VtkNativePartStyle style, float[] coordinates,
                                  float[] values, int[] lines, int[] vertices, VtkNativeExtremeNodes? extremes)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (coordinates == null || coordinates.Length % 3 != 0)
                throw new ArgumentException("The coordinates must be a multiple of 3 values.", "coordinates");
            if (values == null || values.Length != coordinates.Length / 3)
                throw new ArgumentException("There must be one value per point.", "values");
            if (lines == null || lines.Length % 2 != 0)
                throw new ArgumentException("The lines must be pairs of point indices.", "lines");
            if (vertices == null) throw new ArgumentNullException("vertices");
            IntPtr extremesPointer = IntPtr.Zero;
            try
            {
                if (extremes.HasValue)
                {
                    extremesPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VtkNativeExtremeNodes)));
                    Marshal.StructureToPtr(extremes.Value, extremesPointer, false);
                }
                byte[] parent = parentName == null ? null : VtkNativeLibrary.ToUtf8(parentName);
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_glyph_actor(Handle(), VtkNativeLibrary.ToUtf8(name), parent,
                                                                                   ref style, coordinates, values.Length,
                                                                                   values, lines, lines.Length / 2, vertices,
                                                                                   vertices.Length, extremesPointer));
            }
            finally
            {
                if (extremesPointer != IntPtr.Zero) Marshal.FreeHGlobal(extremesPointer);
            }
        }
        public void SetActorScalars(string name, float[] values, VtkNativeExtremeNodes? extremes)
        {
            if (name == null) throw new ArgumentNullException("name");
            IntPtr extremesPointer = IntPtr.Zero;
            try
            {
                if (extremes.HasValue)
                {
                    extremesPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VtkNativeExtremeNodes)));
                    Marshal.StructureToPtr(extremes.Value, extremesPointer, false);
                }
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_actor_scalars(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                                     values, values == null ? 0 :
                                                                                     values.Length, extremesPointer));
            }
            finally
            {
                if (extremesPointer != IntPtr.Zero) Marshal.FreeHGlobal(extremesPointer);
            }
        }
        //
        public void SetActorCellColors(string name, VtkNativeLayer layer, float[] values, Color[] colorTable)
        {
            if (name == null) throw new ArgumentNullException("name");
            double[] rgb = new double[colorTable == null ? 0 : 3 * colorTable.Length];
            for (int i = 0; i < rgb.Length / 3; i++)
            {
                rgb[3 * i] = colorTable[i].R / 255d;
                rgb[3 * i + 1] = colorTable[i].G / 255d;
                rgb[3 * i + 2] = colorTable[i].B / 255d;
            }
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_actor_cell_colors(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                                     (int)layer, values, values == null ? 0 :
                                                                                     values.Length, rgb, rgb.Length / 3));
        }
        //
        public void SetActorsColorContours(string[] names, bool colorContours)
        {
            if (names == null) throw new ArgumentNullException("names");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_actors_color_contours(Handle(),
                                                                                         VtkNativeLibrary.ToUtf8List(names),
                                                                                         colorContours ? 1 : 0));
        }
        //
        public void UpdateScalarFormatting(VtkNativeColorSpectrum spectrum, bool keepScalarBarVisible, bool useAllFrames,
                                           double allFramesMin, double allFramesMax)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_update_scalar_formatting(Handle(), ref spectrum,
                                                                                        keepScalarBarVisible ? 1 : 0,
                                                                                        useAllFrames ? 1 : 0, allFramesMin,
                                                                                        allFramesMax));
        }
        //
        public double[] GetScalarRange()
        {
            double[] range = new double[2];
            int hasRange;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_scalar_range(Handle(), range, out hasRange));
            return hasRange == 0 ? null : range;
        }
        //
        public void SetSectionViewData(string name, VtkNativeMeshData volumeGrid, float[] values, Color sectionColor,
                                       int hatchIndex, bool hasVolumeCells)
        {
            if (name == null) throw new ArgumentNullException("name");
            double[] color = new double[] { sectionColor.R / 255d, sectionColor.G / 255d, sectionColor.B / 255d,
                                            sectionColor.A / 255d };
            IntPtr handle = Handle();
            using (VtkNativePinnedMesh pinnedGrid = volumeGrid == null ? null : volumeGrid.Pin())
            {
                IntPtr gridPointer = IntPtr.Zero;
                try
                {
                    if (pinnedGrid != null) gridPointer = ToUnmanaged(pinnedGrid.Mesh);
                    VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_section_view_data(handle, VtkNativeLibrary.ToUtf8(name),
                                                                                             gridPointer, values,
                                                                                             values == null ? 0 : values.Length,
                                                                                             color, hatchIndex,
                                                                                             hasVolumeCells ? 1 : 0));
                }
                finally
                {
                    if (gridPointer != IntPtr.Zero) Marshal.FreeHGlobal(gridPointer);
                }
            }
        }
        //
        public void CreateSectionView(VtkNativeSectionView sectionView)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_create_section_view(Handle(), ref sectionView));
        }
        //
        public void RemoveSectionView()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_remove_section_view(Handle()));
        }
        //
        public void AddSymmetry(int symmetryPlane, double[] point)
        {
            CheckLength(point, 3, "point");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_symmetry(Handle(), symmetryPlane, point));
        }
        //
        public void AddLinearPattern(double[] displacement, int numberOfItems)
        {
            CheckLength(displacement, 3, "displacement");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_linear_pattern(Handle(), displacement, numberOfItems));
        }
        //
        public void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numberOfItems)
        {
            CheckLength(axisPoint, 3, "axisPoint");
            CheckLength(axisNormal, 3, "axisNormal");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_circular_pattern(Handle(), axisPoint, axisNormal, angle,
                                                                                    numberOfItems));
        }
        //
        public void ApplyTransforms()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_apply_transforms(Handle()));
        }
        //
        public void RemoveTransformedActors()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_remove_transformed_actors(Handle()));
        }
        //
        public void ShowTransformedActors(bool show)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_show_transformed_actors(Handle(), show ? 1 : 0));
        }
        //
        public void SetExplodedViewOffsets(string[] names, double[][] offsets)
        {
            if (names == null || offsets == null || names.Length != offsets.Length)
                throw new ArgumentException("There must be one offset per name.");
            double[] values = new double[3 * offsets.Length];
            for (int i = 0; i < offsets.Length; i++) Array.Copy(offsets[i], 0, values, 3 * i, 3);
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_exploded_view_offsets(Handle(), VtkNativeLibrary.ToUtf8List(names),
                                                                                         values, names.Length));
        }
        //
        public void AddAnimationFrame(string name, float[] coordinates, float[] values, VtkNativeExtremeNodes? extremes,
                                      float[] modelEdgeCoordinates)
        {
            if (name == null) throw new ArgumentNullException("name");
            IntPtr extremesPointer = IntPtr.Zero;
            try
            {
                if (extremes.HasValue)
                {
                    extremesPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VtkNativeExtremeNodes)));
                    Marshal.StructureToPtr(extremes.Value, extremesPointer, false);
                }
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_animation_frame(
                    Handle(), VtkNativeLibrary.ToUtf8(name), coordinates, coordinates == null ? 0 : coordinates.Length / 3,
                    values, values == null ? 0 : values.Length, extremesPointer, modelEdgeCoordinates,
                    modelEdgeCoordinates == null ? 0 : modelEdgeCoordinates.Length / 3));
            }
            finally
            {
                if (extremesPointer != IntPtr.Zero) Marshal.FreeHGlobal(extremesPointer);
            }
        }
        //
        public void SetAnimationFrame(int frameNumber)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_animation_frame(Handle(), frameNumber));
        }
        //
        public double[] GetBounds()
        {
            double[] bounds = new double[6];
            int hasBounds;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_bounds(Handle(), bounds, out hasBounds));
            return hasBounds == 0 ? null : bounds;
        }
        //
        public void RenderToPng(string fileName, bool transparentBackground, int magnification)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_render_to_png(Handle(), VtkNativeLibrary.ToUtf8(fileName),
                                                                              transparentBackground ? 1 : 0, magnification));
        }
        //
        public void SetActorVisible(string name, VtkNativeLayer layer, bool visible)
        {
            if (name == null) throw new ArgumentNullException("name");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_actor_visible(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                                 (int)layer, visible ? 1 : 0));
        }
        //
        public void SetActorColor(string name, VtkNativeLayer layer, System.Drawing.Color color)
        {
            if (name == null) throw new ArgumentNullException("name");
            double[] rgba = new double[] { color.R / 255.0, color.G / 255.0, color.B / 255.0, color.A / 255.0 };
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_actor_color(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                               (int)layer, rgba));
        }
        //
        public void RenameActor(string oldName, string newName)
        {
            if (oldName == null) throw new ArgumentNullException("oldName");
            if (newName == null) throw new ArgumentNullException("newName");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_rename_actor(Handle(), VtkNativeLibrary.ToUtf8(oldName),
                                                                            VtkNativeLibrary.ToUtf8(newName)));
        }
        //
        public void HighlightActor(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_highlight_actor(Handle(), VtkNativeLibrary.ToUtf8(name)));
        }
        //
        public void HideShowActors(string[] names, bool visible)
        {
            if (names == null) throw new ArgumentNullException("names");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_hide_show_actors(Handle(), VtkNativeLibrary.ToUtf8List(names),
                                                                                visible ? 1 : 0));
        }
        public void SetCameraAdjustmentSuspended(bool suspended)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_camera_adjustment_suspended(Handle(), suspended ? 1 : 0));
        }
        //
        public void UpdateResultRepresentationVisibility(string[] partNames, bool visible, bool update)
        {
            if (partNames == null) throw new ArgumentNullException("partNames");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_update_result_representation_visibility(
                Handle(), VtkNativeLibrary.ToUtf8List(partNames), visible ? 1 : 0, update ? 1 : 0));
        }
        //
        public void Clear(VtkNativeClear what)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_clear(Handle(), (int)what));
        }
        //
        public void AddCaption(string name, string caption, double[] position, double[] offset, int fontSize,
                               VtkNativeLayer layer, bool useSecondaryHighlightColor)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (caption == null) throw new ArgumentNullException("caption");
            if (position == null || position.Length != 3) throw new ArgumentException("The position needs 3 values.", "position");
            if (offset != null && offset.Length != 3) offset = null;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_caption(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                           VtkNativeLibrary.ToUtf8(caption), position, offset,
                                                                           fontSize, (int)layer,
                                                                           useSecondaryHighlightColor ? 1 : 0));
        }
        public void AddLabels(string name, double[][] positions, string[] labels, Color color, int fontSize,
                              VtkNativeLayer layer)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (positions == null || labels == null || positions.Length != labels.Length)
                throw new ArgumentException("The label positions and values must have the same length.");
            double[] positionValues = new double[3 * positions.Length];
            for (int i = 0; i < positions.Length; i++) Array.Copy(positions[i], 0, positionValues, 3 * i, 3);
            double[] rgb = new double[] { color.R / 255d, color.G / 255d, color.B / 255d };
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_labels(Handle(), VtkNativeLibrary.ToUtf8(name), positionValues,
                                                                          positions.Length, VtkNativeLibrary.ToUtf8List(labels),
                                                                          rgb, fontSize, (int)layer));
        }
        public void AddCoordinateSystemSymbol(double[] origin, double[][] axisDirections, string[] labels, double symbolSize,
                                              VtkNativeLayer layer, bool materialAxes)
        {
            if (origin == null || origin.Length != 3) throw new ArgumentException("The origin needs 3 values.", "origin");
            if (axisDirections == null || axisDirections.Length != 3)
                throw new ArgumentException("There must be 3 axis directions.", "axisDirections");
            if (labels == null || labels.Length != 3) throw new ArgumentException("There must be 3 labels.", "labels");
            double[] directions = new double[9];
            for (int i = 0; i < 3; i++) Array.Copy(axisDirections[i], 0, directions, 3 * i, 3);
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_coordinate_system_symbol(Handle(), origin, directions,
                                                                                            VtkNativeLibrary.ToUtf8List(labels),
                                                                                            symbolSize, (int)layer,
                                                                                            materialAxes ? 1 : 0));
        }
        public void SetCaptionPosition(string name, double[] position)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (position == null || position.Length != 3) throw new ArgumentException("The position needs 3 values.", "position");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_caption_position(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                                    position));
        }
        public void SetActorPoints(string name, VtkNativeLayer layer, double[][] coordinates, double[][] normals)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (coordinates == null) throw new ArgumentNullException("coordinates");
            double[] coordinateValues = new double[3 * coordinates.Length];
            for (int i = 0; i < coordinates.Length; i++) Array.Copy(coordinates[i], 0, coordinateValues, 3 * i, 3);
            double[] normalValues = null;
            if (normals != null && normals.Length == coordinates.Length)
            {
                normalValues = new double[3 * normals.Length];
                for (int i = 0; i < normals.Length; i++) Array.Copy(normals[i], 0, normalValues, 3 * i, 3);
            }
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_actor_points(Handle(), VtkNativeLibrary.ToUtf8(name), (int)layer,
                                                                                coordinateValues, coordinates.Length,
                                                                                normalValues));
        }
        public void RemoveOverlayActors(string[] names)
        {
            if (names == null) throw new ArgumentNullException("names");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_remove_overlay_actors(Handle(),
                                                                                     VtkNativeLibrary.ToUtf8List(names)));
        }
        //
        public void SetHighlightColors(Color primary, Color secondary)
        {
            double[] primaryRgb = new double[] { primary.R / 255d, primary.G / 255d, primary.B / 255d };
            double[] secondaryRgb = new double[] { secondary.R / 255d, secondary.G / 255d, secondary.B / 255d };
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_highlight_colors(Handle(), primaryRgb, secondaryRgb));
        }
        //
        public void AddPartActor(string name, VtkNativePartStyle style, VtkNativeMeshData geometry,
                                 VtkNativeMeshData modelEdges, VtkNativeMeshData modelEdgesToDraw)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (geometry == null) throw new ArgumentNullException("geometry");
            IntPtr handle = Handle();
            //
            using (VtkNativePinnedMesh pinnedGeometry = geometry.Pin())
            using (VtkNativePinnedMesh pinnedModelEdges = modelEdges == null ? null : modelEdges.Pin())
            using (VtkNativePinnedMesh pinnedModelEdgesToDraw = modelEdgesToDraw == null ? null : modelEdgesToDraw.Pin())
            {
                VtkNativeMesh geometryMesh = pinnedGeometry.Mesh;
                IntPtr modelEdgesPointer = IntPtr.Zero;
                IntPtr modelEdgesToDrawPointer = IntPtr.Zero;
                try
                {
                    // The optional meshes are passed by pointer, so a missing one is a null pointer
                    if (pinnedModelEdges != null) modelEdgesPointer = ToUnmanaged(pinnedModelEdges.Mesh);
                    if (pinnedModelEdgesToDraw != null) modelEdgesToDrawPointer = ToUnmanaged(pinnedModelEdgesToDraw.Mesh);
                    VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_part_actor(handle, VtkNativeLibrary.ToUtf8(name),
                                                                                      ref style, ref geometryMesh,
                                                                                      modelEdgesPointer,
                                                                                      modelEdgesToDrawPointer));
                }
                finally
                {
                    if (modelEdgesPointer != IntPtr.Zero) Marshal.FreeHGlobal(modelEdgesPointer);
                    if (modelEdgesToDrawPointer != IntPtr.Zero) Marshal.FreeHGlobal(modelEdgesToDrawPointer);
                }
            }
        }
        //
        public void ClearActors()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_clear_actors(Handle()));
        }
        //
        public void SetEdgesVisibility(vtkEdgesVisibility edgesVisibility)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_edges_visibility(Handle(), (int)edgesVisibility));
        }
        //
        public void SetLighting(double ambient, double diffuse)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_lighting(Handle(), ambient, diffuse));
        }
        public void SetAmbientOcclusion(bool enabled, double relativeRadius, int kernelSize)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_ambient_occlusion(Handle(), enabled ? 1 : 0,
                                                                                    relativeRadius, kernelSize));
        }
        public void SetBasePassSuspended(bool suspended)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_base_pass_suspended(Handle(), suspended ? 1 : 0));
        }
        public void SetActorBatching(bool enabled)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_actor_batching(Handle(), enabled ? 1 : 0));
        }
        public int[] GetActorBatches()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_actor_batches(Handle(), out int batchCount,
                                                                                out int memberCount));
            return new int[] { batchCount, memberCount };
        }
        public void SetClearedActorReuse(bool enabled)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_cleared_actor_reuse(Handle(), enabled ? 1 : 0));
        }
        public int GetReusedActorCount()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_reused_actor_count(Handle(), out int count));
            return count;
        }
        //
        public void AddMeshActor(string name, double[] points, int[] cells, byte[] cellTypes, double[] colorRgb,
                                 bool edgesVisible)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (points == null || points.Length % 3 != 0)
                throw new ArgumentException("The points must be a multiple of 3 coordinates.", "points");
            if (cells == null) throw new ArgumentNullException("cells");
            if (cellTypes == null) throw new ArgumentNullException("cellTypes");
            CheckLength(colorRgb, 3, "colorRgb");
            //
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_mesh_actor(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                              points, points.Length / 3, cells,
                                                                              cells.Length, cellTypes, cellTypes.Length,
                                                                              colorRgb, edgesVisible ? 1 : 0));
        }
        //
        public void RemoveActor(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_remove_actor(Handle(), VtkNativeLibrary.ToUtf8(name)));
        }
        //
        public string[] GetActorNames()
        {
            IntPtr handle = Handle();
            return VtkNativeLibrary.GetStringList((byte[] buffer, int capacity, out int required) =>
                VtkNativeMethods.vtkn_view_get_actor_names(handle, buffer, capacity, out required));
        }
        //
        public string[] GetSectionViewHiddenActors()
        {
            IntPtr handle = Handle();
            return VtkNativeLibrary.GetStringList((byte[] buffer, int capacity, out int required) =>
                VtkNativeMethods.vtkn_view_get_section_view_hidden_actors(handle, buffer, capacity, out required));
        }
        //
        public void CropPartToStl(string name, int shape, double[] parameters, string fileName)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (fileName == null) throw new ArgumentNullException("fileName");
            if (parameters == null) throw new ArgumentNullException("parameters");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_crop_part_to_stl(Handle(), VtkNativeLibrary.ToUtf8(name), shape,
                                                                                parameters,
                                                                                VtkNativeLibrary.ToUtf8(fileName)));
        }
        // The drawn actors of the base and overlay layers as the 3MF export takes them: a wire as model edges in its
        // own color, the other actors with black edges
        public vtkMaxActorData[] GetExportActors()
        {
            IntPtr handle = Handle();
            int count;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_prepare_export(handle, out count));
            vtkMaxActorData[] actors = new vtkMaxActorData[count];
            for (int i = 0; i < count; i++)
            {
                VtkNativeExportActor exported;
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_export_actor(handle, i, out exported));
                Color color = Color.FromArgb(ToByte(exported.ColorA), ToByte(exported.ColorR), ToByte(exported.ColorG),
                                             ToByte(exported.ColorB));
                vtkMaxActorData data = new vtkMaxActorData();
                data.Name = Marshal.PtrToStringAnsi(exported.Name);
                if (exported.Wire != 0) data.ColorTable = new Color[] { color, color, color };
                else data.ColorTable = new Color[] { color, Color.Black, Color.Black };
                data.ColorContours = exported.ColorContours != 0;
                data.Geometry = ToExchangeData(exported.Coordinates, exported.PointCount, exported.Triangles,
                                               exported.TriangleCount, 3);
                if (data.Geometry != null && exported.Values != IntPtr.Zero)
                {
                    data.Geometry.Nodes.Values = new float[exported.PointCount];
                    Marshal.Copy(exported.Values, data.Geometry.Nodes.Values, 0, exported.PointCount);
                }
                data.ElementEdges = ToExchangeData(exported.ElementEdgeCoordinates, exported.ElementEdgePointCount,
                                                   exported.ElementEdges, exported.ElementEdgeCount, 2);
                data.ModelEdges = ToExchangeData(exported.ModelEdgeCoordinates, exported.ModelEdgePointCount,
                                                 exported.ModelEdges, exported.ModelEdgeCount, 2);
                actors[i] = data;
            }
            return actors;
        }
        private static int ToByte(double value)
        {
            return Math.Max(0, Math.Min(255, (int)Math.Round(value * 255)));
        }
        private static CaeGlobals.PartExchangeData ToExchangeData(IntPtr coordinates, int pointCount, IntPtr cells,
                                                                  int cellCount, int cellSize)
        {
            if (coordinates == IntPtr.Zero || pointCount <= 0) return null;
            double[] values = new double[3 * pointCount];
            Marshal.Copy(coordinates, values, 0, values.Length);
            int[] cellValues = new int[cellSize * cellCount];
            if (cellValues.Length > 0) Marshal.Copy(cells, cellValues, 0, cellValues.Length);
            CaeGlobals.PartExchangeData data = new CaeGlobals.PartExchangeData();
            data.Nodes.Coor = new double[pointCount][];
            for (int i = 0; i < pointCount; i++)
                data.Nodes.Coor[i] = new double[] { values[3 * i], values[3 * i + 1], values[3 * i + 2] };
            data.Cells.CellNodeIds = new int[cellCount][];
            for (int i = 0; i < cellCount; i++)
            {
                data.Cells.CellNodeIds[i] = new int[cellSize];
                Array.Copy(cellValues, cellSize * i, data.Cells.CellNodeIds[i], 0, cellSize);
            }
            return data;
        }
        // The cell of the mesh that holds the point, else the closest cell, with the weights of its points
        public static bool LocatePointInMesh(VtkNativeMeshData mesh, double[] point, out int[] pointIndices,
                                             out double[] weights, out double[] closest)
        {
            CheckLength(point, 3, "point");
            const int capacity = 64;
            int[] indices = new int[capacity];
            double[] cellWeights = new double[capacity];
            closest = new double[3];
            int count;
            int found;
            using (VtkNativePinnedMesh pinned = mesh.Pin())
            {
                VtkNativeMesh nativeMesh = pinned.Mesh;
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_locate_point_in_mesh(ref nativeMesh, point, indices, cellWeights,
                                                                                   capacity, out count, closest, out found));
            }
            pointIndices = new int[count];
            weights = new double[count];
            Array.Copy(indices, pointIndices, count);
            Array.Copy(cellWeights, weights, count);
            return found != 0;
        }
    }
}
