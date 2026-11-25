// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [PmxType(3092)]
    internal class CalMaterial : CalculixKeyword
    {
        // Variables                                                                                                                
        [PmxKey(0)] private Material _material;


        // Properties                                                                                                               
        public object GetBase { get { return _material; } }


        // Constructor                                                                                                              
        public CalMaterial(Material material)
        {
            _material = material;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Material, Name={0}{1}", _material.Name, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
