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
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [PmxType(10432)]
    public class ViewSlipWearStep : ViewStaticStep
    {
        // Variables                                                                                                                
        [PmxKey(0)] private CaeModel.SlipWearStep _slipWearStep;


        // Properties                                                                                                               
        

        // Constructors                                                                                                             
        public ViewSlipWearStep(CaeModel.SlipWearStep step, bool installProvider = true)
            : base(step, false)
        {
            _slipWearStep = step;
            //
            if (installProvider)
            {
                InstallProvider();
                UpdateVisibility();
            }
        }


        // Methods
        public override CaeModel.Step GetBase()
        {
            return _slipWearStep;
        }
        public override void InstallProvider()
        {
            base.InstallProvider();
        }
        public override void UpdateVisibility()
        {
            base.UpdateVisibility();
        }
    }
}
