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
using System.Text;

namespace vtkControl.Native
{
    // The selection area of a native view (vtkNativeSelection.h)
    public sealed partial class VtkNativeView
    {
        // Methods
        // Picking: the screen to model step of a selection. A point pick returns false when nothing was picked; the
        // direction of projection is set either way
        public bool PickPoint(int x, int y, int selectionDistance, bool limitDistance, string[] selectableParts,
                              out double[] point, out double[] direction, out string partName, out int cellId)
        {
            IntPtr handle = Handle();
            byte[] selectable = selectableParts == null ? null : VtkNativeLibrary.ToUtf8List(selectableParts);
            VtkNativePointPick pick = new VtkNativePointPick();
            byte[] name = new byte[256];
            int required;
            VtkNativeStatus status = VtkNativeMethods.vtkn_view_pick_point(handle, x, y, selectionDistance,
                                                                          limitDistance ? 1 : 0, selectable, ref pick,
                                                                          name, name.Length, out required);
            if (status == VtkNativeStatus.BufferTooSmall)
            {
                name = new byte[required];
                status = VtkNativeMethods.vtkn_view_pick_point(handle, x, y, selectionDistance, limitDistance ? 1 : 0,
                                                              selectable, ref pick, name, name.Length, out required);
            }
            VtkNativeLibrary.Check(status);
            direction = new double[] { pick.DirectionX, pick.DirectionY, pick.DirectionZ };
            cellId = pick.CellId;
            if (pick.Hit == 0)
            {
                point = null;
                partName = null;
                return false;
            }
            point = new double[] { pick.PointX, pick.PointY, pick.PointZ };
            partName = Encoding.UTF8.GetString(name, 0, required - 1);
            return true;
        }
        //
        // An area pick: the parts rendered inside the box, and the six frustum planes as origin and normal
        public string[] PickArea(int x1, int y1, int x2, int y2, out double[][] planes)
        {
            IntPtr handle = Handle();
            double[] values = new double[36];
            string[] names = VtkNativeLibrary.GetStringList((byte[] buffer, int capacity, out int required) =>
                VtkNativeMethods.vtkn_view_pick_area(handle, x1, y1, x2, y2, values, buffer, capacity, out required));
            planes = new double[6][];
            for (int i = 0; i < 6; i++)
            {
                planes[i] = new double[6];
                Array.Copy(values, 6 * i, planes[i], 0, 6);
            }
            return names;
        }
        // Exact picking (the default): the point on the ray of the pixel and, where the pixel misses, the geometry
        // within the selection distance; else the point from the depth of the pick and no pick within the distance
        public void SetExactPicking(bool exact)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_exact_picking(Handle(), exact ? 1 : 0));
        }
        public void SetSectionViewPickMode(bool partSelection, bool nodeOrElementSelection)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_section_view_pick_mode(Handle(), partSelection ? 1 : 0,
                                                                                          nodeOrElementSelection ? 1 : 0));
        }
        //
        // The id resolution. The surface cell closest to the point among the visible or the selectable parts, with
        // the node ids and coordinates of the cell, null when no part was searched. The element id is -1 when the
        // cell is beyond a non negative maxDistance; else the point is moved onto the cell
        public int GetClosestCell(ref double[] point, double maxDistance, string[] selectableParts, out int[] nodeIds,
                                  out double[][] nodeCoordinates)
        {
            CheckLength(point, 3, "point");
            byte[] selectable = selectableParts == null ? null : VtkNativeLibrary.ToUtf8List(selectableParts);
            VtkNativeClosestCell cell = new VtkNativeClosestCell();
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_closest_cell(Handle(), point, maxDistance, selectable,
                                                                              ref cell));
            nodeIds = null;
            nodeCoordinates = null;
            if (cell.NodeCount == 0) return -1;
            //
            nodeIds = new int[cell.NodeCount];
            nodeCoordinates = new double[cell.NodeCount][];
            for (int i = 0; i < cell.NodeCount; i++)
            {
                nodeIds[i] = cell.NodeIds[i];
                nodeCoordinates[i] = new double[] { cell.NodeCoordinates[3 * i], cell.NodeCoordinates[3 * i + 1],
                                                    cell.NodeCoordinates[3 * i + 2] };
            }
            point = new double[] { cell.PointX, cell.PointY, cell.PointZ };
            return cell.ElementId;
        }
        // The distance from the point to the closest cell of each named part and the normal of that cell
        public void GetClosestCellsOnParts(double[] point, string[] partNames, out double[] distances,
                                           out double[][] normals)
        {
            CheckLength(point, 3, "point");
            distances = new double[partNames.Length];
            double[] normalValues = new double[3 * partNames.Length];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_closest_cells_on_parts(Handle(), point,
                VtkNativeLibrary.ToUtf8List(partNames), partNames.Length, distances, normalValues));
            normals = new double[partNames.Length][];
            for (int i = 0; i < partNames.Length; i++)
            {
                double[] normal = new double[] { normalValues[3 * i], normalValues[3 * i + 1], normalValues[3 * i + 2] };
                if (distances[i] >= 0 && (normal[0] != 0 || normal[1] != 0 || normal[2] != 0)) normals[i] = normal;
            }
        }
        public double GetSelectionPrecision(int selectionDistance)
        {
            double precision;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_selection_precision(Handle(), selectionDistance,
                                                                                     out precision));
            return precision;
        }
        public double[] GetPartBounds(string name)
        {
            double[] bounds = new double[6];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_part_bounds(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                             bounds));
            return bounds;
        }
        // The volume grid of a part, which the frustum selection searches; null removes it
        public void SetVolumeGrid(string name, VtkNativeMeshData grid)
        {
            IntPtr handle = Handle();
            byte[] utf8Name = VtkNativeLibrary.ToUtf8(name);
            if (grid == null)
            {
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_volume_grid(handle, utf8Name, IntPtr.Zero));
                return;
            }
            using (VtkNativePinnedMesh pinnedGrid = grid.Pin())
            {
                IntPtr gridPointer = ToUnmanaged(pinnedGrid.Mesh);
                try
                {
                    VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_set_volume_grid(handle, utf8Name, gridPointer));
                }
                finally
                {
                    Marshal.FreeHGlobal(gridPointer);
                }
            }
        }
        // The nodes of the part's volume grid inside the frustum and its elements inside or crossing it
        public void GetIdsInsideFrustum(string name, double[][] planes, int pointCount, int cellCount,
                                        out int[] pointIds, out int[] cellIds)
        {
            double[] planeValues = new double[6 * planes.Length];
            for (int i = 0; i < planes.Length; i++) Array.Copy(planes[i], 0, planeValues, 6 * i, 6);
            int[] insidePointIds = new int[Math.Max(1, pointCount)];
            int[] insideCellIds = new int[Math.Max(1, cellCount)];
            int insidePointCount;
            int insideCellCount;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_get_ids_inside_frustum(Handle(), VtkNativeLibrary.ToUtf8(name),
                planeValues, planes.Length, insidePointIds, insidePointIds.Length, out insidePointCount, insideCellIds,
                insideCellIds.Length, out insideCellCount));
            pointIds = new int[insidePointCount];
            cellIds = new int[insideCellCount];
            Array.Copy(insidePointIds, pointIds, insidePointCount);
            Array.Copy(insideCellIds, cellIds, insideCellCount);
        }
        //
        // The selection layer
        public void AddHighlight(VtkNativeMeshData mesh, VtkNativeHighlightStyle style)
        {
            IntPtr handle = Handle();
            using (VtkNativePinnedMesh pinnedMesh = mesh.Pin())
            {
                VtkNativeMesh nativeMesh = pinnedMesh.Mesh;
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_highlight(handle, ref nativeMesh, ref style));
            }
        }
        public void AddPartHighlight(string name, VtkNativeHighlightStyle style)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_add_part_highlight(Handle(), VtkNativeLibrary.ToUtf8(name),
                                                                                ref style));
        }
        public void ClearHighlights(bool currentOnly)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_view_clear_highlights(Handle(), currentOnly ? 1 : 0));
        }
    }
}
