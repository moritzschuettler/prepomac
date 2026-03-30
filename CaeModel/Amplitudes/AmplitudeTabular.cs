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
using CaeGlobals;
using System.Runtime.Serialization;

namespace CaeModel
{
    [Serializable]
    public class AmplitudeTabular : Amplitude
    {
        // Variables                                                                                                                
        


        // Properties                                                                                                               
        


        // Constructors                                                                                                             
        public AmplitudeTabular(string name, double[][] timeAmplitude)
            : base(name)
        {
            _timeAmplitude = timeAmplitude;
        }


        // Methods                                                                                                                  
    }
}
