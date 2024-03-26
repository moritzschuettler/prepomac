using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;
using UnitsNet;
using UnitsNet.Units;

namespace CaeGlobals
{
    public class StringMassConverter : TypeConverter
    {
        // Variables                                                                                                                
        protected static MassUnit _massUnit = MassUnit.Kilogram;


        // Properties                                                                                                               
        public static string SetUnit 
        {
            set
            {
                if (value == "") _massUnit = (MassUnit)MyUnit.NoUnit;
                else if (value == MyUnit.PoundForceSquareSecondPerInchAbbreviation)
                    _massUnit = MyUnit.PoundForceSquareSecondPerInch;
                else _massUnit = Mass.ParseUnit(value);
            }
        }
        public static string GetUnitAbbreviation(MassUnit massUnit)
        {
            string unit;
            if ((int)massUnit == MyUnit.NoUnit) unit = "";
            else if (massUnit == MyUnit.PoundForceSquareSecondPerInch) return MyUnit.PoundForceSquareSecondPerInchAbbreviation;
            else unit = Mass.GetAbbreviation(massUnit);
            return unit;
        }


        // Constructors                                                                                                             
        public StringMassConverter()
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
            // Convert from string
            if (value is string valueString) return MyNCalc.ConvertFromString(valueString, ConvertToCurrentUnits);
            else return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            // Convert to string
            try
            {
                if (destinationType == typeof(string))
                {
                    if (value is double valueDouble)
                    {
                        string valueString = valueDouble.ToString();
                        string unit = GetUnitAbbreviation(_massUnit);
                        if (unit.Length > 0) valueString += " " + unit;
                        return valueString;
                    }
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
                // 1 pound force = 4.44822162 newtons
                // 1 s = 1 s
                // 1 inch = 0.0254 meters
                double conversion = 175.1268354;
                double scale = 1;
                valueWithUnitString = valueWithUnitString.Trim().Replace(" ", "");
                // Check if it is given in unsupported units
                if (valueWithUnitString.Contains(MyUnit.PoundForceSquareSecondPerInchAbbreviation))
                {
                    valueWithUnitString = valueWithUnitString.Replace(MyUnit.PoundForceSquareSecondPerInchAbbreviation, "kg");
                    scale = conversion;
                }
                // Check if it must be converted to unsupported units
                double value;
                if ((int)_massUnit == MyUnit.NoUnit)
                {
                    Mass mass = Mass.Parse(valueWithUnitString);
                    value = (double)mass.Value;
                }
                else if (_massUnit == MyUnit.PoundForceSquareSecondPerInch)
                {
                    Mass mass = Mass.Parse(valueWithUnitString).ToUnit(MassUnit.Kilogram);
                    if (scale == conversion) value = (double)mass.Value;
                    else value = scale * (double)mass.Value / conversion;
                }
                else
                {
                    Mass mass = Mass.Parse(valueWithUnitString).ToUnit(_massUnit);
                    value = scale * (double)mass.Value;
                }
                return value;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + Environment.NewLine + Environment.NewLine + SupportedUnitAbbreviations());
            }
        }
        public static string SupportedUnitAbbreviations()
        {
            string abb;
            string supportedUnitAbbreviations = "Supported mass abbreviations: ";
            var allUnits = Mass.Units;
            for (int i = 0; i < allUnits.Length; i++)
            {
                abb = Mass.GetAbbreviation(allUnits[i]);
                if (abb != null) abb.Trim();
                if (abb.Length > 0) supportedUnitAbbreviations += abb;
                if (i != allUnits.Length - 1) supportedUnitAbbreviations += ", ";
            }
            // My units
            supportedUnitAbbreviations += ", " + MyUnit.PoundForceSquareSecondPerInchAbbreviation;
            //
            supportedUnitAbbreviations += ".";
            //
            return supportedUnitAbbreviations;
        }
    }

}