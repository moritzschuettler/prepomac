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
        private static string GetMaker(StepControlParameter parameter)
        {
            if (parameter is TimeIncrementationStepControlParameter time)
                return PythonJournalWriter.Join("pmx.create.time_incrementation_control",
                                                "i0=" + PythonValueFormatter.Format(time.I0),
                                                "ir=" + PythonValueFormatter.Format(time.IR),
                                                "ip=" + PythonValueFormatter.Format(time.IP),
                                                "ic=" + PythonValueFormatter.Format(time.IC),
                                                "il=" + PythonValueFormatter.Format(time.IL),
                                                "ig=" + PythonValueFormatter.Format(time.IG),
                                                // The Python name carries a suffix, because is is a keyword
                                                "is_value=" + PythonValueFormatter.Format(time.IS),
                                                "ia=" + PythonValueFormatter.Format(time.IA),
                                                "ij=" + PythonValueFormatter.Format(time.IJ),
                                                "it=" + PythonValueFormatter.Format(time.IT),
                                                "df=" + PythonValueFormatter.Number(time.Df),
                                                "dc=" + PythonValueFormatter.Number(time.DC),
                                                "db=" + PythonValueFormatter.Number(time.DB),
                                                "da=" + PythonValueFormatter.Number(time.DA),
                                                "ds=" + PythonValueFormatter.Number(time.DS),
                                                "dh=" + PythonValueFormatter.Number(time.DH),
                                                "dd=" + PythonValueFormatter.Number(time.DD),
                                                "wg=" + PythonValueFormatter.Number(time.WG));
            if (parameter is FieldStepControlParameter field)
                return PythonJournalWriter.Join("pmx.create.field_control",
                                                "rna=" + PythonValueFormatter.Number(field.Rna),
                                                "cna=" + PythonValueFormatter.Number(field.Cna),
                                                "q0a=" + PythonValueFormatter.Number(field.Q0a),
                                                "qua=" + PythonValueFormatter.Number(field.Qua),
                                                "rpa=" + PythonValueFormatter.Number(field.Rpa),
                                                "ea=" + PythonValueFormatter.Number(field.Ea),
                                                "cea=" + PythonValueFormatter.Number(field.Cea),
                                                "rla=" + PythonValueFormatter.Number(field.Rla));
            if (parameter is ContactStepControlParameter contact)
                return PythonJournalWriter.Join("pmx.create.contact_control",
                                                "delcon=" + PythonValueFormatter.Number(contact.Delcon),
                                                "alea=" + PythonValueFormatter.Number(contact.Alea),
                                                "kscalemax=" + PythonValueFormatter.Format(contact.Kscalemax),
                                                "itf2f=" + PythonValueFormatter.Format(contact.Itf2f));
            if (parameter is ResetStepControlParameter) return "pmx.create.reset_control()";
            //
            throw new NotSupportedException("the step control parameter " + parameter.GetType().Name +
                                            " has no pmx.create maker");
        }
    }
}
