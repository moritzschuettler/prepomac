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
using PrePoMax.Python.Journal;
using System;
using System.Collections.Generic;
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
        // The script is emitted as it was recorded and is never expanded into the calls it made. Its
        // own commands were never recorded, and expanding it would replace a live computation with the
        // values it produced on its first run. A script run from a file read sys.argv and __file__ as the
        // engine set them, so the journal sets the same values around it and restores its own afterwards.
        public override void ToPython(PythonJournalWriter writer)
        {
            string source = _fileName == null ? "embedded script" : _fileName;
            writer.Comment("--- Recorded script: " + source + " ---");
            if (_fileName != null)
            {
                List<string> argv = new List<string>();
                argv.Add(writer.Path(_fileName));
                if (_arguments != null)
                {
                    foreach (string argument in _arguments) argv.Add(PythonValueFormatter.Text(argument));
                }
                writer.Line("import sys as _recorded_script_sys");
                writer.Assign("_recorded_script_saved_argv", "list(_recorded_script_sys.argv)");
                writer.Assign("_recorded_script_saved_file", "globals().get('__file__')");
                writer.Assign("_recorded_script_sys.argv", "[" + string.Join(", ", argv) + "]");
                writer.Assign("__file__", writer.Path(_fileName));
            }
            writer.Block(_script);
            if (_fileName != null)
            {
                writer.Assign("_recorded_script_sys.argv", "_recorded_script_saved_argv");
                writer.Assign("__file__", "_recorded_script_saved_file");
            }
            writer.Comment("--- End of recorded script ---");
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Embedded script";
        }
    }
}
