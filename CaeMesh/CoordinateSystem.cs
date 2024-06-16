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
    public enum CoordinateSystemTypeEnum
    {
        //[StandardValue("Selection", Description = "Selection/Coordinates", DisplayName = "Selection/Coordinates")]
        Rectangular,
        //[StandardValue("BetweenTwoPoints", Description = "Between two points", DisplayName = "Between two points")]
        Cylindrical,
    }

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
                    case "_nameVisible":
                        _nameVisible = (bool)entry.Value; break;
                    case "_twoD":
                        _twoD = (bool)entry.Value; break;
                    case "_color":
                        _color = (Color)entry.Value; break;
                }
            }
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
            Vec3D p1 = new Vec3D(Point1());
            Vec3D p2 = new Vec3D(Point2());
            Vec3D p3 = new Vec3D(Point3());
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
            _nameVisible = true;
            _twoD = false;
            _color = Color.Yellow;
            //
            EquationChanged();
        }
        public void Reset()
        {
            Clear();
        }
        public double[] Center()
        {
            if (_center == null) EquationChanged();
            return _center.Coor;
        }
        public double[] Point1()
        {
            return new double[] { _x1.Value, _y1.Value, _z1.Value };
        }
        public double[] Point2()
        {
            return new double[] { _x2.Value, _y2.Value, _z2.Value };
        }
        public double[] Point3()
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
        public bool IsProperlyDefined()
        {
            EquationChanged();
            if (_dx.Len2 == 0 || _dy.Len2 == 0 || _dz.Len2 == 0) return false;
            else return true;
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
            info.AddValue("_nameVisible", _nameVisible, typeof(bool));
            info.AddValue("_twoD", _twoD, typeof(bool));
            info.AddValue("_color", _color, typeof(Color));
        }
    }
}
