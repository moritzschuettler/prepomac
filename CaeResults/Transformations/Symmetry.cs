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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using Octree;

namespace CaeResults
{
    [PmxType(5061)]
    public enum SymmetryPlaneEnum
    {
        X,
        Y,
        Z
    }

    [PmxType(5062)]
    public class Symmetry : Transformation
    {
        // Variables                                                                                                                
        [PmxKey(0)] protected double[] _pointCoor;
        [PmxKey(1)] protected SymmetryPlaneEnum _symmetryPlane;


        // Properties                                                                                                               
        public double[] PointCoor { get { return _pointCoor; } set { _pointCoor = value; } }
        public SymmetryPlaneEnum SymmetryPlane { get { return _symmetryPlane; } set { _symmetryPlane = value; } }


        // Constructor                                                                                                              
        public Symmetry(string name, double[] pointCoor, SymmetryPlaneEnum symmetryPlane)
            : base(name)
        {
            _pointCoor = pointCoor;
            _symmetryPlane = symmetryPlane;
        }


        // Static methods                                                                                                           


        // Methods                                                                                                                  
       

    }
}

