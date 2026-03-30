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
using System.ComponentModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{    
    public abstract class ViewMaterialProperty
    {
        // Variables                                                                                                                
        private DynamicCustomTypeDescriptor _dctd;


        // Properties                                                                                                               
        [Browsable(false)]
        public abstract string Name { get; }
        //
        [Browsable(false)]
        public DynamicCustomTypeDescriptor DynamicCustomTypeDescriptor { get { return _dctd; } set { _dctd = value; } }
        // Methods                                                                                                                  
        public abstract CaeModel.MaterialProperty GetBase();
    }
}
