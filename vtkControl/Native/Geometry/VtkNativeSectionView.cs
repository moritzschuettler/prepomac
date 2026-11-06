// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // Mirrors vtkn_section_view of vtkNativeGeometry.h: the section plane and how its cut faces are drawn
    [StructLayout(LayoutKind.Sequential)]
    public struct VtkNativeSectionView
    {
        // Variables
        public double PointX;
        public double PointY;
        public double PointZ;
        public double NormalX;
        public double NormalY;
        public double NormalZ;
        public int ElementWise;
        public int Hatch;
        public double HatchSpacing;
    }
}
