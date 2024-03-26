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
using System.Security.AccessControl;

namespace CaeGlobals
{
    public class StringDensityConverter : TypeConverter
    {
        // Variables                                                                                                                
        protected static DensityUnit _densityUnit = DensityUnit.KilogramPerCubicMeter;


        // Properties                                                                                                               
        public static string SetUnit
        {
            set
            {
                if (value == "") _densityUnit = (DensityUnit)MyUnit.NoUnit;
                else if (value == MyUnit.PoundForceSquareSecondPerQuadInchAbbreviation)
                    _densityUnit = MyUnit.PoundForceSquareSecondPerQuadInch;
                else _densityUnit = Density.ParseUnit(value);
            }
        }
        public static string GetUnitAbbreviation(DensityUnit densityUnit)
        {
            string unit;
            if ((int)densityUnit == MyUnit.NoUnit) unit = "";
            else if (densityUnit == MyUnit.PoundForceSquareSecondPerQuadInch)
                return MyUnit.PoundForceSquareSecondPerQuadInchAbbreviation;
            else unit = Density.GetAbbreviation(densityUnit);
            return unit;
        }


        // Constructors                                                                                                             
        public StringDensityConverter()
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
                        string unit = GetUnitAbbreviation(_densityUnit);
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
                double conversion = 10686895.19;
                double scale = 1;
                valueWithUnitString = valueWithUnitString.Trim().Replace(" ", "");
                // Check if it is given in unsupported units
                if (valueWithUnitString.Contains(MyUnit.PoundForceSquareSecondPerQuadInchAbbreviation))
                {
                    valueWithUnitString = valueWithUnitString.Replace(MyUnit.PoundForceSquareSecondPerQuadInchAbbreviation,
                                                                      Density.GetAbbreviation(DensityUnit.KilogramPerCubicMeter));
                    scale = conversion;
                }
                // Check if it must be converted to unsupported units
                double value;
                if ((int)_densityUnit == MyUnit.NoUnit)
                {
                    Density density = Density.Parse(valueWithUnitString);
                    value = (double)density.Value;
                }
                else if (_densityUnit == MyUnit.PoundForceSquareSecondPerQuadInch)
                {
                    Density density = Density.Parse(valueWithUnitString).ToUnit(DensityUnit.KilogramPerCubicMeter);
                    if (scale == conversion) value = (double)density.Value;
                    else value = scale * (double)density.Value / conversion;
                }
                else
                {
                    Density density = Density.Parse(valueWithUnitString).ToUnit(_densityUnit);
                    value = scale * (double)density.Value;
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
            string supportedUnitAbbreviations = "Supported density abbreviations: ";
            var allUnits = Density.Units;
            for (int i = 0; i < allUnits.Length; i++)
            {
                abb = Density.GetAbbreviation(allUnits[i]);
                if (abb != null) abb.Trim();
                if (abb.Length > 0) supportedUnitAbbreviations += abb;
                if (i != allUnits.Length - 1) supportedUnitAbbreviations += ", ";
            }
            // My units
            supportedUnitAbbreviations += ", " + MyUnit.PoundForceSquareSecondPerQuadInchAbbreviation;
            //
            supportedUnitAbbreviations += ".";
            //
            return supportedUnitAbbreviations;
        }
    }

}