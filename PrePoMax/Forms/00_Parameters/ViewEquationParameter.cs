// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.ComponentModel;
using CaeGlobals;
using CaeModel;

namespace PrePoMax.Forms
{
    public class ViewEquationParameter
    {
        // Variables                                                                                                                
        // The equation is kept as the text the user typed, because EquationContainer evaluates an equation
        // before it stores it and a row is allowed to use a parameter that is defined below it
        private const string ErrorValue = "Error";
        private string _name;
        private string _equation;
        private string _evaluatesTo;
        private string _error;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Parameter name.")]
        public string Name
        {
            get { return _name; }
            set { _name = value == null ? "" : value.Trim(); }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Value/Equation")]
        [DescriptionAttribute("Parameter value or equation.")]
        public string Equation
        {
            get { return _equation; }
            set { _equation = value == null ? "" : value.Trim(); }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Evaluates to")]
        [DescriptionAttribute("Parameter value, or an error when the equation cannot be evaluated.")]
        public string EvaluatesTo { get { return _evaluatesTo; } }
        //
        [Browsable(false)]
        public string Error { get { return _error; } }


        // Constructors                                                                                                             
        public ViewEquationParameter()
        {
            _name = "";
            _equation = "";
            _evaluatesTo = "";
            _error = null;
        }
        public ViewEquationParameter(EquationParameter parameter)
        {
            _name = parameter.Name;
            _equation = parameter.Equation.String;
            _evaluatesTo = "";
            _error = null;
        }


        // Methods                                                                                                                  
        public void SetValue(double value)
        {
            _evaluatesTo = value.ToString();
            _error = null;
        }
        public void SetError(string error)
        {
            _evaluatesTo = ErrorValue;
            _error = error;
        }
        public EquationParameter GetParameter()
        {
            EquationParameter parameter = new EquationParameter();
            parameter.Name = _name;
            parameter.EquationStr = new EquationString(_equation);
            return parameter;
        }
    }
}
