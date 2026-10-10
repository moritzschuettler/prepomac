// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System.Collections.Generic;

namespace APIs.Python
{
    // The Rayleigh damping of a dynamic step, made by pmx.create.damping or read back from a step
    public class DampingAPI : DetachedPartAPI
    {
        // Variables
        private readonly Damping _damping;


        // Constructors
        internal DampingAPI(Damping damping)
        {
            _damping = damping;
        }


        // Methods
        internal Damping GetDampingCopy()
        {
            return _damping.DeepClone();
        }
        protected override string GetTypeName()
        {
            return "Damping";
        }
        protected override string GetMakerName()
        {
            return "damping";
        }
        protected override Dictionary<string, object> GetValues()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["damping_type"] = _damping.DampingType.ToString();
            values["alpha"] = _damping.Alpha;
            values["beta"] = _damping.Beta;
            //
            return values;
        }
    }
}
