using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Runtime.Serialization;
using System.Drawing;
using System.Net.Configuration;

namespace CaeMesh
{
    [Serializable]
    public enum CsPointCreatedFromEnum
    {
        [StandardValue("Coordinates", Description = "Coordinates", DisplayName = "Coordinates")]
        Coordinates,
        [StandardValue("OnPoint", Description = "On point", DisplayName = "On point")]
        OnPoint,
        [StandardValue("BetweenTwoPoints", Description = "Between two points", DisplayName = "Between two points")]
        BetweenTwoPoints,
        [StandardValue("CircleCenter", Description = "Circle center by 3 points", DisplayName = "Circle center by 3 points")]
        CircleCenter
    }
    //
    [Serializable]
    public enum CoordinateSystemTypeEnum
    {
        //[StandardValue("Selection", Description = "Selection/Coordinates", DisplayName = "Selection/Coordinates")]
        Rectangular,
        //[StandardValue("BetweenTwoPoints", Description = "Between two points", DisplayName = "Between two points")]
        Cylindrical,
    }
    //
    [Serializable]
    public class CoordinateSystem : NamedClass, ISerializable, IContainsEquations
    {
        // Variables                                                                                                                
        private CoordinateSystemTypeEnum _type;                 //ISerializable
        private EquationContainer _x1;                          //ISerializable
        private EquationContainer _y1;                          //ISerializable
        private EquationContainer _z1;                          //ISerializable
        private EquationContainer _x2;                          //ISerializable
        private EquationContainer _y2;                          //ISerializable
        private EquationContainer _z2;                          //ISerializable
        private EquationContainer _x3;                          //ISerializable
        private EquationContainer _y3;                          //ISerializable
        private EquationContainer _z3;                          //ISerializable
        private CsPointCreatedFromEnum _centerCreatedFrom;      //ISerializable
        private CsPointCreatedFromEnum _pointXCreatedFrom;      //ISerializable
        private CsPointCreatedFromEnum _pointXYCreatedFrom;     //ISerializable
        private int[] _centerCreationIds;                       //ISerializable
        private Selection _centerCreationData;                  //ISerializable
        private int[] _pointXCreationIds;                       //ISerializable
        private Selection _pointXCreationData;                  //ISerializable
        private int[] _pointXYCreationIds;                      //ISerializable
        private Selection _pointXYCreationData;                 //ISerializable
        private bool _nameVisible;                              //ISerializable
        private bool _twoD;                                     //ISerializable
        private Color _color;                                   //ISerializable
        [NonSerialized] private Vec3D _center;
        [NonSerialized] private Vec3D _dx;
        [NonSerialized] private Vec3D _dy;
        [NonSerialized] private Vec3D _dz;


        // Properties                                                                                                               
        public CoordinateSystemTypeEnum Type { get { return _type; } set { _type = value; } }
        public EquationContainer X1 { get { return _x1; } set { SetX1(value); } }
        public EquationContainer Y1 { get { return _y1; } set { SetY1(value); } }
        public EquationContainer Z1 { get { return _z1; } set { SetZ1(value); } }
        public EquationContainer X2 { get { return _x2; } set { SetX2(value); } }
        public EquationContainer Y2 { get { return _y2; } set { SetY2(value); } }
        public EquationContainer Z2 { get { return _z2; } set { SetZ2(value); } }
        public EquationContainer X3 { get { return _x3; } set { SetX3(value); } }
        public EquationContainer Y3 { get { return _y3; } set { SetY3(value); } }
        public EquationContainer Z3 { get { return _z3; } set { SetZ3(value); } }
        public CsPointCreatedFromEnum CenterCreatedFrom
        {
            get { return _centerCreatedFrom; }
            set
            {
                if (_centerCreatedFrom != value)
                {
                    ClearCenterRegionData();
                    _centerCreatedFrom = value;
                }
            }
        }
        public CsPointCreatedFromEnum PointXCreatedFrom
        {
            get { return _pointXCreatedFrom; }
            set
            {
                if (_pointXCreatedFrom != value)
                {
                    ClearPointXRegionData();
                    _pointXCreatedFrom = value;
                }
            }
        }
        public CsPointCreatedFromEnum PointXYCreatedFrom
        {
            get { return _pointXYCreatedFrom; }
            set
            {
                if (_pointXYCreatedFrom != value)
                {
                    ClearPointXYRegionData();
                    _pointXYCreatedFrom = value;
                }
            }
        }
        public int[] CenterCreationIds { get { return _centerCreationIds; } set { _centerCreationIds = value; } }
        public Selection CenterCreationData { get { return _centerCreationData; } set { _centerCreationData = value; } }
        public int[] PointXCreationIds { get { return _pointXCreationIds; } set { _pointXCreationIds = value; } }
        public Selection PointXCreationData { get { return _pointXCreationData; } set { _pointXCreationData = value; } }
        public int[] PointXYCreationIds { get { return _pointXYCreationIds; } set { _pointXYCreationIds = value; } }
        public Selection PointXYCreationData { get { return _pointXYCreationData; } set { _pointXYCreationData = value; } }
        public bool NameVisible { get { return _nameVisible; } set { _nameVisible = value; } }
        public bool TwoD { get { return _twoD; } }
        public Color Color { get { return _color; } set { _color = value; } }


