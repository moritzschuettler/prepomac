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
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The detected contact between two exploded view clusters, described by the faces that take part in
    /// it on each side. The interface area is the mean of the two side areas, which is the weight used
    /// when the competing disassembly directions of one cluster are compared.
    /// </summary>
    public class ExplodedViewInterface
    {
        // Variables                                                                                                                
        private int _firstClusterId;
        private int _secondClusterId;
        private List<ExplodedViewFace> _firstFaces;
        private List<ExplodedViewFace> _secondFaces;
        private Vec3D _direction;
        private double _directionConfidence;
        private bool _isAxialDirection;
        private int _shaftClusterId;


        // Properties                                                                                                               
        public int FirstClusterId { get { return _firstClusterId; } }
        public int SecondClusterId { get { return _secondClusterId; } }
        public List<ExplodedViewFace> FirstFaces { get { return _firstFaces; } }
        public List<ExplodedViewFace> SecondFaces { get { return _secondFaces; } }
        /// <summary>
        /// The unit direction along which the second cluster separates from the first one, or null when
        /// the interface faces do not agree on one
        /// </summary>
        public Vec3D Direction { get { return _direction; } }
        /// <summary>
        /// The share of the interface area that agrees with the resolved direction
        /// </summary>
        public double DirectionConfidence { get { return _directionConfidence; } }
        public bool HasDirection { get { return _direction != null; } }
        /// <summary>
        /// True when the direction was taken from the axis of a bore and not from the normal of a plane
        /// </summary>
        public bool IsAxialDirection { get { return _isAxialDirection; } }
        /// <summary>
        /// The cluster that runs through the other one - the one whose curved faces turn their back to the
        /// axis - or -1 when neither of them does. Two parts that both present a hole on the same axis sit
        /// on the same shaft instead of one of them running through the other.
        /// </summary>
        public int ShaftClusterId { get { return _shaftClusterId; } }
        /// <summary>
        /// True when a shaft of one cluster runs through a hole of the other, which is the only axial
        /// contact that mounts one part on the other
        /// </summary>
        public bool IsAxialMount { get { return _isAxialDirection && _shaftClusterId >= 0; } }
        public double Area
        {
            get { return 0.5 * (_firstFaces.Sum(face => face.Area) + _secondFaces.Sum(face => face.Area)); }
        }


        // Constructors                                                                                                             
        public ExplodedViewInterface(int firstClusterId, int secondClusterId)
        {
            _firstClusterId = firstClusterId;
            _secondClusterId = secondClusterId;
            _firstFaces = new List<ExplodedViewFace>();
            _secondFaces = new List<ExplodedViewFace>();
            _direction = null;
            _directionConfidence = 0;
            _isAxialDirection = false;
            _shaftClusterId = -1;
        }


        // Methods                                                                                                                  
        public void AddFacePair(ExplodedViewFace firstFace, ExplodedViewFace secondFace)
        {
            if (!_firstFaces.Contains(firstFace)) _firstFaces.Add(firstFace);
            if (!_secondFaces.Contains(secondFace)) _secondFaces.Add(secondFace);
        }
        public void SetDirection(Vec3D direction, double confidence, bool isAxialDirection, int shaftClusterId)
        {
            _direction = direction;
            _directionConfidence = confidence;
            _isAxialDirection = isAxialDirection;
            _shaftClusterId = shaftClusterId;
        }
        /// <summary>
        /// The unit direction along which the given cluster separates from the other one
        /// </summary>
        public Vec3D GetDirection(int clusterId)
        {
            if (_direction == null) return null;
            //
            if (clusterId == _secondClusterId) return new Vec3D(_direction);
            else if (clusterId == _firstClusterId) return -1 * _direction;
            else throw new NotSupportedException();
        }
        public int GetOtherClusterId(int clusterId)
        {
            if (clusterId == _firstClusterId) return _secondClusterId;
            else if (clusterId == _secondClusterId) return _firstClusterId;
            else throw new NotSupportedException();
        }
    }
}
