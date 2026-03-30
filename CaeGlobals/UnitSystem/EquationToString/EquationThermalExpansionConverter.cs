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
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using UnitsNet.Units;
using UnitsNet;

namespace CaeGlobals
{
    public class EquationThermalExpansionConverter : StringThermalExpansionConverter
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public EquationThermalExpansionConverter()
        {
        }


        // Methods                                                                                                                  
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // Convert from string to equation
            return EquationToString.ConvertFromStringToEquationString(context, culture, value, base.ConvertFrom);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // Convert from equation to string
            return EquationToString.ConvertToStringFromEquationString(context, culture, value, destinationType,
                                                                      base.ConvertFrom, base.ConvertTo);
        }
    }


}