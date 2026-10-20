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

namespace CaeModel
{
    [Serializable]
    public class CompositeCaeMaterial
    {
        public string Name;
        public string RawName;
        public string Architecture;
        public bool HasMechanicalData;
        public double E1;
        public double E2;
        public double E3;
        public double G12;
        public double G13;
        public double G23;
        public double Nu12;
        public double Nu13;
        public double Nu23;
        public double CuredThickness;
        public double WeightPerArea;
    }

    [Serializable]
    public class CompositeCaeSurface
    {
        public double[][] Nodes;            // [nodeIndex][3]
        public int[][] Elements;            // [elementIndex][nodeIndex ...] - compacted node indices
        public int[][] Triangles;           // [triangleIndex][3] - fan triangulation of Elements
        public BoundingBox Box;

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

    [Serializable]
    public class CompositeCaePly
    {
        public string Name;
        public string RawName;
        public string GroupName;
        public string MaterialName;
        public int LayerId;
        public double Orientation;
        public double Thickness;
        public CompositeCaeSurface Surface;
    }

    [Serializable]
    public class CompositeCaeData
    {
        public string FileName;
        public string FormatVersion;
        public string Vendor;
        public string LengthUnit;
        public string AngleUnit;
        public string MassUnit;
        public List<CompositeCaeMaterial> Materials;
        public List<CompositeCaePly> Plies;
        public List<string> Warnings;

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
