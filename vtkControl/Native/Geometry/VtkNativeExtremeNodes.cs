// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Runtime.InteropServices;
using CaeGlobals;

namespace vtkControl.Native
{
    // Mirrors vtkn_extreme_nodes of vtkNativeGeometry.h: the nodes with the smallest and the largest value
    [StructLayout(LayoutKind.Sequential)]
    public struct VtkNativeExtremeNodes
    {
        // Variables
        public int MinId;
        public int MaxId;
        public double MinX;
        public double MinY;
        public double MinZ;
        public double MaxX;
        public double MaxY;
        public double MaxZ;
        public double MinValue;
        public double MaxValue;


        // Static methods
        public static VtkNativeExtremeNodes FromNodesExchangeData(NodesExchangeData extremeNodes)
        {
            VtkNativeExtremeNodes extremes = new VtkNativeExtremeNodes();
            extremes.MinId = extremeNodes.Ids[0];
            extremes.MaxId = extremeNodes.Ids[1];
            extremes.MinX = extremeNodes.Coor[0][0];
            extremes.MinY = extremeNodes.Coor[0][1];
            extremes.MinZ = extremeNodes.Coor[0][2];
            extremes.MaxX = extremeNodes.Coor[1][0];
            extremes.MaxY = extremeNodes.Coor[1][1];
            extremes.MaxZ = extremeNodes.Coor[1][2];
            extremes.MinValue = extremeNodes.Values[0];
            extremes.MaxValue = extremeNodes.Values[1];
            return extremes;
        }
    }
}
