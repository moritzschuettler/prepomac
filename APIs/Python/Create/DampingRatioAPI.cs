// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using CaeModel;
using System.Collections.Generic;

namespace APIs.Python
{
    // The viscous damping ratio of a mode range, made by pmx.create.damping_ratio or read back from a modal damping
    public class DampingRatioAPI : DetachedPartAPI
    {
        // Variables
        private readonly DampingRatioAndRange _dampingRatio;


        // Constructors
        internal DampingRatioAPI(DampingRatioAndRange dampingRatio)
        {
            _dampingRatio = dampingRatio;
        }


        // Methods
        internal DampingRatioAndRange GetDampingRatioCopy()
        {
            return _dampingRatio.DeepClone();
        }
        protected override string GetTypeName()
        {
            return "DampingRatio";
        }
        protected override string GetMakerName()
        {
            return "damping_ratio";
        }
        protected override Dictionary<string, object> GetValues()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["damping_ratio"] = _dampingRatio.DampingRatio;
            values["lowest_mode"] = _dampingRatio.LowestMode;
            values["highest_mode"] = _dampingRatio.HighestMode;
            //
            return values;
        }
    }
}
