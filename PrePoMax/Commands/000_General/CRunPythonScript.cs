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
using System.Text;

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
        // values it produced on its first run. The engine ran it in a namespace of its own holding only
        // pmx, __name__ and __file__, so the journal executes it in the same kind of namespace: a script
        // that binds a name the journal uses, such as selection_1, cannot change the lines after it. A
        // script run from a file read sys.argv as the engine set it, so the journal sets the same value
        // around it and restores its own afterwards.
        public override void ToPython(PythonJournalWriter writer)
        {
            string source = _fileName == null ? "embedded script" : _fileName;
            string scriptFileName = _fileName == null ? PythonValueFormatter.Text("<string>") : writer.Path(_fileName);
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
                writer.Assign("_recorded_script_sys.argv", "[" + string.Join(", ", argv) + "]");
            }
            writer.Line("_recorded_script_source = '''\\");
            writer.Block(GetTripleQuotedBody(_script) + "'''");
            writer.Statement("exec(compile(_recorded_script_source, " + scriptFileName + ", 'exec'), " +
                             "{'__name__': '__main__', '__file__': " + scriptFileName + ", 'pmx': pmx})");
            if (_fileName != null) writer.Assign("_recorded_script_sys.argv", "_recorded_script_saved_argv");
            writer.Comment("--- End of recorded script ---");
            writer.CountEmittedCommand();
        }
        // The body of a ''' literal that holds the script unchanged and readable: a backslash is doubled,
        // and a quote is escaped only where it could close the literal - before another quote or at the
        // end. Python compiles any line break as a newline, so the breaks are normalized.
        private static string GetTripleQuotedBody(string script)
        {
            string text = script.Replace("\r\n", "\n").Replace('\r', '\n');
            StringBuilder builder = new StringBuilder(text.Length + 16);
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == '\\') builder.Append("\\\\");
                else if (character == '\'' && (i == text.Length - 1 || text[i + 1] == '\'')) builder.Append("\\'");
                else builder.Append(character);
            }
            //
            return builder.ToString();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Embedded script";
        }
    }
}
