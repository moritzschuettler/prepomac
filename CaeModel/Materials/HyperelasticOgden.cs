using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [PmxType(50034)]
    public class HyperelasticOgden : MaterialProperty
    {
        // Variables
        [PmxKey(0)] private int _n;
        [PmxKey(1)] private EquationContainer _mu1;     //ISerializable
        [PmxKey(2)] private EquationContainer _alpha1;  //ISerializable
        [PmxKey(3)] private EquationContainer _d1;      //ISerializable
        [PmxKey(4)] private EquationContainer _mu2;     //ISerializable
        [PmxKey(5)] private EquationContainer _alpha2;  //ISerializable
        [PmxKey(6)] private EquationContainer _d2;      //ISerializable
        [PmxKey(7)] private EquationContainer _mu3;     //ISerializable
        [PmxKey(8)] private EquationContainer _alpha3;  //ISerializable
        [PmxKey(9)] private EquationContainer _d3;      //ISerializable


        // Properties
        public int N { get { return _n; } set { _n = value; } }
        public EquationContainer Mu1 { get { return _mu1; } set { _mu1 = value; } }
        public EquationContainer Alpha1 { get { return _alpha1; } set { _alpha1 = value; } }
        public EquationContainer D1 { get { return _d1; } set { _d1 = value; } }
        public EquationContainer Mu2 { get { return _mu2; } set { _mu2 = value; } }
        public EquationContainer Alpha2 { get { return _alpha2; } set { _alpha2 = value; } }
        public EquationContainer D2 { get { return _d2; } set { _d2 = value; } }
        public EquationContainer Mu3 { get { return _mu3; } set { _mu3 = value; } }
        public EquationContainer Alpha3 { get { return _alpha3; } set { _alpha3 = value; } }
        public EquationContainer D3 { get { return _d3; } set { _d3 = value; } }


        // Constructors
        public HyperelasticOgden(int n, double mu1, double alpha1, double d1,
                                 double mu2, double alpha2, double d2,
                                 double mu3, double alpha3, double d3)
        {
            _n = n;
            _mu1 = new EquationContainer(typeof(StringPressureConverter), mu1);
            _alpha1 = new EquationContainer(typeof(StringDoubleConverter), alpha1);
            _d1 = new EquationContainer(typeof(StringDoubleConverter), d1);
            _mu2 = new EquationContainer(typeof(StringPressureConverter), mu2);
            _alpha2 = new EquationContainer(typeof(StringDoubleConverter), alpha2);
            _d2 = new EquationContainer(typeof(StringDoubleConverter), d2);
            _mu3 = new EquationContainer(typeof(StringPressureConverter), mu3);
            _alpha3 = new EquationContainer(typeof(StringDoubleConverter), alpha3);
            _d3 = new EquationContainer(typeof(StringDoubleConverter), d3);
        }
        public HyperelasticOgden(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_n": _n = (int)entry.Value; break;
                    case "_mu1": _mu1 = (EquationContainer)entry.Value; break;
                    case "_alpha1": _alpha1 = (EquationContainer)entry.Value; break;
                    case "_d1": _d1 = (EquationContainer)entry.Value; break;
                    case "_mu2": _mu2 = (EquationContainer)entry.Value; break;
                    case "_alpha2": _alpha2 = (EquationContainer)entry.Value; break;
                    case "_d2": _d2 = (EquationContainer)entry.Value; break;
                    case "_mu3": _mu3 = (EquationContainer)entry.Value; break;
                    case "_alpha3": _alpha3 = (EquationContainer)entry.Value; break;
                    case "_d3": _d3 = (EquationContainer)entry.Value; break;
                    default: break;
                }
            }
        }


        // Methods
        public override void CheckEquations()
        {
            _mu1.CheckEquation();
            _alpha1.CheckEquation();
            _d1.CheckEquation();
            if (_n >= 2) { _mu2.CheckEquation(); _alpha2.CheckEquation(); _d2.CheckEquation(); }
            if (_n >= 3) { _mu3.CheckEquation(); _alpha3.CheckEquation(); _d3.CheckEquation(); }
        }
    }
}
