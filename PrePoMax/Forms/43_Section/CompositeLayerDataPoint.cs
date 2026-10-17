// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class CompositeLayerDataPoint
    {
        // Variables                                                                                                                
        private string _materialName;
        private EquationContainer _thickness;
        private EquationContainer _angle;


        // Properties                                                                                                               
        [DisplayName("Material")]
        public string MaterialName { get { return _materialName; } set { _materialName = value; } }
        //
        [DisplayName("Thickness\n[?]")]
        [TypeConverter(typeof(EquationLengthFromConverter))]
        public EquationString ThicknessEq { get { return _thickness.Equation; } set { _thickness.Equation = value; } }
        [Browsable(false)]
        public EquationContainer Thickness { get { return _thickness; } set { _thickness = value; } }
        //
        [DisplayName("Angle\n[?]")]
        [TypeConverter(typeof(EquationAngleDegFromConverter))]
        public EquationString AngleEq { get { return _angle.Equation; } set { _angle.Equation = value; } }
        [Browsable(false)]
        public EquationContainer Angle { get { return _angle; } set { _angle = value; } }


        // Constructors                                                                                                             
        public CompositeLayerDataPoint()
            : this(new CompositeLayer(null, 0, 0))
        {
        }
        public CompositeLayerDataPoint(CompositeLayer layer)
        {
            _materialName = layer.MaterialName;
            _thickness = layer.Thickness;
            _angle = layer.Angle;
        }


        // Methods                                                                                                                  
        public CompositeLayer GetLayer()
        {
            return new CompositeLayer(_materialName, _thickness, _angle);
        }
    }
}
