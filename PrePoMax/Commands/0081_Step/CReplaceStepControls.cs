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
    class CReplaceStepControls : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private StepControls _stepControls;

        // Constructor                                                                                                              
        public CReplaceStepControls(string stepName, StepControls stepControls)
            : base("Replace step controls")
        {
            _stepName = stepName;
            _stepControls = stepControls.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceStepControls(_stepName, _stepControls.DeepClone());
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            string step = "pmx.model.steps[" + PythonValueFormatter.Text(_stepName) + "]";
            if (_stepControls == null || _stepControls.Parameters == null || _stepControls.Parameters.Count == 0)
            {
                writer.Statement(writer.Call(step, "clear_controls"));
            }
            else
            {
                writer.Statement(writer.Call(step, "set_controls",
                                             PythonStepControlEmitter.GetParameters(_stepControls)));
            }
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName;
        }
    }
}
