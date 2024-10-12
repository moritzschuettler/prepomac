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
    [Serializable]
    public abstract class Load : NamedClass, IMultiRegion, IContainsEquations, ISerializable
    {
        // Variables                                                                                                                
        private int[] _creationIds;                                 //ISerializable
        private Selection _creationData;                            //ISerializable
        protected bool _twoD;                                       //ISerializable
        protected string _amplitudeName;                            //ISerializable
        protected string _coordinateSystemName;                     //ISerializable
        protected bool _complex;                                    //ISerializable
        protected EquationContainer _phaseDeg;                      //ISerializable
        protected Color _color;                                     //ISerializable
        public const string DefaultAmplitudeName = "Default";
        public const string DefaultCoordinateSystemName = "Global";


        // Properties                                                                                                               
        public virtual string RegionName { get; set; }
        public virtual RegionTypeEnum RegionType { get; set; }
        public int[] CreationIds { get { return _creationIds; } set { _creationIds = value; } }
        public Selection CreationData { get { return _creationData; } set { _creationData = value; } }
        public bool TwoD { get { return _twoD; } }
        public string AmplitudeName
        {
            get
            {
                if (_amplitudeName == null) return DefaultAmplitudeName;
                else return _amplitudeName;
            }
            set
            {
                _amplitudeName = value;
                if (_amplitudeName == DefaultAmplitudeName) _amplitudeName = null;
            }
        }
        public string CoordinateSystemName
        {
            get
            {
                if (_coordinateSystemName == null) return DefaultCoordinateSystemName;
                else return _coordinateSystemName;
            }
            set
            {
                _coordinateSystemName = value;
                if (_coordinateSystemName == DefaultCoordinateSystemName) _coordinateSystemName = null;
            }
        }
        public bool Complex { get { return _complex; } set { _complex = value; } }
        public EquationContainer PhaseDeg { get { return _phaseDeg; } set { SetPhaseDeg(value); } }
        public Color Color
        {
            get
            {
                // Compatibility for version v0.6.0
                if (_color == Color.Empty) _color = Color.RoyalBlue;
                //
                return _color;
            }
            set { _color = value; }
        }


        // Constructors                                                                                                             
        public Load(string name, bool twoD)
            : this(name, twoD, false, 0)
        { }
        public Load(string name, bool twoD, bool complex, double phaseDeg, bool constant = false)
            : base(name) 
        {
            _creationIds = null;
            _creationData = null;
            _twoD = twoD;
            _amplitudeName = null;
            _coordinateSystemName = null;
            _complex = complex;
            PhaseDeg = new EquationContainer(typeof(StringAngleDegConverter), phaseDeg, null, constant);
            _color = Color.RoyalBlue;
        }
        public Load(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Compatibility for version v1.4.0
            PhaseDeg = new EquationContainer(typeof(StringAngleDegConverter), 0);
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_creationIds":
                    case "Load+_creationIds":       // Compatibility for version v1.4.0
                        _creationIds = (int[])entry.Value; break;
                    case "_creationData":
                    case "Load+_creationData":      // Compatibility for version v1.4.0
                        _creationData = (Selection)entry.Value; break;
                    case "_twoD":
                    case "Load+_twoD":              // Compatibility for version v1.4.0
                        _twoD = (bool)entry.Value; break;
                    case "_amplitudeName":
                    case "Load+_amplitudeName":     // Compatibility for version v1.4.0
                        _amplitudeName = (string)entry.Value; break;
                    case "_coordinateSystemName":
                        _coordinateSystemName = (string)entry.Value; break;
                    case "_complex":
                    case "Load+_complex":           // Compatibility for version v1.4.0
                        _complex = (bool)entry.Value; break;
                    case "_phaseDeg":
                    case "Load+_phaseDeg":          // Compatibility for version v1.4.0
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valuePhase)
                            PhaseDeg = new EquationContainer(typeof(StringAngleDegConverter), valuePhase);
                        else
                            SetPhaseDeg((EquationContainer)entry.Value, false);
                        break;
                    case "_color":
                    case "Load+_color":             // Compatibility for version v1.4.0
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
        //
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
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_creationIds", _creationIds, typeof(int[]));
            info.AddValue("_creationData", _creationData, typeof(Selection));
            info.AddValue("_twoD", _twoD, typeof(bool));
            info.AddValue("_amplitudeName", _amplitudeName, typeof(string));
            info.AddValue("_coordinateSystemName", _coordinateSystemName, typeof(string));
            info.AddValue("_complex", _complex, typeof(bool));
            info.AddValue("_phaseDeg", _phaseDeg, typeof(EquationContainer));
            info.AddValue("_color", _color, typeof(Color));
        }
    }
}
