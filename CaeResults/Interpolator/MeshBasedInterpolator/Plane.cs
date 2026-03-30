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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeResults
{
    public struct Plane
    {
        // Variables                                                                                                                
        public Vec3D Point;
        public Vec3D Direction;


        // Constructors                                                                                                             
        public Plane(Vec3D point, Vec3D direction)
        {
            Point = point;
            Direction = direction;
        }
        public Plane(Plane plane)
        {
            Point = plane.Point.DeepCopy();
            Direction = plane.Direction.DeepCopy();
        }


        // Methods                                                                                                                  
        public bool IsAbove(Vec3D q)
        { 
            return Vec3D.DotProduct(Direction, q - Point) > 0;
        }
        public Vec3D Project(Vec3D pointToProject)
        {
            Vec3D v = pointToProject - Point;
            Direction.Normalize();
            Vec3D d = Direction * Vec3D.DotProduct(v, Direction);
            Vec3D projectedPoint = pointToProject - d;
            return projectedPoint;
        }
        public Plane DeepCopy()
        {
            return new Plane(this);
        }
    }
}
