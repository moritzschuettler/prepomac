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
using UnitsNet;
using UnitsNet.Units;

namespace CaeGlobals
{
    public class StringIntegerPercentConverter : TypeConverter
    {
        // Variables                                                                                                                
        readonly static RatioUnit _ratioUnit = RatioUnit.Percent;


        // Properties                                                                                                               
        public static string GetUnitAbbreviation()
        {
            return Ratio.GetAbbreviation(_ratioUnit);
        }


        // Constructors                                                                                                             
        public StringIntegerPercentConverter()
        {
        }


        // Methods                                                                                                                   
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            else return base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string valueString)
            {
                double valueDouble = MyNCalc.ConvertFromString(valueString, ConvertToCurrentUnits);
                return (int)Math.Round(valueDouble, MidpointRounding.AwayFromZero);
            }
            else return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // Convert to string
            try
            {
                if (destinationType == typeof(string) && value is int valueInt)
                {
                    return valueInt.ToString() + " " + GetUnitAbbreviation();
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }
            catch
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
        //
        public static double ConvertToCurrentUnits(string valueWithUnitString)
        {
            try
            {
                Ratio ratio = Ratio.Parse(valueWithUnitString).ToUnit(_ratioUnit);
                return ratio.Value;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + Environment.NewLine + Environment.NewLine + SupportedUnitAbbreviations());
            }
        }
        public static string SupportedUnitAbbreviations()
        {
            string abbreviation;
            string supportedUnitAbbreviations = "Supported ratio abbreviations: ";
            bool first = true;
            var allUnits = Ratio.Units;
            for (int i = 0; i < allUnits.Length; i++)
            {
                abbreviation = Ratio.GetAbbreviation(allUnits[i]);
                if (abbreviation != null) abbreviation = abbreviation.Trim();
                if (!string.IsNullOrEmpty(abbreviation))
                {
                    if (!first) supportedUnitAbbreviations += ", ";
                    supportedUnitAbbreviations += abbreviation;
                    first = false;
                }
            }
            supportedUnitAbbreviations += ".";
            //
            return supportedUnitAbbreviations;
        }
    }
}
