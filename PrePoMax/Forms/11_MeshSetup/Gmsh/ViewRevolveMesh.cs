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
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewRevolveMesh : ViewSweepMesh
    {
        // Constructors                                                                                                             
        public ViewRevolveMesh(RevolveMesh revolveMesh)
            : base(revolveMesh)
        {
        }


        // Methods                                                                                                                  
        public override void UpdateVisibility()
        {
            base.UpdateVisibility();
            // A revolve finds the sources of the selected parts itself, and the imprints a sweep carries across a
            // prism are not made by a revolved block
            _dctd.GetProperty(nameof(SourceType)).SetIsBrowsable(false);
            _dctd.GetProperty(nameof(KeepImprintEdges)).SetIsBrowsable(false);
        }
    }
}
