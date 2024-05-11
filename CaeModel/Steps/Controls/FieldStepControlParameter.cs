using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using CaeJob;

namespace CaeModel
{
    [Serializable]
    public class FieldStepControlParameter : StepControlParameter
    {
        // Variables                                                                                                                
        private double _rna;
        private double _cna;
        private double _q0a;
        private double _qua;
        private double _rpa;
        private double _ea;
        private double _cea;
        private double _rla;


        // Properties                                                                                                               
        public double Rna { get { return _rna; } set { _rna = Math.Max(0, value); } }
        public double Cna { get { return _cna; } set { _cna = Math.Max(0, value); } }
        public double Q0a { get { return _q0a; } set { _q0a = Math.Max(0, value); } }
        public double Qua { get { return _qua; } set { _qua = Math.Max(0, value); } }
        public double Rpa { get { return _rpa; } set { _rpa = Math.Max(0, value); } }
        public double Ea { get { return _ea; } set { _ea = Math.Max(0, value); } }
        public double Cea { get { return _cea; } set { _cea = Math.Max(0, value); } }
        public double Rla { get { return _rla; } set { _rla = Math.Max(0, value); } }


        // Constructors                                                                                                             
        public FieldStepControlParameter()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
            _rna = 0.005;
            _cna = 0.01;
            _q0a = double.NaN;
            _qua = double.NaN;
            _rpa = 0.02;
            _ea = 1E-5;
            _cea = 1E-3;
            _rla = 1E-8;
        }
    }
}

