// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;
using CaeGlobals;
using CaeMesh;

namespace CaeModel
{
    [Serializable]
    public class MaterialOrientation : NamedClass, IMultiRegion, IContainsEquations, ISerializable
    {
        // Variables
        public const int DefaultSymbolDensity = 50;
        private string _regionName;                                 //ISerializable
        private RegionTypeEnum _regionType;                         //ISerializable
        private int[] _creationIds;                                 //ISerializable
        private Selection _creationData;                            //ISerializable
        private string _coordinateSystemName;                       //ISerializable
        private MaterialOrientationAxisEnum _referenceAxis;         //ISerializable
        private EquationContainer _rotationAngle;                   //ISerializable
        private int _symbolDensity;                                 //ISerializable


        // Properties
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
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
        public MaterialOrientationAxisEnum ReferenceAxis { get { return _referenceAxis; } set { _referenceAxis = value; } }
        public EquationContainer RotationAngle { get { return _rotationAngle; } set { SetRotationAngle(value); } }
        // The percentage of the full density of the material axes symbols, which does not depend on the mesh size
        public int SymbolDensity
        {
            get { return _symbolDensity; }
            set
            {
                _symbolDensity = value;
                if (_symbolDensity < 0) _symbolDensity = 0;
                else if (_symbolDensity > 100) _symbolDensity = 100;
            }
        }


        // Constructors
        public MaterialOrientation(string name, string regionName, RegionTypeEnum regionType)
            : base(name)
        {
            _regionName = regionName;
            _regionType = regionType;
            _creationIds = null;
            _creationData = null;
            _coordinateSystemName = null;
            _referenceAxis = MaterialOrientationAxisEnum.X;
            RotationAngle = new EquationContainer(typeof(StringAngleDegConverter), 0);
            _symbolDensity = DefaultSymbolDensity;
        }
        public MaterialOrientation(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _symbolDensity = DefaultSymbolDensity;
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionName":
                        _regionName = (string)entry.Value; break;
                    case "_regionType":
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_creationIds":
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                        _creationData = (Selection)entry.Value; break;
                    case "_coordinateSystemName":
                        _coordinateSystemName = (string)entry.Value; break;
                    case "_referenceAxis":
                        _referenceAxis = (MaterialOrientationAxisEnum)entry.Value; break;
                    case "_rotationAngle":
                        SetRotationAngle((EquationContainer)entry.Value, false); break;
                    case "_symbolDensity":
                        _symbolDensity = (int)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        private void SetRotationAngle(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _rotationAngle, value, CheckFinite, checkEquation);
        }
        private double CheckFinite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new CaeException("The rotation angle must be finite.");
            else return value;
        }
        // Returns the material axes 1, 2 and 3 of a shell element, or null for an element that is not a shell
        public double[][] GetElementAxes(FeMesh mesh, FeElement element, double additionalAngleDeg, out bool fallbackUsed)
        {
            fallbackUsed = false;
            double[] point;
            double[] normal;
            if (!MaterialOrientationCalculator.TryGetShellPointAndNormal(mesh, element, out point, out normal)) return null;
            //
            CoordinateSystem coordinateSystem = null;
            if (_coordinateSystemName != null) coordinateSystem = mesh.CoordinateSystems[_coordinateSystemName];
            return MaterialOrientationCalculator.GetAxes(point, normal, coordinateSystem, _referenceAxis,
                                                         _rotationAngle.Value + additionalAngleDeg, out fallbackUsed);
        }
        // IContainsEquations
        public void CheckEquations()
        {
            _rotationAngle.CheckEquation();
        }
        public bool TryCheckEquations()
        {
            try
            {
                CheckEquations();
                return true;
            }
            catch (Exception) { return false; }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_regionName", _regionName, typeof(string));
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_creationIds", _creationIds, typeof(int[]));
            info.AddValue("_creationData", _creationData, typeof(Selection));
            info.AddValue("_coordinateSystemName", _coordinateSystemName, typeof(string));
            info.AddValue("_referenceAxis", _referenceAxis, typeof(MaterialOrientationAxisEnum));
            info.AddValue("_rotationAngle", _rotationAngle, typeof(EquationContainer));
            info.AddValue("_symbolDensity", _symbolDensity, typeof(int));
        }
    }
}
