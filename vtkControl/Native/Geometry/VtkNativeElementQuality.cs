// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using CaeMesh;

namespace vtkControl.Native
{
    // The standard element quality metrics, computed by vtkn_compute_element_quality on the corner nodes of the
    // elements. An element of a type the metric is not defined for, a beam or a point element, gets no value
    public static class VtkNativeElementQuality
    {
        // Methods
        public static bool IsComputed(ElementQualityMetricEnum metric)
        {
            return GetNativeMetric(metric) >= 0;
        }
        public static Dictionary<int, double> Compute(FeMesh mesh, IEnumerable<int> elementIds,
                                                      ElementQualityMetricEnum metric)
        {
            int nativeMetric = GetNativeMetric(metric);
            if (nativeMetric < 0) throw new NotSupportedException("The metric " + metric + " is not a standard metric.");
            //
            Dictionary<int, List<int>> cellTypeElementIds = new Dictionary<int, List<int>>();
            foreach (int elementId in elementIds)
            {
                int cellType = GetLinearCellType(mesh.Elements[elementId].GetVtkCellType());
                if (cellType < 0) continue;
                //
                List<int> ids;
                if (!cellTypeElementIds.TryGetValue(cellType, out ids))
                {
                    ids = new List<int>();
                    cellTypeElementIds.Add(cellType, ids);
                }
                ids.Add(elementId);
            }
            //
            VtkNativeLibrary.EnsureLoaded();
            Dictionary<int, double> elementIdValue = new Dictionary<int, double>();
            foreach (var entry in cellTypeElementIds)
            {
                int cornerCount = GetCornerCount(entry.Key);
                double[] points = new double[entry.Value.Count * cornerCount * 3];
                int count = 0;
                foreach (int elementId in entry.Value)
                {
                    int[] nodeIds = mesh.Elements[elementId].GetVtkNodeIds();
                    for (int i = 0; i < cornerCount; i++)
                    {
                        FeNode node = mesh.Nodes[nodeIds[i]];
                        points[count++] = node.X;
                        points[count++] = node.Y;
                        points[count++] = node.Z;
                    }
                }
                //
                double[] values = new double[entry.Value.Count];
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_compute_element_quality(nativeMetric, entry.Key, points,
                                                                                    values.Length, values));
                for (int i = 0; i < values.Length; i++)
                {
                    if (!double.IsNaN(values[i])) elementIdValue.Add(entry.Value[i], values[i]);
                }
            }
            return elementIdValue;
        }
        // The values of vtkn_quality_metric
        private static int GetNativeMetric(ElementQualityMetricEnum metric)
        {
            switch (metric)
            {
                case ElementQualityMetricEnum.ScaledJacobian: return 0;
                case ElementQualityMetricEnum.AspectRatio: return 1;
                case ElementQualityMetricEnum.EdgeRatio: return 2;
                case ElementQualityMetricEnum.MinimumAngle: return 3;
                case ElementQualityMetricEnum.MaximumAngle: return 4;
                case ElementQualityMetricEnum.Skew: return 5;
                case ElementQualityMetricEnum.Warpage: return 6;
                case ElementQualityMetricEnum.Taper: return 7;
                default: return -1;
            }
        }
        // A parabolic element is measured on its corner nodes, which come first in the VTK node order
        private static int GetLinearCellType(int cellType)
        {
            switch ((vtkCellType)cellType)
            {
                case vtkCellType.VTK_TRIANGLE:
                case vtkCellType.VTK_QUADRATIC_TRIANGLE: return (int)vtkCellType.VTK_TRIANGLE;
                case vtkCellType.VTK_QUAD:
                case vtkCellType.VTK_QUADRATIC_QUAD: return (int)vtkCellType.VTK_QUAD;
                case vtkCellType.VTK_TETRA:
                case vtkCellType.VTK_QUADRATIC_TETRA: return (int)vtkCellType.VTK_TETRA;
                case vtkCellType.VTK_HEXAHEDRON:
                case vtkCellType.VTK_QUADRATIC_HEXAHEDRON: return (int)vtkCellType.VTK_HEXAHEDRON;
                case vtkCellType.VTK_WEDGE:
                case vtkCellType.VTK_QUADRATIC_WEDGE: return (int)vtkCellType.VTK_WEDGE;
                case vtkCellType.VTK_PYRAMID:
                case vtkCellType.VTK_QUADRATIC_PYRAMID: return (int)vtkCellType.VTK_PYRAMID;
                default: return -1;
            }
        }
        private static int GetCornerCount(int linearCellType)
        {
            switch ((vtkCellType)linearCellType)
            {
                case vtkCellType.VTK_TRIANGLE: return 3;
                case vtkCellType.VTK_QUAD: return 4;
                case vtkCellType.VTK_TETRA: return 4;
                case vtkCellType.VTK_HEXAHEDRON: return 8;
                case vtkCellType.VTK_WEDGE: return 6;
                case vtkCellType.VTK_PYRAMID: return 5;
                default: throw new NotSupportedException();
            }
        }
    }
}
