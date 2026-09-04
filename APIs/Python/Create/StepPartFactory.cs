// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeJob;
using CaeModel;
using System.Collections.Generic;

namespace APIs.Python
{
    // Makes the detached parts a step takes as arguments. Only the given values override the model
    // defaults, so a part created without arguments is the part the dialog offers.
    internal static class StepPartFactory
    {
        // Methods
        public static Damping CreateDamping(string dampingType, object alpha, object beta)
        {
            Damping damping = new Damping();
            if (dampingType != null)
                damping.DampingType = PythonValueConverter.ToEnum<DampingTypeEnum>(dampingType, "damping_type");
            if (alpha != null) damping.Alpha = PythonValueConverter.ToDouble(alpha, "alpha");
            if (beta != null) damping.Beta = PythonValueConverter.ToDouble(beta, "beta");
            //
            return damping;
        }
        public static ModalDamping CreateModalDamping(string dampingType, object alpha, object beta,
                                                      object viscousDampingRatio, object dampingRatios)
        {
            ModalDamping modalDamping = new ModalDamping();
            if (dampingType != null)
                modalDamping.DampingType =
                    PythonValueConverter.ToEnum<ModalDampingTypeEnum>(dampingType, "damping_type");
            if (alpha != null) modalDamping.Alpha = PythonValueConverter.ToDouble(alpha, "alpha");
            if (beta != null) modalDamping.Beta = PythonValueConverter.ToDouble(beta, "beta");
            if (viscousDampingRatio != null)
                modalDamping.ViscousDampingRatio =
                    PythonValueConverter.ToDouble(viscousDampingRatio, "viscous_damping_ratio");
            if (dampingRatios != null)
            {
                List<DampingRatioAndRange> ranges =
                    PythonObjectListConverter.ToList<DampingRatioAndRange>(dampingRatios, "damping_ratios",
                                                                            "a damping ratio created by " +
                                                                            "pmx.create.damping_ratio, or a list " +
                                                                            "of them");
                if (ranges.Count == 0)
                    throw new PythonAPIException("The argument 'damping_ratios' must not be empty.");
                //
                modalDamping.DampingRatiosAndRanges = ranges;
            }
            //
            return modalDamping;
        }
        public static DampingRatioAndRange CreateDampingRatio(object dampingRatio, object lowestMode,
                                                              object highestMode)
        {
            return new DampingRatioAndRange(PythonValueConverter.ToDouble(dampingRatio, "damping_ratio"),
                                            PythonValueConverter.ToInt(lowestMode, "lowest_mode"),
                                            PythonValueConverter.ToInt(highestMode, "highest_mode"));
        }
        public static TimeIncrementationStepControlParameter CreateTimeIncrementationControl(
            object i0, object ir, object ip, object ic, object il, object ig, object iS, object ia, object ij,
            object it, object df, object dc, object db, object da, object ds, object dh, object dd, object wg)
        {
            TimeIncrementationStepControlParameter parameter = new TimeIncrementationStepControlParameter();
            if (i0 != null) parameter.I0 = PythonValueConverter.ToInt(i0, "i0");
            if (ir != null) parameter.IR = PythonValueConverter.ToInt(ir, "ir");
            if (ip != null) parameter.IP = PythonValueConverter.ToInt(ip, "ip");
            if (ic != null) parameter.IC = PythonValueConverter.ToInt(ic, "ic");
            if (il != null) parameter.IL = PythonValueConverter.ToInt(il, "il");
            if (ig != null) parameter.IG = PythonValueConverter.ToInt(ig, "ig");
            if (iS != null) parameter.IS = PythonValueConverter.ToInt(iS, "is_value");
            if (ia != null) parameter.IA = PythonValueConverter.ToInt(ia, "ia");
            if (ij != null) parameter.IJ = PythonValueConverter.ToInt(ij, "ij");
            if (it != null) parameter.IT = PythonValueConverter.ToInt(it, "it");
            if (df != null) parameter.Df = PythonValueConverter.ToDouble(df, "df");
            if (dc != null) parameter.DC = PythonValueConverter.ToDouble(dc, "dc");
            if (db != null) parameter.DB = PythonValueConverter.ToDouble(db, "db");
            if (da != null) parameter.DA = PythonValueConverter.ToDouble(da, "da");
            if (ds != null) parameter.DS = PythonValueConverter.ToDouble(ds, "ds");
            if (dh != null) parameter.DH = PythonValueConverter.ToDouble(dh, "dh");
            if (dd != null) parameter.DD = PythonValueConverter.ToDouble(dd, "dd");
            if (wg != null) parameter.WG = PythonValueConverter.ToDouble(wg, "wg");
            //
            return parameter;
        }
        public static FieldStepControlParameter CreateFieldControl(object rna, object cna, object q0a, object qua,
                                                                   object rpa, object ea, object cea, object rla)
        {
            FieldStepControlParameter parameter = new FieldStepControlParameter();
            if (rna != null) parameter.Rna = PythonValueConverter.ToDouble(rna, "rna");
            if (cna != null) parameter.Cna = PythonValueConverter.ToDouble(cna, "cna");
            if (q0a != null) parameter.Q0a = PythonValueConverter.ToDouble(q0a, "q0a");
            if (qua != null) parameter.Qua = PythonValueConverter.ToDouble(qua, "qua");
            if (rpa != null) parameter.Rpa = PythonValueConverter.ToDouble(rpa, "rpa");
            if (ea != null) parameter.Ea = PythonValueConverter.ToDouble(ea, "ea");
            if (cea != null) parameter.Cea = PythonValueConverter.ToDouble(cea, "cea");
            if (rla != null) parameter.Rla = PythonValueConverter.ToDouble(rla, "rla");
            //
            return parameter;
        }
        public static ContactStepControlParameter CreateContactControl(object delcon, object alea,
                                                                       object kscalemax, object itf2f)
        {
            ContactStepControlParameter parameter = new ContactStepControlParameter();
            if (delcon != null) parameter.Delcon = PythonValueConverter.ToDouble(delcon, "delcon");
            if (alea != null) parameter.Alea = PythonValueConverter.ToDouble(alea, "alea");
            if (kscalemax != null) parameter.Kscalemax = PythonValueConverter.ToInt(kscalemax, "kscalemax");
            if (itf2f != null) parameter.Itf2f = PythonValueConverter.ToInt(itf2f, "itf2f");
            //
            return parameter;
        }
        public static ResetStepControlParameter CreateResetControl()
        {
            return new ResetStepControlParameter();
        }
    }
}