        // Constructors                                                                                                             
        public CoordinateSystem(string name, bool twoD)
            : base(name)
        {
            Clear();
            //
            _twoD = twoD;
        }
        public CoordinateSystem(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_type":
                        _type = (CoordinateSystemTypeEnum)entry.Value; break;
                    case "_x1":
                        SetX1((EquationContainer)entry.Value, false); break;
                    case "_y1":
                        SetY1((EquationContainer)entry.Value, false); break;
                    case "_z1":
                        SetZ1((EquationContainer)entry.Value, false); break;
                    case "_x2":
                        SetX2((EquationContainer)entry.Value, false); break;
                    case "_y2":
                        SetY2((EquationContainer)entry.Value, false); break;
                    case "_z2":
                        SetZ2((EquationContainer)entry.Value, false); break;
                    case "_x3":
                        SetX3((EquationContainer)entry.Value, false); break;
                    case "_y3":
                        SetY3((EquationContainer)entry.Value, false); break;
                    case "_z3":
                        SetZ3((EquationContainer)entry.Value, false); break;
                    case "_centerCreatedFrom":
                        _centerCreatedFrom = (CsPointCreatedFromEnum)entry.Value; break;
                    case "_pointXCreatedFrom":
                        _pointXCreatedFrom = (CsPointCreatedFromEnum)entry.Value; break;
                    case "_pointXYCreatedFrom":
                        _pointXYCreatedFrom = (CsPointCreatedFromEnum)entry.Value; break;
                    case "_centerCreationIds":
                        _centerCreationIds = (int[])entry.Value; break;
                    case "_centerCreationData":
                        _centerCreationData = (Selection)entry.Value; break;
                    case "_pointXCreationIds":
                        _pointXCreationIds = (int[])entry.Value; break;
                    case "_pointXCreationData":
                        _pointXCreationData = (Selection)entry.Value; break;
                    case "_pointXYCreationIds":
                        _pointXYCreationIds = (int[])entry.Value; break;
                    case "_pointXYCreationData":
                        _pointXYCreationData = (Selection)entry.Value; break;
                    case "_nameVisible":
                        _nameVisible = (bool)entry.Value; break;
                    case "_twoD":
                        _twoD = (bool)entry.Value; break;
                    case "_color":
                        _color = (Color)entry.Value; break;
                }
            }
            // Compatibility for version v2.1.2
            if (_centerCreationData == null) _centerCreatedFrom = CsPointCreatedFromEnum.Coordinates;
            if (_pointXCreationData == null) _pointXCreatedFrom = CsPointCreatedFromEnum.Coordinates;
            if (_pointXYCreationData == null) _pointXYCreatedFrom = CsPointCreatedFromEnum.Coordinates;
        }


        // Methods                                                                                                                  
        private void SetX1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _x1, value, null, EquationChanged, checkEquation);
        }
        private void SetY1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _y1, value, null, EquationChanged, checkEquation);
        }
        private void SetZ1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _z1, value, Check2D, EquationChanged, checkEquation);
        }
        private void SetX2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _x2, value, null, EquationChanged, checkEquation);
        }
        private void SetY2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _y2, value, null, EquationChanged, checkEquation);
        }
        private void SetZ2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _z2, value, Check2D, EquationChanged, checkEquation);
        }
        private void SetX3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _x3, value, null, EquationChanged, checkEquation);
        }
        private void SetY3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _y3, value, null, EquationChanged, checkEquation);
        }
        private void SetZ3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _z3, value, Check2D, EquationChanged, checkEquation);
        }
        //
        private double Check2D(double value)
        {
            if (_twoD) return 0;
            else return value;
        }
        private void EquationChanged()
        {
            Vec3D p1 = new Vec3D(new double[] { _x1.Value, _y1.Value, _z1.Value }); // _center might be null
            Vec3D p2 = new Vec3D(PointX());
            Vec3D p3 = new Vec3D(PointXY());
            // Center
            _center = p1;
            // Direction x
            _dx = p2 - p1;
            _dx.Normalize();
            // Direction z
            Vec3D d3 = p3 - p1;
            _dz = Vec3D.CrossProduct(_dx, d3);
            _dz.Normalize();
            // Direction y
            _dy = Vec3D.CrossProduct(_dz, _dx);
            _dy.Normalize();
        }

        // IContainsEquations
        public void CheckEquations()
        {
            _x1.CheckEquation();
            _y1.CheckEquation();
            _z1.CheckEquation();
            _x2.CheckEquation();
            _y2.CheckEquation();
            _z2.CheckEquation();
            _x3.CheckEquation();
            _y3.CheckEquation();
            _z3.CheckEquation();
        }
        public bool TryCheckEquations()
        {
            try
            {
                CheckEquations();
                return true;
            }
            catch (Exception ex) { return false; }
        }
        //
        private void Clear()
        {
            if (_x1 == null) _x1 = new EquationContainer(typeof(StringLengthConverter), 0);
            else _x1.SetEquationFromValue(0);
            if (_y1 == null) _y1 = new EquationContainer(typeof(StringLengthConverter), 0);
            else _y1.SetEquationFromValue(0);
            if (_z1 == null) _z1 = new EquationContainer(typeof(StringLengthConverter), 0);
            else _z1.SetEquationFromValue(0);
            //
            if (_x2 == null) _x2 = new EquationContainer(typeof(StringLengthConverter), 1);
            else _x2.SetEquationFromValue(1);
            if (_y2 == null) _y2 = new EquationContainer(typeof(StringLengthConverter), 0);
            else _y2.SetEquationFromValue(0);
            if (_z2 == null) _z2 = new EquationContainer(typeof(StringLengthConverter), 0);
            else _z2.SetEquationFromValue(0);
            //
            if (_x3 == null) _x3 = new EquationContainer(typeof(StringLengthConverter), 0);
            else _x3.SetEquationFromValue(0);
            if (_y3 == null) _y3 = new EquationContainer(typeof(StringLengthConverter), 1);
            else _y3.SetEquationFromValue(1);
            if (_z3 == null) _z3 = new EquationContainer(typeof(StringLengthConverter), 0);
            else _z3.SetEquationFromValue(0);
            //
            ClearCenterRegionData();
            ClearPointXRegionData();
            ClearPointXYRegionData();
            //
            _nameVisible = true;
            _twoD = false;
            _color = Color.Yellow;
            //
            EquationChanged();
        }
        private void ClearCenterRegionData()
        {
            _centerCreatedFrom = CsPointCreatedFromEnum.Coordinates;
            //
            if (_x1.IsEquation()) _x1.SetEquationFromValue(_x1.Value);
            if (_y1.IsEquation()) _y1.SetEquationFromValue(_y1.Value);
            if (_z1.IsEquation()) _z1.SetEquationFromValue(_z1.Value);
            //
            _centerCreationIds = null;
            _centerCreationData = null;
        }
        private void ClearPointXRegionData()
        {
            _pointXCreatedFrom = CsPointCreatedFromEnum.Coordinates;
            //
            if (_x2.IsEquation()) _x2.SetEquationFromValue(_x2.Value);
            if (_y2.IsEquation()) _y2.SetEquationFromValue(_y2.Value);
            if (_z2.IsEquation()) _z2.SetEquationFromValue(_z2.Value);
            //
            _pointXCreationIds = null;
            _pointXCreationData = null;
        }
        private void ClearPointXYRegionData()
        {
            _pointXYCreatedFrom = CsPointCreatedFromEnum.Coordinates;
            //
            if (_x3.IsEquation()) _x3.SetEquationFromValue(_x3.Value);
            if (_y3.IsEquation()) _y3.SetEquationFromValue(_y3.Value);
            if (_z3.IsEquation()) _z3.SetEquationFromValue(_z3.Value);
            //
            _pointXYCreationIds = null;
            _pointXYCreationData = null;
        }
        public void Reset()
        {
            Clear();
        }
        public Vec3D Center()
        {
            if (_center == null) EquationChanged();
            return _center;
        }
        public double[] PointX()
        {
            return new double[] { _x2.Value, _y2.Value, _z2.Value };
        }
        public double[] PointXY()
        {
            return new double[] { _x3.Value, _y3.Value, _z3.Value };
        }
        public Vec3D DirectionX(double[] coor = null)
        {
            if (_dx == null) EquationChanged();
            if (coor == null || coor.Length != 3 || (coor[0] == 0 && coor[1] == 0 && coor[2] == 0) ||
                _type == CoordinateSystemTypeEnum.Rectangular) return _dx;
            // Cylindrical point not equal to (0, 0, 0)
            else
            {
                Vec3D p1 = new Vec3D(Center());
                Vec3D p2 = new Vec3D(coor);
                Vec3D dx = p2 - p1;
                double k = Vec3D.DotProduct(dx, _dz); // project on x-y plane
                Vec3D projDx = dx - k * _dz;
                projDx.Normalize();
                return projDx;
            }
        }
        public Vec3D DirectionY(double[] coor = null, Vec3D dx = null)
        {
            if (_dy == null) EquationChanged();
            if (coor == null || coor.Length != 3 || (coor[0] == 0 && coor[1] == 0 && coor[2] == 0) ||
                _type == CoordinateSystemTypeEnum.Rectangular) return _dy;
            // Cylindrical point not equal to (0, 0, 0)
            else
            {
                if (dx == null) dx = DirectionX(coor);
                Vec3D dy = Vec3D.CrossProduct(_dz, dx);
                dy.Normalize();
                return dy;
            }
        }
        public Vec3D DirectionZ(double[] coor = null)
        {
            if (_dz == null) EquationChanged();
            return _dz;
        }
        public bool IsProperlyDefined(out string error)
        {
            error = null;
            try
            {
                EquationChanged();
                //
                if (_dx.Len2 == 0 || _dy.Len2 == 0 || _dz.Len2 == 0)
                    throw new CaeException("One of the directions is not properly defined. " +
                                           "The selected points must not be colinear.");
                //
                if ((_centerCreatedFrom == CsPointCreatedFromEnum.OnPoint &&
                    (_centerCreationIds == null || _centerCreationIds.Length != 1)) ||
                    (_centerCreatedFrom == CsPointCreatedFromEnum.BetweenTwoPoints &&
                    (_centerCreationIds == null || _centerCreationIds.Length != 2)) ||
                    (_centerCreatedFrom == CsPointCreatedFromEnum.CircleCenter &&
                    (_centerCreationIds == null || _centerCreationIds.Length != 3)))
                    throw new CaeException("The selection of the coordinate system center point is not complete.");
                if ((_pointXCreatedFrom == CsPointCreatedFromEnum.OnPoint &&
                    (_pointXCreationIds == null || _pointXCreationIds.Length != 1)) ||
                    (_pointXCreatedFrom == CsPointCreatedFromEnum.BetweenTwoPoints &&
                    (_pointXCreationIds == null || _pointXCreationIds.Length != 2)) ||
                    (_pointXCreatedFrom == CsPointCreatedFromEnum.CircleCenter &&
                    (_pointXCreationIds == null || _pointXCreationIds.Length != 3)))
                    throw new CaeException("The selection of coordinate system point in the 1st axis direction is not complete.");
                if ((_pointXYCreatedFrom == CsPointCreatedFromEnum.OnPoint &&
                    (_pointXYCreationIds == null || _pointXYCreationIds.Length != 1)) ||
                    (_pointXYCreatedFrom == CsPointCreatedFromEnum.BetweenTwoPoints &&
                    (_pointXYCreationIds == null || _pointXYCreationIds.Length != 2)) ||
                    (_pointXYCreatedFrom == CsPointCreatedFromEnum.CircleCenter &&
                    (_pointXYCreationIds == null || _pointXYCreationIds.Length != 3)))
                    throw new CaeException("The selection of coordinate system point in 1-2 plane is not complete.");
                //
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_type", _type, typeof(CoordinateSystemTypeEnum));
            info.AddValue("_x1", _x1, typeof(EquationContainer));
            info.AddValue("_y1", _y1, typeof(EquationContainer));
            info.AddValue("_z1", _z1, typeof(EquationContainer));
            info.AddValue("_x2", _x2, typeof(EquationContainer));
            info.AddValue("_y2", _y2, typeof(EquationContainer));
            info.AddValue("_z2", _z2, typeof(EquationContainer));
            info.AddValue("_x3", _x3, typeof(EquationContainer));
            info.AddValue("_y3", _y3, typeof(EquationContainer));
            info.AddValue("_z3", _z3, typeof(EquationContainer));
            info.AddValue("_centerCreatedFrom", _centerCreatedFrom, typeof(CsPointCreatedFromEnum));
            info.AddValue("_pointXCreatedFrom", _pointXCreatedFrom, typeof(CsPointCreatedFromEnum));
            info.AddValue("_pointXYCreatedFrom", _pointXYCreatedFrom, typeof(CsPointCreatedFromEnum));
            info.AddValue("_centerCreationIds", _centerCreationIds, typeof(int[]));
            info.AddValue("_centerCreationData", _centerCreationData, typeof(Selection));
            info.AddValue("_pointXCreationIds", _pointXCreationIds, typeof(int[]));
            info.AddValue("_pointXCreationData", _pointXCreationData, typeof(Selection));
            info.AddValue("_pointXYCreationIds", _pointXYCreationIds, typeof(int[]));
            info.AddValue("_pointXYCreationData", _pointXYCreationData, typeof(Selection));
            info.AddValue("_nameVisible", _nameVisible, typeof(bool));
            info.AddValue("_twoD", _twoD, typeof(bool));
            info.AddValue("_color", _color, typeof(Color));
        }
    }
}
