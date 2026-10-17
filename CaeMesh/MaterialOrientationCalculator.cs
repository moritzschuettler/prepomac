// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace CaeMesh
{
    // Computes the material axes of a shell element the way CalculiX does: the reference axis is projected onto the
    // shell at the natural coordinates xi = eta = 0 - the centre of a quadrilateral and the first node of a triangle -
    // and axis 3 is the shell normal given by the right-hand rule on the node order. A reference axis closer than
    // 0.1 degrees to the normal is replaced by the z axis of the coordinate system, or by its x axis when the
    // reference axis is the z axis.
    public static class MaterialOrientationCalculator
    {
        // Variables                                                                                                                
        public const double ParallelToleranceDeg = 0.1;


        // Methods                                                                                                                  
        public static bool IsShellElement(FeElement element)
        {
            return element is LinearTriangleElement || element is ParabolicTriangleElement ||
                   element is LinearQuadrilateralElement || element is ParabolicQuadrilateralElement;
        }
        public static bool TryGetShellPointAndNormal(FeMesh mesh, FeElement element, out double[] point, out double[] normal)
        {
            point = null;
            normal = null;
            if (!IsShellElement(element)) return false;
            //
            Vec3D[] x = new Vec3D[element.NodeIds.Length];
            for (int i = 0; i < x.Length; i++) x[i] = new Vec3D(mesh.Nodes[element.NodeIds[i]].Coor);
            //
            Vec3D p;
            Vec3D dXi;
            Vec3D dEta;
            if (element is LinearQuadrilateralElement)
            {
                p = 0.25 * (x[0] + x[1] + x[2] + x[3]);
                dXi = 0.25 * (x[1] + x[2] - x[0] - x[3]);
                dEta = 0.25 * (x[2] + x[3] - x[0] - x[1]);
            }
            else if (element is ParabolicQuadrilateralElement)
            {
                p = 0.5 * (x[4] + x[5] + x[6] + x[7]) - 0.25 * (x[0] + x[1] + x[2] + x[3]);
                dXi = 0.5 * (x[5] - x[7]);
                dEta = 0.5 * (x[6] - x[4]);
            }
            else if (element is LinearTriangleElement)
            {
                p = x[0];
                dXi = x[1] - x[0];
                dEta = x[2] - x[0];
            }
            else
            {
                p = x[0];
                dXi = 4 * x[3] - 3 * x[0] - x[1];
                dEta = 4 * x[5] - 3 * x[0] - x[2];
            }
            Vec3D n = Vec3D.CrossProduct(dXi, dEta);
            if (n.Normalize() == 0) return false;
            //
            point = p.Coor;
            normal = n.Coor;
            return true;
        }
        // Returns the axes 1, 2 and 3 of the material at the point of a shell with the given normal. The coordinate
        // system null is the global one. The angle rotates axis 1 about the normal.
        public static double[][] GetAxes(double[] point, double[] normal, CoordinateSystem coordinateSystem,
                                         MaterialOrientationAxisEnum referenceAxis, double rotationAngleDeg,
                                         out bool fallbackUsed)
        {
            Vec3D[] csAxes = GetCoordinateSystemAxes(point, coordinateSystem);
            Vec3D n = new Vec3D(normal);
            n.Normalize();
            //
            Vec3D a = csAxes[(int)referenceAxis];
            double cosTolerance = Math.Cos(ParallelToleranceDeg * Math.PI / 180);
            fallbackUsed = Math.Abs(Vec3D.DotProduct(a, n)) / a.Len > cosTolerance;
            if (fallbackUsed) a = referenceAxis == MaterialOrientationAxisEnum.Z ? csAxes[0] : csAxes[2];
            //
            Vec3D e1 = a - Vec3D.DotProduct(a, n) * n;
            e1.Normalize();
            if (rotationAngleDeg != 0)
            {
                double angle = rotationAngleDeg * Math.PI / 180;
                e1 = Math.Cos(angle) * e1 + Math.Sin(angle) * Vec3D.CrossProduct(n, e1);
                e1.Normalize();
            }
            Vec3D e2 = Vec3D.CrossProduct(n, e1);
            e2.Normalize();
            //
            return new double[][] { e1.Coor, e2.Coor, n.Coor };
        }
        private static Vec3D[] GetCoordinateSystemAxes(double[] point, CoordinateSystem coordinateSystem)
        {
            if (coordinateSystem == null)
                return new Vec3D[] { new Vec3D(1, 0, 0), new Vec3D(0, 1, 0), new Vec3D(0, 0, 1) };
            //
            Vec3D dx = coordinateSystem.DirectionX(point);
            Vec3D dy = coordinateSystem.DirectionY(point, dx);
            Vec3D dz = coordinateSystem.DirectionZ(point);
            return new Vec3D[] { new Vec3D(dx), new Vec3D(dy), new Vec3D(dz) };
        }
    }
}
