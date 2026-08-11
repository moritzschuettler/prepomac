// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class CyclicSymmetry : Tie, IMultiRegion, ISerializable // ISerializable must be here
    {
        // Variables                                                                                                                
        private string _regionName;                         //ISerializable
        private RegionTypeEnum _regionType;                 //ISerializable
        private int[] _creationIds;                         //ISerializable
        private Selection _creationData;                    //ISerializable
        private int _numberOfSectors;                      //ISerializable
        private int _numberOfGraphSectors;                 //ISerializable
        private bool _checkSectorAngle;                    //ISerializable
        private EquationContainer _x;                      //ISerializable
        private EquationContainer _y;                      //ISerializable
        private EquationContainer _z;                      //ISerializable
        private EquationContainer _n1;                     //ISerializable
        private EquationContainer _n2;                     //ISerializable
        private EquationContainer _n3;                     //ISerializable


        // Properties                                                                                                               
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        public RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public int NumberOfSectors { get { return _numberOfSectors; } set { SetNumberOfSectors(value); } }
        public int NumberOfGraphSectors
        {
            get { return _numberOfGraphSectors; }
            set { SetNumberOfGraphSectors(value); }
        }
        public bool CheckSectorAngle { get { return _checkSectorAngle; } set { _checkSectorAngle = value; } }
        public EquationContainer X { get { return _x; } set { SetX(value); } }
        public EquationContainer Y { get { return _y; } set { SetY(value); } }
        public EquationContainer Z { get { return _z; } set { SetZ(value); } }
        public EquationContainer N1 { get { return _n1; } set { SetN1(value); } }
        public EquationContainer N2 { get { return _n2; } set { SetN2(value); } }
        public EquationContainer N3 { get { return _n3; } set { SetN3(value); } }
        public double SectorAngleDeg { get { return 360.0 / _numberOfSectors; } }


        // Constructors                                                                                                             
        public CyclicSymmetry(string name, string masterSurfaceName, RegionTypeEnum masterRegionType,
                              string slaveSurfaceName, RegionTypeEnum slaveRegionType, bool twoD)
            : this(name, double.NaN, true, 2, 1, true, new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 },
                   masterSurfaceName, masterRegionType, slaveSurfaceName, slaveRegionType, twoD)
        {
        }
        public CyclicSymmetry(string name, double positionTolerance, bool adjust, int numberOfSectors,
                              int numberOfGraphSectors, bool checkSectorAngle, double[] axisPoint,
                              double[] axisDirection, string masterSurfaceName, RegionTypeEnum masterRegionType,
                              string slaveSurfaceName, RegionTypeEnum slaveRegionType, bool twoD)
            : base(name, positionTolerance, adjust, masterSurfaceName, masterRegionType,
                   slaveSurfaceName, slaveRegionType, twoD)
        {
            if (twoD) throw new CaeException("Cyclic symmetry is supported only for 3D models.");
            if (axisPoint == null || axisPoint.Length != 3)
                throw new ArgumentException("The cyclic symmetry axis point must contain three components.", nameof(axisPoint));
            if (axisDirection == null || axisDirection.Length != 3)
                throw new ArgumentException("The cyclic symmetry axis direction must contain three components.",
                                            nameof(axisDirection));
            //
            _regionName = "";
            _regionType = RegionTypeEnum.Selection;
            _creationIds = null;
            _creationData = null;
            //
            _numberOfGraphSectors = 1;
            NumberOfSectors = numberOfSectors;
            NumberOfGraphSectors = numberOfGraphSectors;
            _checkSectorAngle = checkSectorAngle;
            //
            X = new EquationContainer(typeof(StringLengthConverter), axisPoint[0]);
            Y = new EquationContainer(typeof(StringLengthConverter), axisPoint[1]);
            Z = new EquationContainer(typeof(StringLengthConverter), axisPoint[2]);
            //
            N1 = new EquationContainer(typeof(StringDoubleConverter), axisDirection[0]);
            N2 = new EquationContainer(typeof(StringDoubleConverter), axisDirection[1]);
            N3 = new EquationContainer(typeof(StringDoubleConverter), axisDirection[2]);
            CheckAxisDirection();
        }
        public CyclicSymmetry(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Defaults make the object tolerant of fields added in later versions.
            _regionName = "";
            _regionType = RegionTypeEnum.Selection;
            _creationIds = null;
            _creationData = null;
            _numberOfSectors = 2;
            _numberOfGraphSectors = 1;
            _checkSectorAngle = true;
            X = new EquationContainer(typeof(StringLengthConverter), 0);
            Y = new EquationContainer(typeof(StringLengthConverter), 0);
            Z = new EquationContainer(typeof(StringLengthConverter), 0);
            N1 = new EquationContainer(typeof(StringDoubleConverter), 0);
            N2 = new EquationContainer(typeof(StringDoubleConverter), 0);
            N3 = new EquationContainer(typeof(StringDoubleConverter), 1);
            //
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
                    case "_numberOfSectors":
                        SetNumberOfSectors((int)entry.Value); break;
                    case "_numberOfGraphSectors":
                        SetNumberOfGraphSectors((int)entry.Value); break;
                    case "_checkSectorAngle":
                        _checkSectorAngle = (bool)entry.Value; break;
                    case "_x":
                        SetX(GetEquationContainer(entry.Value, typeof(StringLengthConverter)), false); break;
                    case "_y":
                        SetY(GetEquationContainer(entry.Value, typeof(StringLengthConverter)), false); break;
                    case "_z":
                        SetZ(GetEquationContainer(entry.Value, typeof(StringLengthConverter)), false); break;
                    case "_n1":
                        SetN1(GetEquationContainer(entry.Value, typeof(StringDoubleConverter)), false); break;
                    case "_n2":
                        SetN2(GetEquationContainer(entry.Value, typeof(StringDoubleConverter)), false); break;
                    case "_n3":
                        SetN3(GetEquationContainer(entry.Value, typeof(StringDoubleConverter)), false); break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private static EquationContainer GetEquationContainer(object value, Type converterType)
        {
            if (value is double valueDouble) return new EquationContainer(converterType, valueDouble);
            else return (EquationContainer)value;
        }
        private void SetNumberOfSectors(int value)
        {
            if (value < 2) throw new CaeException("The number of cyclic sectors must be at least 2.");
            //
            _numberOfSectors = value;
            if (_numberOfGraphSectors > value) _numberOfGraphSectors = value;
        }
        private void SetNumberOfGraphSectors(int value)
        {
            if (value < 1 || value > _numberOfSectors)
                throw new CaeException("The number of displayed sectors must be between 1 and the number of cyclic sectors.");
            //
            _numberOfGraphSectors = value;
        }
        private void SetX(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _x, value, null, checkEquation);
        }
        private void SetY(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _y, value, null, checkEquation);
        }
        private void SetZ(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _z, value, null, checkEquation);
        }
        private void SetN1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n1, value, null, checkEquation);
        }
        private void SetN2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n2, value, null, checkEquation);
        }
        private void SetN3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _n3, value, null, checkEquation);
        }
        private void CheckAxisDirection()
        {
            double lengthSquared = _n1.Value * _n1.Value + _n2.Value * _n2.Value + _n3.Value * _n3.Value;
            if (double.IsNaN(lengthSquared) || double.IsInfinity(lengthSquared) || lengthSquared <= 1E-24)
                throw new CaeException("The cyclic symmetry axis direction must be non-zero.");
        }
        public double[] GetAxisPoint()
        {
            return new double[] { _x.Value, _y.Value, _z.Value };
        }
        public double[] GetAxisDirection()
        {
            return new double[] { _n1.Value, _n2.Value, _n3.Value };
        }
        public double[] GetSecondAxisPoint()
        {
            return new double[] { _x.Value + _n1.Value, _y.Value + _n2.Value, _z.Value + _n3.Value };
        }
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _x.CheckEquation();
            _y.CheckEquation();
            _z.CheckEquation();
            _n1.CheckEquation();
            _n2.CheckEquation();
            _n3.CheckEquation();
            //
            CheckAxisDirection();
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
            info.AddValue("_numberOfSectors", _numberOfSectors, typeof(int));
            info.AddValue("_numberOfGraphSectors", _numberOfGraphSectors, typeof(int));
            info.AddValue("_checkSectorAngle", _checkSectorAngle, typeof(bool));
            info.AddValue("_x", _x, typeof(EquationContainer));
            info.AddValue("_y", _y, typeof(EquationContainer));
            info.AddValue("_z", _z, typeof(EquationContainer));
            info.AddValue("_n1", _n1, typeof(EquationContainer));
            info.AddValue("_n2", _n2, typeof(EquationContainer));
            info.AddValue("_n3", _n3, typeof(EquationContainer));
        }
    }
}
