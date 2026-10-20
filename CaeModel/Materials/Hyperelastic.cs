using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class Hyperelastic : MaterialProperty, ISerializable
    {
        // Variables
        private EquationContainer _c10;         //ISerializable
        private EquationContainer _c01;         //ISerializable
        private EquationContainer _d1;          //ISerializable


        // Properties
        public EquationContainer C10 { get { return _c10; } set { SetC10(value); } }
        public EquationContainer C01 { get { return _c01; } set { _c01 = value; } }
        public EquationContainer D1 { get { return _d1; } set { SetD1(value); } }


        // Constructors
        public Hyperelastic(double c10, double c01, double d1)
        {
            SetC10(new EquationContainer(typeof(StringPressureConverter), c10), false);
            _c01 = new EquationContainer(typeof(StringPressureConverter), c01);
            SetD1(new EquationContainer(typeof(StringDoubleConverter), d1), false);
        }
        public Hyperelastic(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_c10":
                        SetC10((EquationContainer)entry.Value, false);
                        break;
                    case "_c01":
                        _c01 = (EquationContainer)entry.Value;
                        break;
                    case "_d1":
                        SetD1((EquationContainer)entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
        }


        // Methods
        private void SetC10(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _c10, value, CheckPositive, checkEquation);
        }
        private void SetD1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _d1, value, CheckNonNegative, checkEquation);
        }
        //
        private double CheckPositive(double value)
        {
            if (value <= 0) throw new CaeException(_positive);
            else return value;
        }
        private double CheckNonNegative(double value)
        {
            if (value < 0) throw new CaeException("The value must be larger than or equal to 0.");
            else return value;
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            _c10.CheckEquation();
            _c01.CheckEquation();
            _d1.CheckEquation();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            //
            info.AddValue("_c10", _c10, typeof(EquationContainer));
            info.AddValue("_c01", _c01, typeof(EquationContainer));
            info.AddValue("_d1", _d1, typeof(EquationContainer));
        }
    }
}
