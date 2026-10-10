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
using System;
using System.Collections.Generic;

namespace APIs.Python
{
    // A step control parameter, made by a pmx.create *_control method or read back from a step
    public class StepControlParameterAPI : DetachedPartAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ControlTypes = new Dictionary<Type, string>
        {
            { typeof(TimeIncrementationStepControlParameter), "time_incrementation_control" },
            { typeof(FieldStepControlParameter), "field_control" },
            { typeof(ContactStepControlParameter), "contact_control" },
            { typeof(ResetStepControlParameter), "reset_control" }
        };
        private readonly StepControlParameter _parameter;


        // Properties
        [PythonAPI("The control type, named after the pmx.create method that makes it, for example " +
                   "contact_control.", Returns = "str")]
        public string control_type { get { return GetControlType(_parameter); } }


        // Constructors
        internal StepControlParameterAPI(StepControlParameter parameter)
        {
            _parameter = parameter;
        }


        // Static methods
        internal static string GetControlType(StepControlParameter parameter)
        {
            string controlType;
            if (ControlTypes.TryGetValue(parameter.GetType(), out controlType)) return controlType;
            //
            throw new PythonAPIException("The step control parameter " + parameter.GetType().Name + " is not made " +
                                         "by any pmx.create method.");
        }


        // Methods
        internal StepControlParameter GetParameterCopy()
        {
            return _parameter.DeepClone();
        }
        protected override string GetTypeName()
        {
            return "StepControlParameter";
        }
        protected override string GetMakerName()
        {
            return GetControlType(_parameter);
        }
        protected override Dictionary<string, object> GetValues()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            if (_parameter is TimeIncrementationStepControlParameter time)
            {
                values["i0"] = FromUnset(time.I0);
                values["ir"] = FromUnset(time.IR);
                values["ip"] = FromUnset(time.IP);
                values["ic"] = FromUnset(time.IC);
                values["il"] = FromUnset(time.IL);
                values["ig"] = FromUnset(time.IG);
                values["is_value"] = FromUnset(time.IS);
                values["ia"] = FromUnset(time.IA);
                values["ij"] = FromUnset(time.IJ);
                values["it"] = FromUnset(time.IT);
                values["df"] = FromUnset(time.Df);
                values["dc"] = FromUnset(time.DC);
                values["db"] = FromUnset(time.DB);
                values["da"] = FromUnset(time.DA);
                values["ds"] = FromUnset(time.DS);
                values["dh"] = FromUnset(time.DH);
                values["dd"] = FromUnset(time.DD);
                values["wg"] = FromUnset(time.WG);
            }
            else if (_parameter is FieldStepControlParameter field)
            {
                values["rna"] = FromUnset(field.Rna);
                values["cna"] = FromUnset(field.Cna);
                values["q0a"] = FromUnset(field.Q0a);
                values["qua"] = FromUnset(field.Qua);
                values["rpa"] = FromUnset(field.Rpa);
                values["ea"] = FromUnset(field.Ea);
                values["cea"] = FromUnset(field.Cea);
                values["rla"] = FromUnset(field.Rla);
            }
            else if (_parameter is ContactStepControlParameter contact)
            {
                values["delcon"] = contact.Delcon;
                values["alea"] = contact.Alea;
                values["kscalemax"] = contact.Kscalemax;
                values["itf2f"] = contact.Itf2f;
            }
            else GetControlType(_parameter);
            //
            return values;
        }
    }
}
