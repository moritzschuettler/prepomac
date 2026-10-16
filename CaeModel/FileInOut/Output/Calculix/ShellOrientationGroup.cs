// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using CaeGlobals;
using CaeMesh;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    // CalculiX takes no distribution of orientations on a shell section, so the shell elements whose material axis 1 and
    // normal agree within about 0.1 degrees share one orientation. A flat region becomes a single group.
    internal class ShellOrientationGroup
    {
        // Variables
        private const double _quantizationStep = 0.001;
        private double[] _axis1;
        private double[] _normal;
        private List<int> _elementIds;


        // Properties
        public double[] Axis1 { get { return _axis1; } }
        public double[] Normal { get { return _normal; } }
        public List<int> ElementIds { get { return _elementIds; } }


        // Constructor
        private ShellOrientationGroup(double[] axis1, double[] normal)
        {
            _axis1 = axis1;
            _normal = normal;
            _elementIds = new List<int>();
        }


        // Static methods
        public static List<ShellOrientationGroup> Create(FeModel model, int[] elementIds,
                                                         Dictionary<int, MaterialOrientation> elementOrientations,
                                                         out List<int> unorientedElementIds)
        {
            unorientedElementIds = new List<int>();
            List<ShellOrientationGroup> groups = new List<ShellOrientationGroup>();
            Dictionary<Tuple<long, long, long, long, long, long>, ShellOrientationGroup> keyGroups =
                new Dictionary<Tuple<long, long, long, long, long, long>, ShellOrientationGroup>();
            //
            int[] sortedIds = (int[])elementIds.Clone();
            Array.Sort(sortedIds);
            //
            bool fallbackUsed;
            double[][] axes;
            MaterialOrientation orientation;
            ShellOrientationGroup group;
            Tuple<long, long, long, long, long, long> key;
            foreach (int elementId in sortedIds)
            {
                axes = null;
                if (elementOrientations.TryGetValue(elementId, out orientation))
                    axes = orientation.GetElementAxes(model.Mesh, model.Mesh.Elements[elementId], 0, out fallbackUsed);
                //
                if (axes == null)
                {
                    unorientedElementIds.Add(elementId);
                    continue;
                }
                //
                key = new Tuple<long, long, long, long, long, long>(Quantize(axes[0][0]), Quantize(axes[0][1]),
                                                                    Quantize(axes[0][2]), Quantize(axes[2][0]),
                                                                    Quantize(axes[2][1]), Quantize(axes[2][2]));
                if (!keyGroups.TryGetValue(key, out group))
                {
                    group = new ShellOrientationGroup(axes[0], axes[2]);
                    keyGroups.Add(key, group);
                    groups.Add(group);
                }
                group._elementIds.Add(elementId);
            }
            return groups;
        }
        private static long Quantize(double value)
        {
            return (long)Math.Round(value / _quantizationStep);
        }


        // Methods
        // Returns axes 1 and 2 of the group turned by the angle about the normal
        public double[][] GetAxes(double angleDeg)
        {
            Vec3D e1 = new Vec3D(_axis1);
            Vec3D n = new Vec3D(_normal);
            if (angleDeg != 0)
            {
                double angle = angleDeg * Math.PI / 180;
                e1 = Math.Cos(angle) * e1 + Math.Sin(angle) * Vec3D.CrossProduct(n, e1);
                e1.Normalize();
            }
            Vec3D e2 = Vec3D.CrossProduct(n, e1);
            e2.Normalize();
            //
            return new double[][] { e1.Coor, e2.Coor };
        }
    }
}
