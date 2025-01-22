using NCalc.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnitsNet;

namespace CaeGlobals
{
    [Serializable]
    public class EquationContainer
    {
        // Variables                                                                                                                
        private Type _stringDoubleConverterType;
        private string _equation;
        private bool _constant;
        private double _constantValue;
        [NonSerialized] private Func<double, double> _checkValue;
        [NonSerialized] private Action _equationChanged;


        // Properties                                                                                                               
        public double Value { get { return GetValueFromEquation(_equation); } }
        public EquationString Equation
        {
            get { return new EquationString(_equation); }
            set
            {
                if (value == null) SetEquation(null, true);
                else SetEquation(value.Equation, true);
            }
        }
        public string String { get { return _equation; } }
        public Func<double, double> CheckValue { get { return _checkValue; } set { _checkValue = value; } }
        public Action EquationChanged { get { return _equationChanged; } set { _equationChanged = value; } }
       

        // Constructors                                                                                                             
        public EquationContainer(Type stringDoubleConverterType, double value, Func<double, double> checkValue = null,
                                 bool constant = false)
        {
            _stringDoubleConverterType = stringDoubleConverterType;
            _constantValue = value;
            _checkValue = checkValue;
            _constant = constant;
            SetEquationFromValue(value, false);
        }


        // Methods                                                                                                                  
        private string GetEquationFromValue(double value)
        {
            if (_stringDoubleConverterType == null) throw new NotSupportedException();
            else if (_constant) return _constantValue.ToString();
            else
            {
                TypeConverter stringDoubleConverter = (TypeConverter)Activator.CreateInstance(_stringDoubleConverterType);
                return (string)stringDoubleConverter.ConvertTo(value, typeof(string));
            }
        }
        private double GetValueFromEquation(string equation)
        {
            if (_stringDoubleConverterType == null) throw new NotSupportedException();
            else if (_constant) return _constantValue;
            else
            {
                TypeConverter stringDoubleConverter = (TypeConverter)Activator.CreateInstance(_stringDoubleConverterType);
                return Convert.ToDouble(stringDoubleConverter.ConvertFrom(equation));
            }
        }
        public void SetConverterType(Type stringDoubleConverterType)
        {
            if (IsEquation())
            {
                _stringDoubleConverterType = stringDoubleConverterType;
            }
            else
            {
                double value = Value;
                _stringDoubleConverterType = stringDoubleConverterType;
                SetEquationFromValue(value);
            }
            
        }
        public void SetEquation(EquationString equation)
        {
            SetEquation(equation.Equation);
        }
        public void SetEquation(string equation, bool enableEquationChanged = false)
        {
            try
            {
                if (equation == null) equation = "0";
                else
                {
                    equation = equation.Trim();
                    if (equation.Length == 0) equation = "0";
                }
                // Remove the result from the equation
                equation = equation.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[0];
                // Get the equation value
                double equationValue = GetValueFromEquation(equation);
                // Check the equation value
                double checkedValue = _checkValue != null ? _checkValue(equationValue) : equationValue;
                // If the check changed the value, apply changed value to the equation
                if (equationValue != checkedValue && !(double.IsNaN(equationValue) && double.IsNaN(checkedValue)) &&
                    !(double.IsInfinity(equationValue) && double.IsInfinity(checkedValue)))
                {
                    _equation = GetEquationFromValue(checkedValue);
                    if (enableEquationChanged) _equationChanged?.Invoke();
                }
                else
                {
                    bool isEquation = false;
                    if (equation.StartsWith("="))
                    {
                        // Check invalid doubles
                        if (double.IsNaN(equationValue)) throw new CaeException("The equation value is equal to NaN.");
                        if (double.IsInfinity(equationValue)) throw new CaeException("The equation value is equal to infinity.");
                        isEquation = true;
                    }
                    // If the equation changed, apply changed value to the equation
                    if (_equation != equation)
                    {
                        if (isEquation) equation = equation.Replace(" ", "");
                        // Add unit to the equation if there is none
                        else if (double.TryParse(equation, out double number) && number == equationValue)
                            equation = GetEquationFromValue(equationValue);
                        //
                        _equation = equation;
                        if (enableEquationChanged) _equationChanged?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CaeException("Equation error: " + equation + Environment.NewLine + ex.Message);
            }
        }
        public void SetEquationFromValue(double value, bool enableEquationChanged = false)
        {
            SetEquation(GetEquationFromValue(value), enableEquationChanged);
        }
        public void CheckEquation()
        {
            SetEquation(_equation);
        }
        public bool IsEquation()
        {
            if (_equation != null && _equation.StartsWith("=")) return true;
            else return false;
        }
        //
        public static void SetAndCheck(ref EquationContainer variable, EquationContainer value, Func<double, double> CheckValue,
                                       bool check)
        {
            SetAndCheck(ref variable, value, CheckValue, null, check);
        }
        public static void SetAndCheck(ref EquationContainer[][] variable, EquationContainer[][] value,
                                       Func<double, double>[] CheckValue, bool check)
        {
            Func<double, double> checkFunction;
            variable = new EquationContainer[value.Length][];
            //
            for (int i = 0; i < value.Length; i++)
            {
                variable[i] = new EquationContainer[value[i].Length];
                for (int j = 0; j < value[i].Length; j++)
                {
                    if (CheckValue == null) checkFunction = null;
                    else checkFunction = CheckValue[j];
                    SetAndCheck(ref variable[i][j], value[i][j], checkFunction, null, check);
                }
            }
        }

        public static void SetAndCheck(ref EquationContainer variable, EquationContainer value, Func<double, double> CheckValue,
                                       Action EquationChangedCallback, bool check)
        {
            if (value == null)
            {
                variable = null;
                return;
            }
            //
            string prevEquation = variable != null ? variable.String : value.String;
            //
            value.CheckValue = CheckValue;
            value.EquationChanged = EquationChangedCallback;
            //
            if (check)
            {
                value.CheckEquation();
                if (variable != null && prevEquation != variable.String) EquationChangedCallback?.Invoke();
            }
            //
            variable = value;
        }
    }
}
