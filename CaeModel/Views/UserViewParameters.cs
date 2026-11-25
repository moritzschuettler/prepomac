// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeModel
{
    [PmxType(3283)]
    public class UserViewParameters : NamedClass
    {
        // Variables                                                                                                                
        [PmxKey(0)] private double[] _position;
        [PmxKey(1)] private double[] _focalPoint;
        [PmxKey(2)] private double[] _upVector;
        [PmxKey(3)] private double _parallelScale;


        // Properties                                                                                                               
        public double[] Position { get { return _position; } set { _position = value; } }
        public double[] FocalPoint { get { return _focalPoint; } set { _focalPoint = value; } }
        public double[] UpVector { get { return _upVector; } set { _upVector = value; } }
        public double ParallelScale { get { return _parallelScale; } set { _parallelScale = value; } }


        // Constructors                                                                                                             
        public UserViewParameters(string name)
            : base(name)
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void Reset()
        {
            _position = null;
            _focalPoint = null;
            _upVector = null;
            _parallelScale = -1;
        }
    }
}