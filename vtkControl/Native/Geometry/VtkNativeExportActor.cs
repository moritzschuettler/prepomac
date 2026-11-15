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
    // Mirrors vtkn_export_actor of vtkNativeGeometry.h: the pointers belong to the native view until its next export
    [StructLayout(LayoutKind.Sequential)]
    internal struct VtkNativeExportActor
    {
        // Variables
        public IntPtr Name;
        public double ColorR;
        public double ColorG;
        public double ColorB;
        public double ColorA;
        public int Wire;
        public int ColorContours;
        public IntPtr Coordinates;
        public int PointCount;
        public IntPtr Triangles;
        public int TriangleCount;
        public IntPtr Values;
        public IntPtr ElementEdgeCoordinates;
        public int ElementEdgePointCount;
        public IntPtr ElementEdges;
        public int ElementEdgeCount;
        public IntPtr ModelEdgeCoordinates;
        public int ModelEdgePointCount;
        public IntPtr ModelEdges;
        public int ModelEdgeCount;
    }
}
