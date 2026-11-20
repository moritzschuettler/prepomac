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
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace PrePoMax
{
    [PmxType(10422)]
    public abstract class ViewStepControlParameter
    {
        // Variables                                                                                                                
        [PmxKey(0)] private DynamicCustomTypeDescriptor _dctd;


        // Properties                                                                                                               
        public abstract string Name { get; }
        //
        [Browsable(false)]
        public DynamicCustomTypeDescriptor DynamicCustomTypeDescriptor { get { return _dctd; } set { _dctd = value; } }
        public abstract CaeModel.StepControlParameter Base { get; }
    }
}
