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
    public class ContactStepControlParameter : StepControlParameter
    {
        // Variables                                                                                                                
        private double _delcon;
        private double _alea;
        private int _kscalemax;
        private int _itf2f;


        // Properties                                                                                                               
        public double Delcon { get { return _delcon; } set { _delcon = Math.Max(0, value); } }
        public double Alea { get { return _alea; } set { _alea = Math.Min(1, Math.Max(0, value)); } }
        public int Kscalemax { get { return _kscalemax; } set { _kscalemax = Math.Max(1, value); } }
        public int Itf2f { get { return _itf2f; } set { _itf2f = Math.Max(1, value); } }


        // Constructors                                                                                                             
        public ContactStepControlParameter()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
            _delcon = 0.001;
            _alea = 0.1;
            _kscalemax = 100;
            _itf2f = 60;
        }
    }
}

