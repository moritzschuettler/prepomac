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
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public class ViewCoupledTempDispStep : ViewUncoupledTempDispStep
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        

        // Constructors                                                                                                             
        public ViewCoupledTempDispStep(CaeModel.UncoupledTempDispStep step, bool installProvider = true)
            : base(step, false)
        {
            if (installProvider)
            {
                InstallProvider();
                UpdateVisibility();
            }
        }


        // Methods
        public override CaeModel.Step GetBase()
        {
            return _heatTransferStep;
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
