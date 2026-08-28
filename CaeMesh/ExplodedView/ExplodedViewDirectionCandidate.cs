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
    /// <summary>
    /// One disassembly direction proposed by a single interface face, weighted by the face area.
    ///
    /// A candidate coming from a curved face is an axis candidate. A bore constrains two degrees of
    /// freedom while a mating plane constrains only one, so an axis candidate outranks a plane candidate
    /// whenever the two disagree.
    ///
    /// An axis candidate also carries the radius of the surface it was recognized from and the number of
    /// faces that were merged into it. When one part sits in another over bores at an angle, the direction
    /// the most of them agree on is the one the part comes off along: a row of screw holes holds it more
    /// firmly than a single wider bore across them. Only when as many bores agree either way does the wider
    /// of them decide, the narrower one being a dowel or a screw hole that follows it.
    /// </summary>
    public class ExplodedViewDirectionCandidate
    {
        // Variables                                                                                                                
        private Vec3D _direction;
        private double _weight;
        private bool _isAxis;
        private double _radius;
        private int _faceCount;


        // Properties                                                                                                               
        public Vec3D Direction { get { return _direction; } }
        public double Weight { get { return _weight; } set { _weight = value; } }
        public bool IsAxis { get { return _isAxis; } }
        public double Radius { get { return _radius; } set { _radius = value; } }
        public int FaceCount { get { return _faceCount; } set { _faceCount = value; } }


        // Constructors                                                                                                             
        public ExplodedViewDirectionCandidate(Vec3D direction, double weight, bool isAxis, double radius)
        {
            _direction = direction;
            _weight = weight;
            _isAxis = isAxis;
            _radius = radius;
            _faceCount = 1;
        }
    }
}
