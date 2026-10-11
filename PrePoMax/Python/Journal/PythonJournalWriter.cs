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
using System.Text;
using PrePoMax.Commands;

namespace PrePoMax.Python.Journal
{
    // Collects the lines of an emitted session journal. A command writes through this rather than
    // returning a string, because one command often needs several lines, may need to bind a variable
    // the following commands use, and must be able to refuse visibly.
    public class PythonJournalWriter
    {
        // Variables                                                                                                                
        private readonly StringBuilder _builder;
        private readonly PythonJournalScope _scope;
        private readonly List<string> _refusals;
        private readonly List<int> _commandFirstLineIndices;
        private int _emittedCommandCount;
        private int _commandIndex;
        private int _lineIndex;
        private int _numOfLegacySplitFaces;
        private string _workDirectory;
        private bool _writesToWorkDirectory;


        // Properties                                                                                                               
        public PythonJournalScope Scope { get { return _scope; } }
        // The work directory of the session the journal is written in, which a replay is run in with -w, so that a
        // command holding a file name a regeneration rooted there can write the name the API takes
        public string WorkDirectory { get { return _workDirectory; } set { _workDirectory = value; } }
        // True once a file the session saved or exported is written into the work directory of the replay, which
        // the header then says and imports os for
        public bool WritesToWorkDirectory { get { return _writesToWorkDirectory; } }
        public List<string> Refusals { get { return _refusals; } }
        public int EmittedCommandCount { get { return _emittedCommandCount; } }
        // The position of the command being emitted, so that one which only makes sense at the start of
        // a session can tell whether it is there
        public int CommandIndex { get { return _commandIndex; } set { _commandIndex = value; } }
        public int RefusedCommandCount { get { return _refusals.Count; } }
        // The line each command started at, so that a caller showing the script can pair a line with its command
        public List<int> CommandFirstLineIndices { get { return _commandFirstLineIndices; } }
        // The number of faces a command made before Split Periodic Faces became a command split the periodic faces
        // into, so that its script splits them as its regeneration does
        public int NumOfLegacySplitFaces
        {
            get { return _numOfLegacySplitFaces; }
            set { _numOfLegacySplitFaces = Math.Max(value, 1); }
        }


        // Constructors                                                                                                             
        public PythonJournalWriter()
        {
            _builder = new StringBuilder();
            _scope = new PythonJournalScope();
            _refusals = new List<string>();
            _commandFirstLineIndices = new List<int>();
            _emittedCommandCount = 0;
            _commandIndex = 0;
            _lineIndex = 0;
            _numOfLegacySplitFaces = 1;
            _workDirectory = null;
            _writesToWorkDirectory = false;
        }


        // Methods                                                                                                                  
        public void BeginCommand(int commandIndex)
        {
            _commandIndex = commandIndex;
            _commandFirstLineIndices.Add(_lineIndex);
        }
        public void Line(string line)
        {
            _builder.AppendLine(line == null ? "" : line);
            _lineIndex++;
        }
        public void Blank()
        {
            _builder.AppendLine();
            _lineIndex++;
        }
        public void Comment(string comment)
        {
            Line("# " + comment);
        }
        public void Block(string block)
        {
            if (block == null) return;
            // A recorded script is emitted as it was written, so its own line breaks and the spaces that
            // end a line inside one of its strings are kept
            foreach (string line in block.Replace("\r\n", "\n").Split('\n')) Line(line);
        }
        public void Unsupported(Command command, string reason)
        {
            string name = command == null ? "Unknown command" : command.Name;
            string type = command == null ? "" : " (" + command.GetType().Name + ")";
            //
            Comment("NOT EMITTED - " + name + type + ": " + reason);
            _refusals.Add(name + type + ": " + reason);
        }
        public void CountEmittedCommand()
        {
            _emittedCommandCount++;
        }
        public string Value(object value)
        {
            return PythonValueFormatter.Format(value);
        }
        public string Path(string value)
        {
            return PythonValueFormatter.Path(value);
        }
        // A file the session saved or exported is written by the replay under its name in the work directory of
        // the replay, as a batch regeneration writes it, so a replay never overwrites the file the session wrote
        public string OutputPath(string fileName)
        {
            _writesToWorkDirectory = true;
            return "os.path.join(pmx.work_directory, " +
                   PythonValueFormatter.Path(System.IO.Path.GetFileName(fileName)) + ")";
        }
        public string Call(string target, string method, params string[] arguments)
        {
            string call = Join(method, arguments);
            if (string.IsNullOrEmpty(target)) return call;
            //
            return target + "." + call;
        }
        public static string Join(string callee, params string[] arguments)
        {
            return callee + "(" + ArgumentList(arguments) + ")";
        }
        // A call carrying a rebuilt selection or a long id list is far too long for one line, so the
        // emitted code is wrapped; a comment and a recorded script keep the lines they were written with
        public void Statement(string statement)
        {
            List<string> wrapped = PythonCodeFormatter.Wrap(statement);
            for (int i = 0; i < wrapped.Count; i++) Line(wrapped[i]);
        }
        public void Assign(string variableName, string expression)
        {
            Statement(variableName + " = " + expression);
        }
        // A null argument is one the caller chose to leave at its default, so it is left out
        public static string ArgumentList(params string[] arguments)
        {
            if (arguments == null) return "";
            //
            List<string> given = new List<string>();
            foreach (string argument in arguments)
            {
                if (argument != null) given.Add(argument);
            }
            //
            return string.Join(", ", given);
        }
        public string GetText()
        {
            return _builder.ToString();
        }
    }
}
