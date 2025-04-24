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
    public class StringAreaDefaultConverter : TypeConverter
    {
        // Variables                                                                                                                
        protected static AreaUnit _areaUnit = AreaUnit.SquareMeter;
        //
        protected ArrayList values;
        protected string _default = "Default";
        protected static double _initialValue = 0;


        // Properties                                                                                                               
        public static string SetUnit
        {
            set
            {
                if (value == "") _areaUnit = (AreaUnit)MyUnit.NoUnit;
                else _areaUnit = Area.ParseUnit(value);
            }
        }
        public static string SetInitialValue
        {
            set
            {
                if (!double.TryParse(value, out _initialValue)) _initialValue = ConvertToCurrentUnits(value);
                _initialValue = Tools.RoundToSignificantDigits(_initialValue, 3);
            }
        }


        // Constructors                                                                                                             
        public StringAreaDefaultConverter()
        {
            // Initializes the standard values list with defaults.
            values = new ArrayList(new double[] { double.NaN, _initialValue });
        }


        // Methods                                                                                                                  
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Passes the local integer array.
            StandardValuesCollection svc = new StandardValuesCollection(values);
            return svc;
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            else return base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // Convert from string
            if (value is string valueString)
            {
                if (String.Equals(value, _default)) return double.NaN;
                else return MyNCalc.ConvertFromString(valueString, ConvertToCurrentUnits);
            }
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
                        if (double.IsNaN(valueDouble)) return _default;
                        else
                        {
                            string valueString = valueDouble.ToString();
                            if ((int)_areaUnit != MyUnit.NoUnit) valueString += " " + Area.GetAbbreviation(_areaUnit);
                            return valueString;
                        }
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
                Area area = Area.Parse(valueWithUnitString);
                if ((int)_areaUnit != MyUnit.NoUnit) area = area.ToUnit(_areaUnit);
                return area.Value;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + Environment.NewLine + Environment.NewLine + SupportedUnitAbbreviations());
            }
        }
        public static string SupportedUnitAbbreviations()
        {
            string abb;
            string supportedUnitAbbreviations = "Supported area abbreviations: ";
            var allUnits = Area.Units;
            for (int i = 0; i < allUnits.Length; i++)
            {
                abb = Area.GetAbbreviation(allUnits[i]);
                if (abb != null) abb.Trim();
                if (abb.Length > 0) supportedUnitAbbreviations += abb;
                if (i != allUnits.Length - 1) supportedUnitAbbreviations += ", ";
            }
            supportedUnitAbbreviations += ".";
            //
            return supportedUnitAbbreviations;
        }
    }

}