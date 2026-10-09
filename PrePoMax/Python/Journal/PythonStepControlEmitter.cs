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
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // The CalculiX solution controls of a step. They are detached parts the pmx.create factories make,
    // and a step takes the whole list at once, so the list is rendered as one argument.
    public static class PythonStepControlEmitter
    {
        // Methods                                                                                                                  
        public static string GetParameters(StepControls controls)
        {
            List<string> makers = new List<string>();
            foreach (StepControlParameter parameter in controls.Parameters) makers.Add(GetMaker(parameter));
            //
            return "[" + string.Join(", ", makers) + "]";
        }
        // Only a value that differs from a new parameter is written: the maker leaves an argument it is not given at
        // that value, and several of them hold -1 or NaN, which is unset in the solver input and refused as an argument
        private static string GetMaker(StepControlParameter parameter)
        {
            if (parameter is TimeIncrementationStepControlParameter time)
            {
                TimeIncrementationStepControlParameter unset = new TimeIncrementationStepControlParameter();
                return PythonJournalWriter.Join("pmx.create.time_incrementation_control",
                                                Changed("i0=", time.I0, unset.I0),
                                                Changed("ir=", time.IR, unset.IR),
                                                Changed("ip=", time.IP, unset.IP),
                                                Changed("ic=", time.IC, unset.IC),
                                                Changed("il=", time.IL, unset.IL),
                                                Changed("ig=", time.IG, unset.IG),
                                                // The Python name carries a suffix, because is is a keyword
                                                Changed("is_value=", time.IS, unset.IS),
                                                Changed("ia=", time.IA, unset.IA),
                                                Changed("ij=", time.IJ, unset.IJ),
                                                Changed("it=", time.IT, unset.IT),
                                                Changed("df=", time.Df, unset.Df),
                                                Changed("dc=", time.DC, unset.DC),
                                                Changed("db=", time.DB, unset.DB),
                                                Changed("da=", time.DA, unset.DA),
                                                Changed("ds=", time.DS, unset.DS),
                                                Changed("dh=", time.DH, unset.DH),
                                                Changed("dd=", time.DD, unset.DD),
                                                Changed("wg=", time.WG, unset.WG));
            }
            if (parameter is FieldStepControlParameter field)
            {
                FieldStepControlParameter unset = new FieldStepControlParameter();
                return PythonJournalWriter.Join("pmx.create.field_control",
                                                Changed("rna=", field.Rna, unset.Rna),
                                                Changed("cna=", field.Cna, unset.Cna),
                                                Changed("q0a=", field.Q0a, unset.Q0a),
                                                Changed("qua=", field.Qua, unset.Qua),
                                                Changed("rpa=", field.Rpa, unset.Rpa),
                                                Changed("ea=", field.Ea, unset.Ea),
                                                Changed("cea=", field.Cea, unset.Cea),
                                                Changed("rla=", field.Rla, unset.Rla));
            }
            if (parameter is ContactStepControlParameter contact)
            {
                ContactStepControlParameter unset = new ContactStepControlParameter();
                return PythonJournalWriter.Join("pmx.create.contact_control",
                                                Changed("delcon=", contact.Delcon, unset.Delcon),
                                                Changed("alea=", contact.Alea, unset.Alea),
                                                Changed("kscalemax=", contact.Kscalemax, unset.Kscalemax),
                                                Changed("itf2f=", contact.Itf2f, unset.Itf2f));
            }
            if (parameter is ResetStepControlParameter) return "pmx.create.reset_control()";
            //
            throw new NotSupportedException("the step control parameter " + parameter.GetType().Name +
                                            " has no pmx.create maker");
        }
        private static string Changed(string keyword, int value, int unsetValue)
        {
            if (value == unsetValue) return null;
            //
            return keyword + PythonValueFormatter.Format(value);
        }
        private static string Changed(string keyword, double value, double unsetValue)
        {
            if (value.Equals(unsetValue)) return null;
            //
            return keyword + PythonValueFormatter.Number(value);
        }
    }
}
