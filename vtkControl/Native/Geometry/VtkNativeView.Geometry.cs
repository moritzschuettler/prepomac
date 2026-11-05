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
    // The geometry area of a native view (vtkNativeGeometry.h)
    public sealed partial class VtkNativeView
    {
        // Methods
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
