// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
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
    public class ExplodedViewParameters
    {
        // Variables                                                                                                                
        private ExplodedViewMethodEnum _explodedViewMethod;
        private double[] _center;
        private ExplodedViewDirectionEnum _explodedViewDirection;
        private double _scaleFactor;
        private double _magnification;
        private double _tolerance;
        private bool _sequential;


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
            _sequential = true;
        }
    }
}
