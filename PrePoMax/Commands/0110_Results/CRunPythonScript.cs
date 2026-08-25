// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Python;
using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CRunPythonScript : PostprocessCommand
    {
        // Variables                                                                                                                
        private string _script;
        [NonSerialized] private PythonEngineResult _executionResult;


        // Properties                                                                                                               
        public PythonEngineResult ExecutionResult { get { return _executionResult; } }


        // Constructor                                                                                                              
        public CRunPythonScript(string script)
            : base("Run Python script")
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentException("The Python script must not be empty.", "script");
            //
            _script = script;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            _executionResult = receiver.RunPythonScript(_script);
            //
            if (!string.IsNullOrEmpty(_executionResult.Error))
                throw new CaeException(_executionResult.Error.TrimEnd());
            //
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Embedded script";
        }
    }
}

