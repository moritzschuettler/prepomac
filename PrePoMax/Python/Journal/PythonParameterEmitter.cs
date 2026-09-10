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
using CaeGlobals;
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // A parameter is a name, a value or an equation, and a description that is left out of the call
    // when it is empty. The whole table is emitted as one set_all, because a parameter of it may use a
    // parameter that is given after it, which add cannot express.
    public static class PythonParameterEmitter
    {
        // Methods                                                                                                                  
        public static void Add(PythonJournalWriter writer, EquationParameter parameter)
        {
            writer.Statement(writer.Call("pmx.model.parameters", "add", PythonValueFormatter.Text(parameter.Name),
                                         PythonValueFormatter.Equation(parameter.Equation),
                                         GetDescription(parameter)));
            writer.CountEmittedCommand();
        }
        public static void Replace(PythonJournalWriter writer, string oldName, EquationParameter parameter)
        {
            writer.Statement(writer.Call("pmx.model.parameters", "replace", PythonValueFormatter.Text(oldName),
                                         PythonValueFormatter.Text(parameter.Name),
                                         PythonValueFormatter.Equation(parameter.Equation),
                                         GetDescription(parameter)));
            writer.CountEmittedCommand();
        }
        public static void SetAll(PythonJournalWriter writer, EquationParameter[] parameters)
        {
            bool described = false;
            List<object> names = new List<object>();
            List<object> equations = new List<object>();
            List<object> descriptions = new List<object>();
            //
            foreach (EquationParameter parameter in parameters)
            {
                names.Add(parameter.Name);
                equations.Add(parameter.Equation);
                descriptions.Add(parameter.Description == null ? "" : parameter.Description);
                if (!string.IsNullOrEmpty(parameter.Description)) described = true;
            }
            //
            // The descriptions are the third positional argument, so that the emitted name is not read
            // as a member of the API by the coverage audit
            string descriptionArgument = described ? PythonValueFormatter.List(descriptions) : null;
            writer.Statement(writer.Call("pmx.model.parameters", "set_all", PythonValueFormatter.List(names),
                                         PythonValueFormatter.List(equations), descriptionArgument));
            writer.CountEmittedCommand();
        }
        private static string GetDescription(EquationParameter parameter)
        {
            if (string.IsNullOrEmpty(parameter.Description)) return null;
            //
            return PythonValueFormatter.Argument("description", PythonValueFormatter.Text(parameter.Description));
        }
    }
}
