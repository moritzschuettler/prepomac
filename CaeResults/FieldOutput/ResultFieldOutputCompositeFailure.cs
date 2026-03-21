using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeResults
{
    [Serializable]
    public class ResultFieldOutputCompositeFailure : ResultFieldOutput
    {
        // Variables
        private string _fieldName;
        private double _xt;
        private double _xc;
        private double _yt;
        private double _yc;
        private double _s12;
        [OptionalField]
        private double _fiberAngleDeg;


        // Properties
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public double Xt { get { return _xt; } set { _xt = CheckPositive(value); } }
        public double Xc { get { return _xc; } set { _xc = CheckPositive(value); } }
        public double Yt { get { return _yt; } set { _yt = CheckPositive(value); } }
        public double Yc { get { return _yc; } set { _yc = CheckPositive(value); } }
        public double S12 { get { return _s12; } set { _s12 = CheckPositive(value); } }
        public double FiberAngleDeg { get { return _fiberAngleDeg; } set { _fiberAngleDeg = value; } }


        // Constructors
        public ResultFieldOutputCompositeFailure(string name, string fieldName,
                                                 double xt, double xc, double yt, double yc, double s12,
                                                 double fiberAngleDeg = 0.0)
            : base(name)
        {
            _fieldName = fieldName;
            _xt = xt;
            _xc = xc;
            _yt = yt;
            _yc = yc;
            _s12 = s12;
            _fiberAngleDeg = fiberAngleDeg;
        }


        // Methods
        private double CheckPositive(double value)
        {
            if (value <= 0) throw new CaeException("Strength values must be greater than 0.");
            return value;
        }
        public override string[] GetParentNames()
        {
            return new string[] { _fieldName };
        }
        public override string[] GetComponentNames()
        {
            return new string[] { FOComponentNames.TsaiHill, FOComponentNames.TsaiWu };
        }
    }
}
