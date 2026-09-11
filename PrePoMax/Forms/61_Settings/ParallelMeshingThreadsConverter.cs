// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using System.Globalization;
using CaeGlobals;

namespace PrePoMax.Settings
{
    public class ParallelMeshingThreadsConverter : StringIntegerConverter
    {
        // Methods                                                                                                                   
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string valueString && valueString.Trim().Equals("Auto", StringComparison.OrdinalIgnoreCase))
                return 0;
            else return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                                         Type destinationType)
        {
            if (destinationType == typeof(string) && value is int intValue && intValue == 0) return "Auto";
            else return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
