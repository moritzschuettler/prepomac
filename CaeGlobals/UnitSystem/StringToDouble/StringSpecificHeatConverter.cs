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
    public enum SpecificHeatUnit { }
    public class StringSpecificHeatConverter : TypeConverter
    {
        // Variables                                                                                                                
        protected static EnergyUnit _energyUnit = EnergyUnit.Joule;
        protected static MassUnit _massUnit = MassUnit.Kilogram;
        protected static TemperatureDeltaUnit _temperatureDeltaUnit = TemperatureDeltaUnit.DegreeCelsius;
        protected static SpecificHeatUnit _specificHeatUnit = (SpecificHeatUnit)MyUnit.NoUnit;
        protected static string error = "Unable to parse quantity. Expected the form \"{value} {unit abbreviation}" +
                                        "\", such as \"5.5 m\". The spacing is optional.";


        // Properties                                                                                                               
        public static string SetEnergyUnit
        {
            set
            {
                if (value == "")
                {
                    _energyUnit = (EnergyUnit)MyUnit.NoUnit;
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                    _temperatureDeltaUnit = (TemperatureDeltaUnit)MyUnit.NoUnit;
                }
                else
                {
                    if (value == MyUnit.PoundForceInchAbbreviation) _energyUnit = MyUnit.PoundForceInch;
                    else _energyUnit = Energy.ParseUnit(value);
                }
                //
                _specificHeatUnit = (SpecificHeatUnit)MyUnit.NoUnit;
            }
        }
        public static string SetMassUnit
        { 
            set 
            {
                if (value == "")
                {
                    _energyUnit = (EnergyUnit)MyUnit.NoUnit;
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                    _temperatureDeltaUnit = (TemperatureDeltaUnit)MyUnit.NoUnit;
                }
                else
                {
                    if (value == MyUnit.PoundForceSquareSecondPerInchAbbreviation)
                        _massUnit = MyUnit.PoundForceSquareSecondPerInch;
                    else _massUnit = Mass.ParseUnit(value);
                }
                //
                _specificHeatUnit = (SpecificHeatUnit)MyUnit.NoUnit;
            }
        }
        public static string SetTemperatureDeltaUnit
        {
            set
            {
                if (value == "")
                {
                    _energyUnit = (EnergyUnit)MyUnit.NoUnit;
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                    _temperatureDeltaUnit = (TemperatureDeltaUnit)MyUnit.NoUnit;
                }
                else
                {
                    _temperatureDeltaUnit = TemperatureDelta.ParseUnit(value);
                }
                //
                _specificHeatUnit = (SpecificHeatUnit)MyUnit.NoUnit;
            }
        }
        public static string SetUnit
        {
            set
            {
                if (value == MyUnit.SquareInchPerSquareSecondFahrenheitAbbreviation)
                    _specificHeatUnit = MyUnit.SquareInchPerSquareSecondFahrenheit;
                else throw new NotSupportedException();
            }
        }
        public static string GetUnitAbbreviation(EnergyUnit energyUnit, MassUnit massUnit,
                                                 TemperatureDeltaUnit temperatureDeltaUnit,
                                                 SpecificHeatUnit specificHeatUnit)
        {
            string unit;
            if (specificHeatUnit == MyUnit.SquareInchPerSquareSecondFahrenheit)
                unit = MyUnit.SquareInchPerSquareSecondFahrenheitAbbreviation;
            else if ((int)energyUnit == MyUnit.NoUnit || (int)massUnit == MyUnit.NoUnit ||
                     (int)temperatureDeltaUnit == MyUnit.NoUnit) unit = "";
            else
            {
                if (energyUnit == MyUnit.PoundForceInch) unit = MyUnit.PoundForceInchAbbreviation;
                else unit = Energy.GetAbbreviation(energyUnit);
                unit += "/(" + Mass.GetAbbreviation(massUnit) + "·" + TemperatureDelta.GetAbbreviation(temperatureDeltaUnit) + ")";
            }
            return unit.Replace("∆", "");
        }
        
        
        // Constructors                                                                                                             
        public StringSpecificHeatConverter()
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
            try
            {
                if (destinationType == typeof(string))
                {
                    if (value is double valueDouble)
                    {
                        string valueString = valueDouble.ToString();
                        string unit = GetUnitAbbreviation(_energyUnit, _massUnit, _temperatureDeltaUnit, _specificHeatUnit);
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
        private static void GetConversionToSI(string valueWithUnitString, out double value, out double conversionToSI)
        {
            valueWithUnitString = valueWithUnitString.Trim().Replace(" ", "");
            // From my unit
            if (valueWithUnitString.Contains(MyUnit.SquareInchPerSquareSecondFahrenheitAbbreviation))
            {
                valueWithUnitString = valueWithUnitString.Replace(MyUnit.SquareInchPerSquareSecondFahrenheitAbbreviation, "");
                if (double.TryParse(valueWithUnitString, out value))
                {
                    // 1 inch = 0.0254 meters
                    // 1 s = 1 s
                    // 1 ∆F = 0.555555556 ∆K
                    conversionToSI = 0.001161288;
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
                //
                Energy energy = Energy.Parse(tmp[0]);
                value = (double)energy.Value;
                EnergyUnit energyUnit = energy.Unit;
                energy = Energy.From(1, energyUnit).ToUnit(EnergyUnit.Joule);
                //
                tmp = tmp[1].Replace("(", "").Replace(")", "").Split(new string[] { "*", "·" },
                                                                     StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length != 2) throw new FormatException(error);
                //
                MassUnit massUnit = Mass.ParseUnit(tmp[0]);
                Mass mass = Mass.From(1, massUnit).ToUnit(MassUnit.Kilogram);
                //
                if (!tmp[1].Contains("∆")) tmp[1] = "∆" + tmp[1];
                TemperatureDeltaUnit temperatureDeltaUnit = TemperatureDelta.ParseUnit(tmp[1]);
                TemperatureDelta temperatureDelta =
                    TemperatureDelta.From(1, temperatureDeltaUnit).ToUnit(TemperatureDeltaUnit.Kelvin);
                //
                conversionToSI = (double)energy.Value / (mass.Value * temperatureDelta.Value);
            }
        }
        private static void GetConversionFromSI(out double conversionFromSI)
        {
            // To my unit
            if (_specificHeatUnit == MyUnit.SquareInchPerSquareSecondFahrenheit)
            {
                // 1 inch = 0.0254 meters
                // 1 s = 1 s
                // 1 ∆F = 0.555555556 ∆K
                conversionFromSI = 1 / 0.001161288;
            }
            // To no unit
            else if ((int)_energyUnit == MyUnit.NoUnit || (int)_massUnit == MyUnit.NoUnit ||
                     (int)_temperatureDeltaUnit == MyUnit.NoUnit)
            {
                conversionFromSI = 1;
            }
            // To supported unit
            else
            {
                Energy energy = Energy.From(1, EnergyUnit.Joule).ToUnit(_energyUnit);
                Mass mass = Mass.From(1, MassUnit.Kilogram).ToUnit(_massUnit);
                TemperatureDelta temperatureDelta =
                    TemperatureDelta.From(1, TemperatureDeltaUnit.Kelvin).ToUnit(_temperatureDeltaUnit);
                //
                conversionFromSI = (double)energy.Value / (mass.Value * temperatureDelta.Value);
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
                if ((int)_energyUnit == MyUnit.NoUnit || (int)_massUnit == MyUnit.NoUnit ||
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
            string supportedUnitAbbreviations = StringEnergyConverter.SupportedUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += StringMassConverter.SupportedUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += StringTemperatureConverter.SupportedDeltaUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += "Additionally supported abbreviations: " +
                                          MyUnit.SquareInchPerSquareSecondFahrenheitAbbreviation;
            return supportedUnitAbbreviations;
        }
    }


}