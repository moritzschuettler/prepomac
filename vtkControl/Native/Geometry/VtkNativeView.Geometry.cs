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
        public void SetActorVisible(string name, VtkNativeLayer layer, bool visible)
        {
            if (name == null) throw new ArgumentNullException("name");
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_actor_visible(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                                 (int)layer, visible ? 1 : 0));
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
    }
}
