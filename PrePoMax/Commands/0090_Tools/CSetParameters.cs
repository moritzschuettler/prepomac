// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    /// <summary>
    /// Stores the complete parameter table produced by one editing session. A parameter may use a parameter
    /// that is defined below it, so the table is applied at once: applying it one parameter at a time would
    /// leave the model holding an equation whose parameter is not there yet.
    /// </summary>
    [Serializable]
    class CSetParameters : PreprocessCommand
    {
        // Variables                                                                                                                
        private EquationParameter[] _parameters;


        // Constructor                                                                                                              
        public CSetParameters(EquationParameter[] parameters)
            : base("Set parameters")
        {
            _parameters = new EquationParameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) _parameters[i] = parameters[i].DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            EquationParameter[] parameters = new EquationParameter[_parameters.Length];
            for (int i = 0; i < _parameters.Length; i++) parameters[i] = _parameters[i].DeepClone();
            //
            receiver.SetParameters(parameters);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            List<object> names = new List<object>();
            List<object> equations = new List<object>();
            foreach (EquationParameter parameter in _parameters)
            {
                names.Add(parameter.Name);
                equations.Add(parameter.Equation);
            }
            //
            writer.Statement(writer.Call("pmx.model.parameters", "set_all", PythonValueFormatter.List(names),
                                         PythonValueFormatter.List(equations)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            string[] nameEquations = new string[_parameters.Length];
            for (int i = 0; i < _parameters.Length; i++)
                nameEquations[i] = _parameters[i].Name + " = " + _parameters[i].Equation.String;
            //
            return base.GetCommandString() + GetArrayAsString(nameEquations);
        }
    }
}
