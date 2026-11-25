// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using CaeMesh;
using CaeGlobals;

namespace CaeModel
{
    [PmxType(50001)]
    public class CompositeCaeMaterial
    {
        [PmxKey(0)] public string Name;
        [PmxKey(1)] public string RawName;
        [PmxKey(2)] public string Architecture;
        [PmxKey(3)] public bool HasMechanicalData;
        [PmxKey(4)] public double E1;
        [PmxKey(5)] public double E2;
        [PmxKey(6)] public double E3;
        [PmxKey(7)] public double G12;
        [PmxKey(8)] public double G13;
        [PmxKey(9)] public double G23;
        [PmxKey(10)] public double Nu12;
        [PmxKey(11)] public double Nu13;
        [PmxKey(12)] public double Nu23;
        [PmxKey(13)] public double CuredThickness;
        [PmxKey(14)] public double WeightPerArea;
    }

    [PmxType(50002)]
    public class CompositeCaeSurface
    {
        [PmxKey(0)] public double[][] Nodes;            // [nodeIndex][3]
        [PmxKey(1)] public int[][] Elements;            // [elementIndex][nodeIndex ...] - compacted node indices
        [PmxKey(2)] public int[][] Triangles;           // [triangleIndex][3] - fan triangulation of Elements
        [PmxKey(3)] public BoundingBox Box;

        public int TriangleCount { get { return Triangles == null ? 0 : Triangles.Length; } }

        public double GetArea()
        {
            double area = 0;
            if (Triangles == null) return area;
            //
            foreach (int[] triangle in Triangles)
            {
                double[] v0 = Nodes[triangle[0]];
                double[] v1 = Nodes[triangle[1]];
                double[] v2 = Nodes[triangle[2]];
                //
                double e1x = v1[0] - v0[0], e1y = v1[1] - v0[1], e1z = v1[2] - v0[2];
                double e2x = v2[0] - v0[0], e2y = v2[1] - v0[1], e2z = v2[2] - v0[2];
                //
                double nx = e1y * e2z - e1z * e2y;
                double ny = e1z * e2x - e1x * e2z;
                double nz = e1x * e2y - e1y * e2x;
                //
                area += 0.5 * Math.Sqrt(nx * nx + ny * ny + nz * nz);
            }
            return area;
        }
    }

    [PmxType(50003)]
    public class CompositeCaePly
    {
        [PmxKey(0)] public string Name;
        [PmxKey(1)] public string RawName;
        [PmxKey(2)] public string GroupName;
        [PmxKey(3)] public string MaterialName;
        [PmxKey(4)] public int LayerId;
        [PmxKey(5)] public double Orientation;
        [PmxKey(6)] public double Thickness;
        [PmxKey(7)] public CompositeCaeSurface Surface;
    }

    [PmxType(50004)]
    public class CompositeCaeData
    {
        [PmxKey(0)] public string FileName;
        [PmxKey(1)] public string FormatVersion;
        [PmxKey(2)] public string Vendor;
        [PmxKey(3)] public string LengthUnit;
        [PmxKey(4)] public string AngleUnit;
        [PmxKey(5)] public string MassUnit;
        [PmxKey(6)] public List<CompositeCaeMaterial> Materials;
        [PmxKey(7)] public List<CompositeCaePly> Plies;
        [PmxKey(8)] public List<string> Warnings;

        public CompositeCaeData()
        {
            Materials = new List<CompositeCaeMaterial>();
            Plies = new List<CompositeCaePly>();
            Warnings = new List<string>();
        }

        // The sum of the thicknesses of the layers of the stack
        public double GetTotalThickness()
        {
            double sum = 0;
            HashSet<int> countedLayers = new HashSet<int>();
            foreach (CompositeCaePly ply in Plies)
            {
                if (countedLayers.Add(ply.LayerId)) sum += ply.Thickness;
            }
            return sum;
        }
        // The average normal of the ply surfaces; the direction in which the plies are stacked
        public double[] GetStackDirection()
        {
            double[] sum = new double[3];
            //
            foreach (CompositeCaePly ply in Plies)
            {
                CompositeCaeSurface surface = ply.Surface;
                if (surface == null || surface.TriangleCount == 0) continue;
                //
                foreach (int[] triangle in surface.Triangles)
                {
                    double[] v0 = surface.Nodes[triangle[0]];
                    double[] v1 = surface.Nodes[triangle[1]];
                    double[] v2 = surface.Nodes[triangle[2]];
                    //
                    double e1x = v1[0] - v0[0], e1y = v1[1] - v0[1], e1z = v1[2] - v0[2];
                    double e2x = v2[0] - v0[0], e2y = v2[1] - v0[1], e2z = v2[2] - v0[2];
                    //
                    sum[0] += e1y * e2z - e1z * e2y;
                    sum[1] += e1z * e2x - e1x * e2z;
                    sum[2] += e1x * e2y - e1y * e2x;
                }
            }
            //
            double length = Math.Sqrt(sum[0] * sum[0] + sum[1] * sum[1] + sum[2] * sum[2]);
            if (length < 1E-12) return new double[] { 0, 0, 1 };
            //
            return new double[] { sum[0] / length, sum[1] / length, sum[2] / length };
        }
        // The default core sampling drill depth: the extent of the stack in the stacking direction plus one
        // ply thickness. It does not rely on the layer ids of the file.
        public double GetAutoDrillDepth()
        {
            double[] direction = GetStackDirection();
            double min = double.MaxValue;
            double max = double.MinValue;
            double maxThickness = 0;
            //
            foreach (CompositeCaePly ply in Plies)
            {
                if (ply.Thickness > maxThickness) maxThickness = ply.Thickness;
                if (ply.Surface == null || ply.Surface.Nodes == null) continue;
                //
                foreach (double[] node in ply.Surface.Nodes)
                {
                    double value = node[0] * direction[0] + node[1] * direction[1] + node[2] * direction[2];
                    if (value < min) min = value;
                    if (value > max) max = value;
                }
            }
            //
            if (min > max) return GetBoundingBox().GetDiagonal();
            //
            double depth = (max - min) + maxThickness;
            if (depth <= 0) depth = GetBoundingBox().GetDiagonal();
            return depth;
        }
        public int GetLayerCount()
        {
            HashSet<int> layers = new HashSet<int>();
            foreach (CompositeCaePly ply in Plies) layers.Add(ply.LayerId);
            return layers.Count;
        }
        public int GetTriangleCount()
        {
            int count = 0;
            foreach (CompositeCaePly ply in Plies) count += ply.Surface.TriangleCount;
            return count;
        }
        public BoundingBox GetBoundingBox()
        {
            BoundingBox box = new BoundingBox();
            foreach (CompositeCaePly ply in Plies) box.IncludeBox(ply.Surface.Box);
            return box;
        }
        public CompositeCaeMaterial GetMaterial(string name)
        {
            foreach (CompositeCaeMaterial material in Materials)
            {
                if (material.Name == name) return material;
            }
            return null;
        }
    }
}
