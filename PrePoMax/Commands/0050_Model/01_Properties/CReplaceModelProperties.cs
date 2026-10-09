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
    class CReplaceModelProperties : PreprocessCommand, IFileDependentCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private string _newModelName;
        private ModelProperties _newModelProperties;


        // Properties                                                                                                               
        public string FileName
        {
            get { return _newModelProperties.GlobalResultsFileName; }   // null if not a submodel
            set { if (_newModelProperties.ModelType == ModelType.Submodel) _newModelProperties.GlobalResultsFileName = value; }
        }


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
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            if (FileName != null)
            {
                string fileName = receiver.GetFileNameToImport("Replace file: " + FileName, "Result files|*.frd");
                if (fileName != null) FileName = fileName;
            }
            return Execute(receiver);
        }

        // Every value the dialog sets is written, because a replace keeps what it is not given and a journal that
        // left out the slip wear or submodel settings replayed a different analysis
        public override void ToPython(PythonJournalWriter writer)
        {
            List<string> arguments = new List<string>();
            arguments.Add("name=" + PythonValueFormatter.Text(_newModelName));
            arguments.Add("model_type=" + PythonValueFormatter.Text(_newModelProperties.ModelType.ToString()));
            // The replace refuses a value the model type does not use, and keeps the hidden one it is not given
            if (_newModelProperties.ModelType == ModelType.Submodel && _newModelProperties.GlobalResultsFileName != null)
                arguments.Add("global_results_file_name=" + writer.Path(_newModelProperties.GlobalResultsFileName));
            if (_newModelProperties.ModelType == ModelType.SlipWearModel)
            {
                arguments.Add("slip_wear_results=" +
                              PythonValueFormatter.Text(_newModelProperties.SlipWearResults.ToString()));
                arguments.Add("number_of_cycles=" + PythonValueFormatter.Equation(_newModelProperties.NumberOfCycles));
                arguments.Add("cycles_increment=" + PythonValueFormatter.Equation(_newModelProperties.CyclesIncrement));
                arguments.Add("number_of_smoothing_steps=" +
                              PythonValueFormatter.Equation(_newModelProperties.NumOfSmoothingSteps));
                arguments.Add("bdm_remeshing=" + PythonValueFormatter.Format(_newModelProperties.BdmRemeshing));
            }
            // An undefined physical constant is stored as infinity, which has no Python form; the replace makes
            // every constant it is not given undefined when told to, so the replay sets exactly these constants
            bool anyConstantUndefined = false;
            anyConstantUndefined |= AddConstant(arguments, "absolute_zero", _newModelProperties.AbsoluteZero);
            anyConstantUndefined |= AddConstant(arguments, "stefan_boltzmann", _newModelProperties.StefanBoltzmann);
            anyConstantUndefined |= AddConstant(arguments, "newton_gravity", _newModelProperties.NewtonGravity);
            if (anyConstantUndefined) arguments.Add("clear_undefined_constants=True");
            writer.Statement(writer.Call("pmx.model.properties", "replace", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        private static bool AddConstant(List<string> arguments, string argumentName, double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value)) return true;
            //
            arguments.Add(argumentName + "=" + PythonValueFormatter.Number(value));
            return false;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _newModelName + ", " + _newModelProperties.ToString();
        }
    }
}
