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
    public class ViewBoundaryDisplacementStep : ViewStep
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        

        // Constructors                                                                                                             
        public ViewBoundaryDisplacementStep(CaeModel.BoundaryDisplacementStep step, bool installProvider = true)
            : base(step)
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
            return _step;
        }
        public override void InstallProvider()
        {
            base.InstallProvider();
        }
        public override void UpdateVisibility()
        {
            base.UpdateVisibility();
            //
            _dctd.GetProperty(nameof(SolverType)).SetIsBrowsable(false);
        }

    }
}
