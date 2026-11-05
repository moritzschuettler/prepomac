// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
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
    }
}
