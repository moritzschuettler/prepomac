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
    [PmxType(50037)]
    public class LineSearchStepControlParameter : StepControlParameter
    {
        // Variables
        [PmxKey(0)] private double _slsMax;
        [PmxKey(1)] private double _slsMin;


        // Properties
        public double SlsMax { get { return _slsMax; } set { _slsMax = value; } }
        public double SlsMin { get { return _slsMin; } set { _slsMin = value; } }


        // Constructors
        public LineSearchStepControlParameter()
        {
            Reset();
        }


        // Methods
        public override void Reset()
        {
            _slsMax = 1.01;
            _slsMin = 0.25;
        }
    }
}
