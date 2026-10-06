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
    [Serializable]
    class CReplaceModelProperties : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _newModelName;
        private ModelProperties _newModelProperties;


        // Constructor                                                                                                              
        public CReplaceModelProperties(string newModelName, ModelProperties newModelProperties)
            : base("Replace model properties")
        {
            _newModelName = newModelName;
            _newModelProperties = newModelProperties;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceModelProperties(_newModelName, _newModelProperties);
            return true;
        }

        // Every value the dialog sets is written, because a replace keeps what it is not given and a journal that
        // left out the slip wear or submodel settings replayed a different analysis
        public override void ToPython(PythonJournalWriter writer)
        {
            List<string> arguments = new List<string>();
            arguments.Add("name=" + PythonValueFormatter.Text(_newModelName));
            arguments.Add("model_type=" + PythonValueFormatter.Text(_newModelProperties.ModelType.ToString()));
            if (_newModelProperties.GlobalResultsFileName != null)
                arguments.Add("global_results_file_name=" + writer.Path(_newModelProperties.GlobalResultsFileName));
            arguments.Add("slip_wear_results=" +
                          PythonValueFormatter.Text(_newModelProperties.SlipWearResults.ToString()));
            arguments.Add("number_of_cycles=" + PythonValueFormatter.Equation(_newModelProperties.NumberOfCycles));
            arguments.Add("cycles_increment=" + PythonValueFormatter.Equation(_newModelProperties.CyclesIncrement));
            arguments.Add("number_of_smoothing_steps=" +
                          PythonValueFormatter.Equation(_newModelProperties.NumOfSmoothingSteps));
            arguments.Add("bdm_remeshing=" + PythonValueFormatter.Format(_newModelProperties.BdmRemeshing));
            arguments.Add("absolute_zero=" + PythonValueFormatter.Number(_newModelProperties.AbsoluteZero));
            arguments.Add("stefan_boltzmann=" + PythonValueFormatter.Number(_newModelProperties.StefanBoltzmann));
            arguments.Add("newton_gravity=" + PythonValueFormatter.Number(_newModelProperties.NewtonGravity));
            writer.Statement(writer.Call("pmx.model.properties", "replace", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _newModelName + ", " + _newModelProperties.ToString();
        }
    }
}
