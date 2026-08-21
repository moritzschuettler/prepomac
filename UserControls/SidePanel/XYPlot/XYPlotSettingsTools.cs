// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using DynamicTypeDescriptor;

namespace UserControls
{
    public static class XYPlotSettingsTools
    {
        // Methods                                                                                                                  
        public static void PopulateNumericProperty(DynamicCustomTypeDescriptor dctd, string propertyName, float[] values)
        {
            CustomPropertyDescriptor cpd = dctd.GetProperty(propertyName);
            //
            cpd.StatandardValues.Clear();
            cpd.PropertyFlags |= PropertyFlags.ExclusiveStandardValues;
            //
            foreach (float value in values) cpd.StatandardValues.Add(new StandardValueAttribute(value));
        }
    }
}
