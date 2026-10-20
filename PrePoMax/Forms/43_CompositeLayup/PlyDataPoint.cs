// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors: Yvan Blanchard (Composites Fiber Modeler)

using System;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    // A row of the ply table of the composite layup form
    [Serializable]
    public class PlyDataPoint
    {
        // Variables                                                                                                                
        private string _name;
        private string _regionName;
        private string _materialName;
        private EquationContainer _thickness;
        private EquationContainer _angle;
        private string _coordinateSystemName;


        // Properties                                                                                                               
        [DisplayName("Name")]
        public string Name { get { return _name; } set { _name = value; } }
        //
        [DisplayName("Region")]
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        //
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
        //
        [DisplayName("Coordinate system")]
        public string CoordinateSystemName
        {
            get { return _coordinateSystemName ?? Ply.LayupCoordinateSystemName; }
            set { _coordinateSystemName = value == Ply.LayupCoordinateSystemName ? null : value; }
        }


        // Constructors                                                                                                             
        public PlyDataPoint()
            : this(new Ply(null, null, null, 0, 0))
        {
        }
        public PlyDataPoint(Ply ply)
        {
            _name = ply.Name;
            _regionName = ply.RegionName;
            _materialName = ply.MaterialName;
            _thickness = ply.Thickness;
            _angle = ply.Angle;
            _coordinateSystemName = ply.CoordinateSystemName;
        }


        // Methods                                                                                                                  
        public Ply GetPly()
        {
            Ply ply = new Ply(_name, _regionName, _materialName, _thickness, _angle);
            ply.CoordinateSystemName = _coordinateSystemName;
            return ply;
        }
    }
}
