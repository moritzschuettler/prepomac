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
    public class TimeIncrementationStepControlParameter : StepControlParameter
    {
        // Variables                                                                                                                
        private int _i0;
        private int _ir;
        private int _ip;
        private int _ic;
        private int _il;
        private int _ig;
        private int _is;
        private int _ia;
        private int _ij;
        private int _it;
        //
        private double _df;
        private double _dc;
        private double _db;
        private double _da;
        private double _ds;
        private double _dh;
        private double _dd;
        private double _wg;


        // Properties                                                                                                               
        public int I0 { get { return _i0; } set { _i0 = Math.Max(1, value); } }
        public int IR { get { return _ir; } set { _ir = Math.Max(1, value); } }
        public int IP { get { return _ip; } set { _ip = Math.Max(1, value); } }
        public int IC { get { return _ic; } set { _ic = Math.Max(1, value); } }
        public int IL { get { return _il; } set { _il = Math.Max(1, value); } }
        public int IG { get { return _ig; } set { _ig = Math.Max(1, value); } }
        public int IS { get { return _is; } set { _is = Math.Max(1, value); } }
        public int IA { get { return _ia; } set { _ia = Math.Max(1, value); } }
        public int IJ { get { return _ij; } set { _ij = Math.Max(1, value); } }
        public int IT { get { return _it; } set { _it = Math.Max(1, value); } }
        //
        public double Df { get { return _df; } set { _df = Math.Max(0, value); } }
        public double DC { get { return _dc; } set { _dc = Math.Max(0, value); } }
        public double DB { get { return _db; } set { _db = Math.Max(0, value); } }
        public double DA { get { return _da; } set { _da = Math.Max(0, value); } }
        public double DS { get { return _ds; } set { _ds = Math.Max(0, value); } }
        public double DH { get { return _dh; } set { _dh = Math.Max(0, value); } }
        public double DD { get { return _dd; } set { _dd = Math.Max(0, value); } }
        public double WG { get { return _wg; } set { _wg = Math.Max(0, value); } }


        // Constructors                                                                                                             
        public TimeIncrementationStepControlParameter()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
            _i0 = 4;
            _ir = 8;
            _ip = 9;
            _ic = 16;
            _il = 10;
            _ig = 4;
            _is = -1;
            _ia = 5;
            _ij = -1;
            _it = -1;
            //
            _df = 0.25;
            _dc = 0.5;
            _db = 0.75;
            _da = 0.85;
            _ds = -1;
            _dh = -1;
            _dd = 1.5;
            _wg = -1;
        }
    }
}

