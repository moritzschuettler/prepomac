// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh
{
    [Serializable]
    public enum IntegrationScaleType
    {
        None,
        Thickness,
        Axisymmetric
    }

    // Out-of-plane scaling applied inside the Gauss loop of an element integral so that a 2D
    // (plane stress / plane strain / axisymmetric) region yields the physically correct
    // 3D-equivalent result:
    // - None:         weight = 1                 (3D or plain geometric integral)
    // - Thickness:    weight = t (constant)      (plane stress / plane strain slab of thickness t)
    // - Axisymmetric: weight = 2 * pi * r        (surface / volume of revolution)
    [Serializable]
    public struct IntegrationScale
    {
        public IntegrationScaleType Type;
        public double Thickness;    // used when Type == Thickness


        // A default(IntegrationScale) has Type == None, so it behaves as an unscaled integral.
        public bool IsUnity { get { return Type == IntegrationScaleType.None; } }


        // Factory methods
        public static IntegrationScale None
        {
            get { return new IntegrationScale() { Type = IntegrationScaleType.None, Thickness = 1.0 }; }
        }
        public static IntegrationScale FromThickness(double thickness)
        {
            return new IntegrationScale() { Type = IntegrationScaleType.Thickness, Thickness = thickness };
        }
        public static IntegrationScale FromAxisymmetric()
        {
            return new IntegrationScale() { Type = IntegrationScaleType.Axisymmetric, Thickness = 1.0 };
        }


        // The axisymmetric weight is the revolution circumference 2 * pi * r, where the radial
        // coordinate r is the X coordinate (the axis of symmetry is the Y axis).
        public double GetWeight(double radialCoor)
        {
            switch (Type)
            {
                case IntegrationScaleType.None: return 1.0;
                case IntegrationScaleType.Thickness: return Thickness;
                case IntegrationScaleType.Axisymmetric: return 2.0 * Math.PI * radialCoor;
                default: throw new NotSupportedException();
            }
        }
    }
}
