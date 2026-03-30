// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class StepControls
    {
        // Variables                                                                                                                
        private List<StepControlParameter> _parameters;


        // Properties                                                                                                               
        public List<StepControlParameter> Parameters { get { return _parameters; } }


        // Constructors                                                                                                             
        public StepControls()
        {
            _parameters = new List<StepControlParameter>();
        }


        // Methods                                                                                                                  
        public void AddParameter(StepControlParameter parameter)
        {
            _parameters.Add(parameter);
        }
    }
}
