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
            if (i0 != null) parameter.I0 = ToIntAtLeast(1, i0, "i0");
            if (ir != null) parameter.IR = ToIntAtLeast(1, ir, "ir");
            if (ip != null) parameter.IP = ToIntAtLeast(1, ip, "ip");
            if (ic != null) parameter.IC = ToIntAtLeast(1, ic, "ic");
            if (il != null) parameter.IL = ToIntAtLeast(1, il, "il");
            if (ig != null) parameter.IG = ToIntAtLeast(1, ig, "ig");
            if (iS != null) parameter.IS = ToIntAtLeast(1, iS, "is_value");
            if (ia != null) parameter.IA = ToIntAtLeast(1, ia, "ia");
            if (ij != null) parameter.IJ = ToIntAtLeast(1, ij, "ij");
            if (it != null) parameter.IT = ToIntAtLeast(1, it, "it");
            if (df != null) parameter.Df = ToDoubleAtLeast(0, df, "df");
            if (dc != null) parameter.DC = ToDoubleAtLeast(0, dc, "dc");
            if (db != null) parameter.DB = ToDoubleAtLeast(0, db, "db");
            if (da != null) parameter.DA = ToDoubleAtLeast(0, da, "da");
            if (ds != null) parameter.DS = ToDoubleAtLeast(0, ds, "ds");
            if (dh != null) parameter.DH = ToDoubleAtLeast(0, dh, "dh");
            if (dd != null) parameter.DD = ToDoubleAtLeast(0, dd, "dd");
            if (wg != null) parameter.WG = ToDoubleAtLeast(0, wg, "wg");
            //
            return parameter;
        }
        public static FieldStepControlParameter CreateFieldControl(object rna, object cna, object q0a, object qua,
                                                                   object rpa, object ea, object cea, object rla)
        {
            FieldStepControlParameter parameter = new FieldStepControlParameter();
            if (rna != null) parameter.Rna = ToDoubleAtLeast(0, rna, "rna");
            if (cna != null) parameter.Cna = ToDoubleAtLeast(0, cna, "cna");
            if (q0a != null) parameter.Q0a = ToDoubleAtLeast(0, q0a, "q0a");
            if (qua != null) parameter.Qua = ToDoubleAtLeast(0, qua, "qua");
            if (rpa != null) parameter.Rpa = ToDoubleAtLeast(0, rpa, "rpa");
            if (ea != null) parameter.Ea = ToDoubleAtLeast(0, ea, "ea");
            if (cea != null) parameter.Cea = ToDoubleAtLeast(0, cea, "cea");
            if (rla != null) parameter.Rla = ToDoubleAtLeast(0, rla, "rla");
            //
            return parameter;
        }
        public static ContactStepControlParameter CreateContactControl(object delcon, object alea,
                                                                       object kscalemax, object itf2f)
        {
            ContactStepControlParameter parameter = new ContactStepControlParameter();
            if (delcon != null) parameter.Delcon = ToDoubleAtLeast(0, delcon, "delcon");
            if (alea != null) parameter.Alea = ToDoubleBetween(0, 1, alea, "alea");
            if (kscalemax != null) parameter.Kscalemax = ToIntAtLeast(1, kscalemax, "kscalemax");
            if (itf2f != null) parameter.Itf2f = ToIntAtLeast(1, itf2f, "itf2f");
            //
            return parameter;
        }
        public static ResetStepControlParameter CreateResetControl()
        {
            return new ResetStepControlParameter();
        }
        //
        // The parameter setters clamp a value outside their range, so a value the caller gave is refused instead
        private static int ToIntAtLeast(int minimum, object value, string argumentName)
        {
            int converted = PythonValueConverter.ToInt(value, argumentName);
            if (converted < minimum)
                throw new PythonAPIException("The argument '" + argumentName + "' must be at least " + minimum + ".");
            //
            return converted;
        }
        private static double ToDoubleAtLeast(double minimum, object value, string argumentName)
        {
            double converted = PythonValueConverter.ToDouble(value, argumentName);
            if (!(converted >= minimum))
                throw new PythonAPIException("The argument '" + argumentName + "' must be at least " + minimum + ".");
            //
            return converted;
        }
        private static double ToDoubleBetween(double minimum, double maximum, object value, string argumentName)
        {
            double converted = PythonValueConverter.ToDouble(value, argumentName);
            if (!(converted >= minimum && converted <= maximum))
                throw new PythonAPIException("The argument '" + argumentName + "' must be between " + minimum +
                                             " and " + maximum + ".");
            //
            return converted;
        }
    }
}
