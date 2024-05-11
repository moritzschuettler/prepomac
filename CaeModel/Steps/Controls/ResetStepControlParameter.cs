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
    public class ResetStepControlParameter : StepControlParameter
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public ResetStepControlParameter()
        {
            Reset();
        }


        // Methods                                                                                                                  
        public override void Reset()
        {
        }
    }
}
