using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class HyperelasticNeoHooke : MaterialProperty, ISerializable
    {
        // Variables
        private EquationContainer _c10;     //ISerializable
        private EquationContainer _d1;      //ISerializable


        // Properties
        public EquationContainer C10 { get { return _c10; } set { _c10 = value; } }
        public EquationContainer D1 { get { return _d1; } set { _d1 = value; } }


        // Constructors
        public HyperelasticNeoHooke(double c10, double d1)
        {
            _c10 = new EquationContainer(typeof(StringPressureConverter), c10);
            _d1 = new EquationContainer(typeof(StringDoubleConverter), d1);
        }
        public HyperelasticNeoHooke(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_c10": _c10 = (EquationContainer)entry.Value; break;
                    case "_d1": _d1 = (EquationContainer)entry.Value; break;
                    default: break;
                }
            }
        }


        // Methods
        public override void CheckEquations()
        {
            _c10.CheckEquation();
            _d1.CheckEquation();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_c10", _c10, typeof(EquationContainer));
            info.AddValue("_d1", _d1, typeof(EquationContainer));
        }
    }
}
