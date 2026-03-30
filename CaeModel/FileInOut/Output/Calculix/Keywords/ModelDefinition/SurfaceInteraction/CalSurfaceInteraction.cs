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
using CaeModel;
using CaeMesh;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalSurfaceInteraction : CalculixKeyword
    {
        // Variables                                                                                                                
        private SurfaceInteraction _surfaceInteraction;


        // Properties                                                                                                               
        public object GetBase { get { return _surfaceInteraction; } }


        // Constructor                                                                                                              
        public CalSurfaceInteraction(SurfaceInteraction surfaceInteraction)
        {
            _surfaceInteraction = surfaceInteraction;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Surface interaction, Name={0}{1}", _surfaceInteraction.Name, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
