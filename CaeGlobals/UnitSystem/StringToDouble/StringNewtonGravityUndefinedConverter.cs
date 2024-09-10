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
using System.Windows.Forms;

namespace CaeGlobals
{
    public enum NewtonGravityUnit { }
    public class StringNewtonGravityUndefinedConverter : TypeConverter
    {
        // Variables                                                                                                                
        protected static ForceUnit _forceUnit = ForceUnit.Newton;
        protected static LengthUnit _lengthUnit = LengthUnit.Meter;
        protected static MassUnit _massUnit = MassUnit.Kilogram;
        protected static NewtonGravityUnit _newtonGravityUnit = (NewtonGravityUnit)MyUnit.NoUnit;
        protected static string error = "Unable to parse quantity. Expected the form \"{value} {unit abbreviation}" +
                                        "\", such as \"5.5 m\". The spacing is optional.";
        //
        protected static ArrayList values;
        protected static double _initialValue = 0;      // use initial value for the constructor to work
        protected static string _undefined = "Undefined";


        // Properties                                                                                                               
        public static string SetForceUnit
        {
            set
            {
                if (value == "")
                {
                    _forceUnit = (ForceUnit)MyUnit.NoUnit;
                    _lengthUnit = (LengthUnit)MyUnit.NoUnit;
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                }
                else
                {
                    _forceUnit = Force.ParseUnit(value);
                }
                //
                _newtonGravityUnit = (NewtonGravityUnit)MyUnit.NoUnit;
            }
        }
        public static string SetLengthUnit
        {
            set
            {
                if (value == "")
                {
                    _forceUnit = (ForceUnit)MyUnit.NoUnit;
                    _lengthUnit = (LengthUnit)MyUnit.NoUnit;
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                }
                else
                {
                    _lengthUnit = Length.ParseUnit(value);
                }
                //
                _newtonGravityUnit = (NewtonGravityUnit)MyUnit.NoUnit;
            }
        }
        public static string SetMassUnit
        {
            set
            {
                if (value == "")
                {
                    _forceUnit = (ForceUnit)MyUnit.NoUnit;
                    _lengthUnit = (LengthUnit)MyUnit.NoUnit;
                    _massUnit = (MassUnit)MyUnit.NoUnit;
                }
                else
                {
                    if (value == MyUnit.PoundForceSquareSecondPerInchAbbreviation)
                        _massUnit = MyUnit.PoundForceSquareSecondPerInch;
                    else _massUnit = Mass.ParseUnit(value);
                }
                //
                _newtonGravityUnit = (NewtonGravityUnit)MyUnit.NoUnit;
            }
        }
        public static string SetUnit
        {
            set
            {
                if (value == MyUnit.QuadInchPerPoundForceQuadSecondAbbreviation)
                    _newtonGravityUnit = MyUnit.QuadInchPerPoundForceQuadSecond;
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
        public static string GetUnitAbbreviation(ForceUnit forceUnit, LengthUnit lengthUnit, MassUnit massUnit,
                                                 NewtonGravityUnit newtonGravityUnit)
        {
            string unit;
            if (newtonGravityUnit == MyUnit.QuadInchPerPoundForceQuadSecond)
                unit = MyUnit.QuadInchPerPoundForceQuadSecondAbbreviation;
            else if ((int)forceUnit == MyUnit.NoUnit || (int)lengthUnit == MyUnit.NoUnit ||
                     (int)massUnit == MyUnit.NoUnit) unit = "";
            else unit = Force.GetAbbreviation(forceUnit) + "·" + Length.GetAbbreviation(lengthUnit) + "²/" +
                        Mass.GetAbbreviation(massUnit) + "²";
            return unit;
        }
        
        
        // Constructors                                                                                                             
        public StringNewtonGravityUndefinedConverter()
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
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
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
                            string unit = GetUnitAbbreviation(_forceUnit, _lengthUnit, _massUnit, _newtonGravityUnit);
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
            if (valueWithUnitString.Contains(MyUnit.QuadInchPerPoundForceQuadSecondAbbreviation))
            {
                valueWithUnitString = valueWithUnitString.Replace(MyUnit.QuadInchPerPoundForceQuadSecondAbbreviation, "");
                if (double.TryParse(valueWithUnitString, out value))
                {
                    // 1 inch = 0.0254 meters
                    // 1 pound force = 4.44822162 newtons
                    // 1 s = 1 s
                    conversionToSI = 9.35725E-08;
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
                string[] tmp = valueWithUnitString.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length != 2) throw new FormatException(error);
                string denominator = tmp[1];
                //
                tmp = tmp[0].Split(new string[] { "*", "·" }, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length != 2) throw new FormatException(error);
                //
                Force force = Force.Parse(tmp[0]);
                value = (double)force.Value;
                ForceUnit forceUnit = force.Unit;
                force = Force.From(1, forceUnit).ToUnit(ForceUnit.Newton);
                //
                if (tmp[1].EndsWith("²") || tmp[1].EndsWith("^2")) tmp[1] = tmp[1].Replace("²", "").Replace("^2", "");
                else throw new FormatException(error);
                LengthUnit lengthUnit = Length.ParseUnit(tmp[1]);
                Length length = Length.From(1, lengthUnit).ToUnit(LengthUnit.Meter);
                //
                if (denominator.EndsWith("²") || denominator.EndsWith("^2"))
                    denominator = denominator.Replace("²", "").Replace("^2", "");
                else throw new FormatException(error);
                MassUnit massUnit = Mass.ParseUnit(denominator);
                Mass mass = Mass.From(1, massUnit).ToUnit(MassUnit.Kilogram);
                //
                conversionToSI = (double)force.Value * Math.Pow(length.Value, 2) / Math.Pow(mass.Value, 2);
            }
        }
        private static void GetConversionFromSI(out double conversionFromSI)
        {
            // To my unit
            if (_newtonGravityUnit == MyUnit.QuadInchPerPoundForceQuadSecond)
            {
                // 1 inch = 0.0254 meters
                // 1 pound force = 4.44822162 newtons
                // 1 s = 1 s
                conversionFromSI = 1 / 9.35725E-08;
            }
            // To no unit
            else if ((int)_forceUnit == MyUnit.NoUnit || (int)_lengthUnit == MyUnit.NoUnit || (int)_massUnit == MyUnit.NoUnit)
            {
                conversionFromSI = 1;
            }
            // To supported unit
            else
            {
                Force force = Force.From(1, ForceUnit.Newton).ToUnit(_forceUnit);
                Length length = Length.From(1, LengthUnit.Meter).ToUnit(_lengthUnit);
                Mass mass = Mass.From(1, MassUnit.Kilogram).ToUnit(_massUnit);
                //
                conversionFromSI = (double)force.Value * Math.Pow(length.Value, 2) / Math.Pow(mass.Value, 2);
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
                if ((int)_forceUnit == MyUnit.NoUnit || (int)_lengthUnit == MyUnit.NoUnit || (int)_massUnit == MyUnit.NoUnit) { }
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
                throw new Exception(ex.Message + Environment.NewLine + Environment.NewLine + SupportedUnitAbbreviations());
            }
        }
        public static string SupportedUnitAbbreviations()
        {
            string supportedUnitAbbreviations = StringForceConverter.SupportedUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += StringLengthConverter.SupportedUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += StringMassConverter.SupportedUnitAbbreviations();
            supportedUnitAbbreviations += Environment.NewLine + Environment.NewLine;
            supportedUnitAbbreviations += "Additionally supported abbreviations: " +
                                          MyUnit.QuadInchPerPoundForceQuadSecondAbbreviation;
            return supportedUnitAbbreviations;
        }
    }


}