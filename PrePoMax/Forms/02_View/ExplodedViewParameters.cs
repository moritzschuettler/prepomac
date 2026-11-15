// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    [PmxType(10313)]
    public enum ExplodedViewDirectionEnum
    {
        XYZ = 0,
        X = 1,
        Y = 2,
        Z = 3,
        XY = 4,
        XZ = 5,
        YZ = 6
    }
    //
    [Serializable]
    [PmxType(10314)]
    public enum ExplodedViewMethodEnum
    {
        [StandardValue("Disassembly", "Disassembly direction exploded view method. Each part is moved the " +
                                      "way it would be taken off: along the normal of the mating face or " +
                                      "along the axis of the bore that holds it.")]
        Disassembly = 0,
        [StandardValue("Assembly center", "Assembly center exploded view method. Each part is moved away " +
                                          "from the center of the assembly until it clears the parts that " +
                                          "are already spaced out.")]
        AssemblyCenter = 1,
        [StandardValue("Center point", "Center point exploded view method. Each part is moved away from the " +
                                       "entered point, the further the further away it already is.")]
        CenterPoint = 2
    }
    //
    [Serializable]
    [PmxType(10315)]
    public class ExplodedViewParameters
    {
        // Variables                                                                                                                
        [PmxKey(0)] private ExplodedViewMethodEnum _explodedViewMethod;
        [PmxKey(1)] private double[] _center;
        [PmxKey(2)] private ExplodedViewDirectionEnum _explodedViewDirection;
        [PmxKey(3)] private double _scaleFactor;
        [PmxKey(4)] private double _magnification;
        [PmxKey(5)] private double _tolerance;
        [PmxKey(6)] private bool _sequential;


        // Properties                                                                                                               
        public ExplodedViewMethodEnum Method
        {
            get { return _explodedViewMethod; }
            set { _explodedViewMethod = value; }
        }
        public double[] Center { get { return _center; } set { _center = value; } }
        public double CenterX { get { return _center[0]; } set { _center[0] = value; } }
        public double CenterY { get { return _center[1]; } set { _center[1] = value; } }
        public double CenterZ { get { return _center[2]; } set { _center[2] = value; } }
        public ExplodedViewDirectionEnum Direction
        {
            get { return _explodedViewDirection; }
            set { _explodedViewDirection = value; }
        }
        public double ScaleFactor { get { return _scaleFactor; } set { _scaleFactor = value; } }
        public double Magnification { get { return _magnification; } set { _magnification = value; } }
        /// <summary>
        /// The distance below which two part faces are considered to be in contact by the disassembly
        /// method; zero means the automatic value
        /// </summary>
        public double Tolerance { get { return _tolerance; } set { _tolerance = value; } }
        /// <summary>
        /// Move the parts one level of the assembly after the other instead of all of them at the same time
        /// </summary>
        public bool Sequential { get { return _sequential; } set { _sequential = value; } }
        

        // Constructors                                                                                                             
        public ExplodedViewParameters()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public void Reset()
        {
            _explodedViewMethod = ExplodedViewMethodEnum.Disassembly;
            _center = new double[3];
            _explodedViewDirection = ExplodedViewDirectionEnum.XYZ;
            _scaleFactor = -1;
            _magnification = 2;
            _tolerance = 0;
            _sequential = false;
        }
    }
}
