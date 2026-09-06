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
using System.Runtime.Serialization;

namespace PrePoMax.Commands
{
    [Serializable]
    class CRunPythonScript : GeneralCommand
    {
        // Variables                                                                                                                
        private string _script;
        [OptionalField] private string _fileName;
        [OptionalField] private string[] _arguments;
        [NonSerialized] private PythonEngineResult _executionResult;


        // Properties                                                                                                               
        public PythonEngineResult ExecutionResult { get { return _executionResult; } }


        // Constructor                                                                                                              
        public CRunPythonScript(string script, string fileName = null, string[] arguments = null)
            : base("Run Python script")
        {
            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentException("The Python script must not be empty.", "script");
            //
            _script = script;
            _fileName = fileName;
            _arguments = arguments == null ? null : (string[])arguments.Clone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            _executionResult = receiver.RunPythonScript(_script, _fileName, _arguments);
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
