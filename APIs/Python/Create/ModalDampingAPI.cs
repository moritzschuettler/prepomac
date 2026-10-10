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
    // The modal damping of a modal dynamics or steady state dynamics step, made by pmx.create.modal_damping or read
    // back from a step. Its damping ratios are read back as the parts pmx.create.damping_ratio makes.
    public class ModalDampingAPI : DetachedPartAPI
    {
        // Variables
        private readonly ModalDamping _modalDamping;


        // Constructors
        internal ModalDampingAPI(ModalDamping modalDamping)
        {
            _modalDamping = modalDamping;
        }


        // Methods
        internal ModalDamping GetModalDampingCopy()
        {
            return _modalDamping.DeepClone();
        }
        protected override string GetTypeName()
        {
            return "ModalDamping";
        }
        protected override string GetMakerName()
        {
            return "modal_damping";
        }
        protected override Dictionary<string, object> GetValues()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["damping_type"] = _modalDamping.DampingType.ToString();
            // A value the damping type does not use is kept hidden, as the dialog hides it, and would be refused back
            ModalDampingTypeEnum type = _modalDamping.DampingType;
            bool rayleigh = type == ModalDampingTypeEnum.Rayleigh;
            values["alpha"] = rayleigh ? (object)_modalDamping.Alpha : null;
            values["beta"] = rayleigh ? (object)_modalDamping.Beta : null;
            values["viscous_damping_ratio"] =
                type == ModalDampingTypeEnum.Constant ? (object)_modalDamping.ViscousDampingRatio : null;
            // The maker refuses an empty list of ratios, so a modal damping without them reads back None
            List<DampingRatioAndRange> ranges = _modalDamping.DampingRatiosAndRanges;
            if (type != ModalDampingTypeEnum.Direct || ranges == null || ranges.Count == 0)
                values["damping_ratios"] = null;
            else
            {
                object[] dampingRatios = new object[ranges.Count];
                for (int i = 0; i < ranges.Count; i++) dampingRatios[i] = new DampingRatioAPI(ranges[i].DeepClone());
                values["damping_ratios"] = PythonListConverter.ToPythonList(dampingRatios);
            }
            //
            return values;
        }
    }
}
