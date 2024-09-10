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
    public enum StefanBoltzmannUnit { }
    public class StringStefanBoltzmannUndefinedConverter : TypeConverter
    {
        // Variables                                                                                                                
        protected static MassUnit _massUnit = MassUnit.Kilogram;
        protected static DurationUnit _timeUnit = DurationUnit.Second;
        protected static TemperatureDeltaUnit _temperatureDeltaUnit = TemperatureDeltaUnit.DegreeCelsius;
        protected static StefanBoltzmannUnit _stefanBoltzmannUnit = (StefanBoltzmannUnit)MyUnit.NoUnit;
        protected static string error = "Unable to parse quantity. Expected the form \"{value} {unit abbreviation}" +
                                        "\", such as \"5.5 m\". The spacing is optional.";
        //
        protected static ArrayList values;
        protected static double _initialValue = 0;      // use initial value for the constructor to work
        protected static string _undefined = "Undefined";


        // Properties                                                                                                               
        public static string SetMassUnit
        { 
            set 
            {
                if (value == "")
                {
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                    _timeUnit = (DurationUnit)MyUnit.NoUnit;
                    _temperatureDeltaUnit = (TemperatureDeltaUnit)MyUnit.NoUnit;
                }
                else
                {
                    if (value == MyUnit.PoundForceSquareSecondPerInchAbbreviation)
                        _massUnit = MyUnit.PoundForceSquareSecondPerInch;
                    else _massUnit = Mass.ParseUnit(value);
                }
                //
                _stefanBoltzmannUnit = (StefanBoltzmannUnit)MyUnit.NoUnit;
            }
        }
        public static string SetTimeUnit
        {
            set
            {
                if (value == "")
                {
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                    _timeUnit = (DurationUnit)MyUnit.NoUnit;
                    _temperatureDeltaUnit = (TemperatureDeltaUnit)MyUnit.NoUnit;
                }
                else
                {
                    _timeUnit = Duration.ParseUnit(value);
                }
                //
                _stefanBoltzmannUnit = (StefanBoltzmannUnit)MyUnit.NoUnit;
            }
        }
        public static string SetTemperatureDeltaUnit
        {
            set
            {
                if (value == "")
                {
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                    _timeUnit = (DurationUnit)MyUnit.NoUnit;
                    _temperatureDeltaUnit = (TemperatureDeltaUnit)MyUnit.NoUnit;
                }
                else
                {
                    _temperatureDeltaUnit = TemperatureDelta.ParseUnit(value);
                }
                //
                _stefanBoltzmannUnit = (StefanBoltzmannUnit)MyUnit.NoUnit;
            }
        }
        public static string SetUnit
        {
            set
            {
                if (value == MyUnit.PoundForcePerInchSecondQuadFahrenheitAbbreviation)
                    _stefanBoltzmannUnit = MyUnit.PoundForcePerInchSecondQuadFahrenheit;
                else throw new NotSupportedException();
            }
        }
        public static double SetInitialValue
        {
            set
            {
                _initialValue = value;
                CreateListOfStandardValues();
            }
        }
        //
        public static string GetUnitAbbreviation(MassUnit massUnit, DurationUnit timeUnit, 
                                                 TemperatureDeltaUnit temperatureDeltaUnit,
                                                 StefanBoltzmannUnit stefanBoltzmannUnit)
        {
            string unit;
            if (stefanBoltzmannUnit == MyUnit.PoundForcePerInchSecondQuadFahrenheit)
                unit = MyUnit.PoundForcePerInchSecondQuadFahrenheitAbbreviation;
            else if ((int)massUnit == MyUnit.NoUnit || (int)timeUnit == MyUnit.NoUnit ||
                     (int)temperatureDeltaUnit == MyUnit.NoUnit) unit = "";
            else unit = Mass.GetAbbreviation(massUnit) + "/(" + Duration.GetAbbreviation(timeUnit) + "³·" +
                        TemperatureDelta.GetAbbreviation(temperatureDeltaUnit) + "⁴)";
            return unit.Replace("∆", "");
        }
        

        // Constructors                                                                                                             
        public StringStefanBoltzmannUndefinedConverter()
        {
            CreateListOfStandardValues();
        }


        // Methods                                                                                                                  
        private static void CreateListOfStandardValues()
        {
            values = new ArrayList(new double[] { double.PositiveInfinity, _initialValue });
        }
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Passes the local integer array.
            StandardValuesCollection svc = new StandardValuesCollection(values);
            return svc;
        }
        //
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
                if (string.Equals(valueString, _undefined)) return double.PositiveInfinity;
                else return MyNCalc.ConvertFromString(valueString, ConvertToCurrentUnits);
            }
            else return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            try
            {
                if (destinationType == typeof(string))
                {
                    if (value is double valueDouble)
                    {
                        if (double.IsPositiveInfinity(valueDouble)) return _undefined;
                        else
                        {
                            string valueString = valueDouble.ToString();
                            string unit = GetUnitAbbreviation(_massUnit, _timeUnit, _temperatureDeltaUnit, _stefanBoltzmannUnit);
                            if (unit.Length > 0) valueString += " " + unit;
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
        private static void GetConversionToSI(string valueWithUnitString, out double value, out double conversionToSI)
        {
            valueWithUnitString = valueWithUnitString.Trim().Replace(" ", "");
            // From my unit
            if (valueWithUnitString.Contains(MyUnit.PoundForcePerInchSecondQuadFahrenheitAbbreviation))
            {
                valueWithUnitString = valueWithUnitString.Replace(MyUnit.PoundForcePerInchSecondQuadFahrenheitAbbreviation, "");
                if (double.TryParse(valueWithUnitString, out value))
                {
                    // 1 pound force = 4.44822162 newtons
                    // 1 inch = 0.0254 meters
                    // 1 s = 1 s
                    // 1 ∆F = 0.555555556 ∆K
                    conversionToSI = 315.2283038;
                }
                else throw new ArgumentException(error);
            }
            // From no unit
            else if (double.TryParse(valueWithUnitString, out value))
            {
                conversionToSI = 1;
            }
            // From supported unit
            else
            {
                string[] tmp = valueWithUnitString.Split('/');
                if (tmp.Length != 2) throw new FormatException(error);
                Mass mass = Mass.Parse(tmp[0]);
                value = (double)mass.Value;
                MassUnit massUnit = mass.Unit;
                mass = Mass.From(1, massUnit).ToUnit(MassUnit.Kilogram);
                //
                if (tmp[1].StartsWith("(") && tmp[1].EndsWith(")"))
                {
                    tmp[1] = tmp[1].Replace("(", "").Replace(")", "");
                    tmp = tmp[1].Split(new string[] { "*", "·" }, StringSplitOptions.RemoveEmptyEntries);
                }
                else throw new FormatException(error);
                if (tmp.Length != 2) throw new FormatException(error);
                //
                if (tmp[0].EndsWith("³") || tmp[0].EndsWith("^3")) tmp[0] = tmp[0].Replace("³", "").Replace("^3", "");
                else throw new FormatException(error);
                DurationUnit timeUnit = Duration.ParseUnit(tmp[0]);
                Duration time = Duration.From(1, timeUnit).ToUnit(DurationUnit.Second);
                //
                if (tmp[1].EndsWith("⁴") || tmp[1].EndsWith("^4")) tmp[1] = tmp[1].Replace("⁴", "").Replace("^4", "");
                else throw new FormatException(error);
                if (!tmp[1].Contains("∆")) tmp[1] = "∆" + tmp[1];
                TemperatureDeltaUnit temperatureDeltaUnit = TemperatureDelta.ParseUnit(tmp[1]);
                TemperatureDelta temperatureDelta =
                    TemperatureDelta.From(1, temperatureDeltaUnit).ToUnit(TemperatureDeltaUnit.Kelvin);
                //
                conversionToSI = (double)mass.Value / (Math.Pow(time.Value, 3) * Math.Pow(temperatureDelta.Value, 4));
            }
        }
        private static void GetConversionFromSI(out double conversionFromSI)
        {
            // To my unit
            if (_stefanBoltzmannUnit == MyUnit.PoundForcePerInchSecondQuadFahrenheit)
            {
                // 1 pound force = 4.44822162 newtons
                // 1 inch = 0.0254 meters
                // 1 s = 1 s
                // 1 ∆F = 0.555555556 ∆K
                conversionFromSI = 1 / 315.2283038;
            }
            // To no unit
            else if ((int)_massUnit == MyUnit.NoUnit || (int)_timeUnit == MyUnit.NoUnit ||
                     (int)_temperatureDeltaUnit == MyUnit.NoUnit)
            {
                conversionFromSI = 1;
            }
            // To supported unit
            else
            {
                Mass mass = Mass.From(1, MassUnit.Kilogram).ToUnit(_massUnit);
                Duration time = Duration.From(1, DurationUnit.Second).ToUnit(_timeUnit);
                TemperatureDelta temperatureDelta =
                    TemperatureDelta.From(1, TemperatureDeltaUnit.Kelvin).ToUnit(_temperatureDeltaUnit);
                //
                conversionFromSI = (double)mass.Value / (Math.Pow(time.Value, 3) * Math.Pow(temperatureDelta.Value, 4));
            }
        }
        //
        public static double ConvertToCurrentUnits(string valueWithUnitString)
        {
            try
            {
                double valueDouble;
                double conversionToSI;
                double conversionFromSI;
                GetConversionToSI(valueWithUnitString, out valueDouble, out conversionToSI);
                // To no unit
                if ((int)_massUnit == MyUnit.NoUnit || (int)_timeUnit == MyUnit.NoUnit ||
                    (int)_temperatureDeltaUnit == MyUnit.NoUnit) { }
                else
                {
                    GetConversionFromSI(out conversionFromSI);
                    //
                    if (Math.Abs(conversionToSI - 1 / conversionFromSI) > 1E-6)
                        valueDouble *= conversionToSI * conversionFromSI;
                }
                //
                return valueDouble;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.Replace("∆", "") + Environment.NewLine + Environment.NewLine + SupportedUnitAbbreviations());
            }
        }
        public static string SupportedUnitAbbreviations()
        {
            string supportedUnitAbbreviations = StringMassConverter.SupportedUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += StringTimeConverter.SupportedUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += StringTemperatureConverter.SupportedDeltaUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += "Additionally supported abbreviations: " +
                                          MyUnit.PoundForcePerInchSecondQuadFahrenheitAbbreviation;
            return supportedUnitAbbreviations;
        }
    }


}