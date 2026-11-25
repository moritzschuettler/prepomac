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
using System.Drawing;
using System.Runtime.Serialization;

namespace CaeModel
{
    [PmxType(3207)]
    public class BoundaryCondition : NamedClass, IMultiRegion, IContainsEquations
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _regionName;                             //ISerializable
        [PmxKey(1)] private RegionTypeEnum _regionType;                     //ISerializable
        [PmxKey(2)] private int[] _creationIds;                             //ISerializable
        [PmxKey(3)] private Selection _creationData;                        //ISerializable
        [PmxKey(4)] protected bool _twoD;                                   //ISerializable
        [PmxKey(5)] protected string _amplitudeName;                        //ISerializable
        [PmxKey(6)] protected string _coordinateSystemName;                 //ISerializable
        [PmxKey(7)] protected bool _complex;                                //ISerializable
        [PmxKey(8)] protected EquationContainer _phaseDeg;                  //ISerializable
        [PmxKey(9)] protected Color _color;                                 //ISerializable


        // Properties                                                                                                               
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public bool TwoD { get { return _twoD; } }
        public string AmplitudeName
        {
            get
            {
                if (_amplitudeName == null) return Amplitude.DefaultAmplitudeName;
                else return _amplitudeName;
            }
            set
            {
                _amplitudeName = value;
                if (_amplitudeName == Amplitude.DefaultAmplitudeName) _amplitudeName = null;
            }
        }
        public string CoordinateSystemName
        {
            get
            {
                if (_coordinateSystemName == null) return CoordinateSystem.DefaultCoordinateSystemName;
                else return _coordinateSystemName;
            }
            set
            {
                _coordinateSystemName = value;
                if (_coordinateSystemName == CoordinateSystem.DefaultCoordinateSystemName) _coordinateSystemName = null;
            }
        }
        public bool Complex { get { return _complex; } set { _complex = value; } }
        public EquationContainer PhaseDeg { get { return _phaseDeg; } set { SetPhaseDeg(value); } }
        public Color Color
        {
            get
            {
                // Compatibility for version v0.6.0
                if (_color == Color.Empty) _color = Color.Lime;
                //
                return _color;
            }
            set { _color = value; }
        }


        // Constructors                                                                                                             
        public BoundaryCondition(string name, string regionName, RegionTypeEnum regionType, bool twoD,
                                 bool complex, double phaseDeg, bool constant = false)
            : base(name) 
        {
            _regionName = regionName;
            _regionType = regionType;
            _creationIds = null;
            _creationData = null;
            _twoD = twoD;
            _amplitudeName = null;
            _coordinateSystemName = null;
            _complex = complex;
            PhaseDeg = new EquationContainer(typeof(StringAngleDegConverter), phaseDeg, null, constant);
            _color = Color.Lime;
        }
        public BoundaryCondition(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionName":
                    case "BoundaryCondition+_regionName":               // Compatibility for version v1.4.0
                        _regionName = (string)entry.Value; break;
                    case "_regionType":
                    case "BoundaryCondition+_regionType":               // Compatibility for version v1.4.0
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_creationIds":
                    case "BoundaryCondition+_creationIds":              // Compatibility for version v1.4.0
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                    case "BoundaryCondition+_creationData":             // Compatibility for version v1.4.0
                        _creationData = (Selection)entry.Value; break;
                    case "_twoD":
                    case "BoundaryCondition+_twoD":                     // Compatibility for version v1.4.0
                        _twoD = (bool)entry.Value; break;
                    case "_amplitudeName":
                    case "BoundaryCondition+_amplitudeName":            // Compatibility for version v1.4.0
                        _amplitudeName = (string)entry.Value; break;
                    case "_coordinateSystemName":
                        _coordinateSystemName = (string)entry.Value; break;
                    case "_complex":
                    case "BoundaryCondition+_complex":                  // Compatibility for version v1.4.0
                        _complex = (bool)entry.Value; break;
                    case "_phaseDeg":
                    case "BoundaryCondition+_phaseDeg":                 // Compatibility for version v1.4.0
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valuePhase)
                            PhaseDeg = new EquationContainer(typeof(StringAngleDegConverter), valuePhase);
                        else
                            SetPhaseDeg((EquationContainer)entry.Value, false);
                        break;
                    case "_color":
                    case "BoundaryCondition+_color":                    // Compatibility for version v1.4.0
                        _color = (Color)entry.Value; break;
                    default:
                        break;
                }
            }
            // Compatibility for version v1.3.5
            if (_phaseDeg == null)
            {
                PhaseDeg = new EquationContainer(typeof(StringAngleDegConverter), 0);
            }
        }


        // Methods                                                                                                                  
        [OnDeserialized]
        private void RestoreEquationCallbacks(StreamingContext context)
        {
            SetPhaseDeg(_phaseDeg, false);
        }
        private void SetPhaseDeg(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _phaseDeg, value, CheckAngle, checkEquation);
        }
        //
        public Vec3D GetDirectionX(CoordinateSystem coordinateSystem, double[] coor = null)
        {
            Vec3D direction = new Vec3D(1, 0, 0);
            if (coordinateSystem != null) direction = coordinateSystem.DirectionX(coor);
            return direction;
        }
        public Vec3D GetDirectionY(CoordinateSystem coordinateSystem, double[] coor = null)
        {
            Vec3D direction = new Vec3D(0, 1, 0);
            if (coordinateSystem != null) direction = coordinateSystem.DirectionY(coor);
            return direction;
        }
        public Vec3D GetDirectionZ(CoordinateSystem coordinateSystem, double[] coor = null)
        {
            Vec3D direction = new Vec3D(0, 0, 1);
            if (coordinateSystem != null) direction = coordinateSystem.DirectionZ(coor);
            return direction;
        }
        private double CheckAngle(double value)
        {
            return Tools.GetPhase360(value);
        }
        // IContainsEquations
        public virtual void CheckEquations()
        {
            _phaseDeg.CheckEquation();
        }
        public virtual bool TryCheckEquations()
        {
            try
            {
                CheckEquations();
                return true;
            }
            catch (Exception ex) { return false; }
        }
    }
}
