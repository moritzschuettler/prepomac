using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    public class MomentLoad : Load, ISerializable
    {
        // Variables                                                                                                                
        private string _regionName;                 //ISerializable
        private RegionTypeEnum _regionType;         //ISerializable
        private int _nodeId;                        //ISerializable
        private EquationContainer _m1;              //ISerializable
        private EquationContainer _m2;              //ISerializable
        private EquationContainer _m3;              //ISerializable
        private EquationContainer _magnitude;       //ISerializable


        // Properties                                                                                                               
        public override string RegionName { get { return _regionName; } set { _regionName = value; } }
        public override RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int NodeId { get { return _nodeId; } set { _nodeId = value; } }
        public EquationContainer M1 { get { UpdateEquations(); return _m1; } set { SetM1(value); } }
        public EquationContainer M2 { get { UpdateEquations(); return _m2; } set { SetM2(value); } }
        public EquationContainer M3 { get { UpdateEquations(); return _m3; } set { SetM3(value); } }
        public EquationContainer Magnitude { get { UpdateEquations(); return _magnitude; } set { SetMagnitude(value); } }
        public double GetDirection(int direction)
        {
            if (direction == 0) return M1.Value;
            else if (direction == 1) return M2.Value;
            else if (direction == 2) return M3.Value;
            else throw new NotSupportedException();
        }


        // Constructors                                                                                                             
        public MomentLoad(string name, int nodeId, double m1, double m2, double m3, bool twoD, bool complex, double phaseDeg)
            : this(name, null, RegionTypeEnum.NodeId, m1, m2, m3, twoD, complex, phaseDeg)
        {
            _nodeId = nodeId;   // set the nodeId
        }
        public MomentLoad(string name, string regionName, RegionTypeEnum regionType, double m1, double m2, double m3,
                          bool twoD, bool complex, double phaseDeg)
            : base(name, twoD, complex, phaseDeg) 
        {
            _regionName = regionName;
            RegionType = regionType;
            _nodeId = -1;
            //
            double mag = Math.Sqrt(m1 * m1 + m2 * m2 + m3 * m3);
            M1 = new EquationContainer(typeof(StringMomentConverter), m1);
            M2 = new EquationContainer(typeof(StringMomentConverter), m2);
            M3 = new EquationContainer(typeof(StringMomentConverter), m3);
            Magnitude = new EquationContainer(typeof(StringMomentConverter), mag);
        }
        
        public MomentLoad(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionName":
                        _regionName = (string)entry.Value; break;
                    case "_regionType":
                        _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_nodeId":
                        _nodeId = (int)entry.Value; break;
                    case "_m1":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueM1)
                            M1 = new EquationContainer(typeof(StringMomentConverter), valueM1);
                        else
                            SetM1((EquationContainer)entry.Value, false);
                        break;
                    case "_m2":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueM2)
                            M2 = new EquationContainer(typeof(StringMomentConverter), valueM2);
                        else
                            SetM2((EquationContainer)entry.Value, false);
                        break;
                    case "_m3":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double valueM3)
                            M3 = new EquationContainer(typeof(StringMomentConverter), valueM3);
                        else
                            SetM3((EquationContainer)entry.Value, false);
                        break;
                    case "_magnitude":
                        SetMagnitude((EquationContainer)entry.Value, false); break;
                    default:
                        break;
                }
            }
            // Compatibility for version v1.4.0
            if (_magnitude == null)
            {
                double mag = Math.Sqrt(_m1.Value * _m1.Value + _m2.Value * _m2.Value + _m3.Value * _m3.Value);
                Magnitude = new EquationContainer(typeof(StringMomentConverter), mag);
            }
        }


        // Methods                                                                                                                  
        private void UpdateEquations()
        {
            try
            {
                // If error catch it silently
                if (_m1.IsEquation() || _m2.IsEquation() || _m3.IsEquation()) MEquationChanged();
                else if (_magnitude.IsEquation()) MagnitudeEquationChanged();
            }
            catch (Exception ex) { }
        }
        private void SetM1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _m1, value, Check2D, MEquationChanged, checkEquation);
        }
        private void SetM2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _m2, value, Check2D, MEquationChanged, checkEquation);
        }
        private void SetM3(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _m3, value, null, MEquationChanged, checkEquation);
        }
        private void SetMagnitude(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _magnitude, value, CheckMagnitude, MagnitudeEquationChanged, checkEquation);
        }
        //
        private void MEquationChanged()
        {
            double mag = Math.Sqrt(_m1.Value * _m1.Value + _m2.Value * _m2.Value + _m3.Value * _m3.Value);
            _magnitude.SetEquationFromValue(mag, false);
        }
        private void MagnitudeEquationChanged()
        {
            double c1;
            double c2;
            double c3;
            //
            if (_magnitude.Value == 0)
            {
                c1 = 0;
                c2 = 0;
                c3 = 0;
            }
            else
            {
                double r;
                c1 = _m1.Value;
                c2 = _m2.Value;
                c3 = _m3.Value;
                double mag = Math.Sqrt(c1 * c1 + c2 * c2 + c3 * c3);
                //
                if (mag != 0)
                {
                    r = _magnitude.Value / mag;
                    c1 *= r;
                    c2 *= r;
                    c3 *= r;
                }
                else
                {
                    r = Math.Sqrt(Math.Pow(_magnitude.Value, 2) / 3);
                    c1 = r;
                    c2 = r;
                    c3 = r;
                }
            }
            //
            _m1.SetEquationFromValue(c1, false);
            _m2.SetEquationFromValue(c2, false);
            _m3.SetEquationFromValue(c3, false);
        }
        //
        private double Check2D(double value)
        {
            if (_twoD) return 0;
            else return value;
        }
        private double CheckMagnitude(double value)
        {
            if (value < 0) throw new Exception("Value of the moment load magnitude must be non-negative.");
            else return value;
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _m1.CheckEquation();
            _m2.CheckEquation();
            _m3.CheckEquation();
            _magnitude.CheckEquation();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_regionName", _regionName, typeof(string));
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_nodeId", _nodeId, typeof(int));
            info.AddValue("_m1", _m1, typeof(EquationContainer));
            info.AddValue("_m2", _m2, typeof(EquationContainer));
            info.AddValue("_m3", _m3, typeof(EquationContainer));
            info.AddValue("_magnitude", _magnitude, typeof(EquationContainer));
        }
    }
}
