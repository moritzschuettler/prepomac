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
using CaeGlobals;
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
        private int _sessionStartCommandCount;
        private int _lineIndex;
        private int _numOfLegacySplitFaces;
        private string _workDirectory;
        private bool _writesToWorkDirectory;
        private string _sessionStartFileName;
        private readonly Dictionary<string, string> _outputFileNames;
        private readonly HashSet<string> _usedOutputFileNames;
        private readonly Dictionary<string, HashSet<string>> _removedNames;


        // Properties                                                                                                               
        public PythonJournalScope Scope { get { return _scope; } }
        // The work directory of the session the journal is written in, which a replay is run in with -w, so that a
        // command holding a file name a regeneration rooted there can write the name the API takes
        public string WorkDirectory { get { return _workDirectory; } set { _workDirectory = value; } }
        // True once a file the session saved or exported is written into the work directory of the replay, which
        // the header then says and imports os for
        public bool WritesToWorkDirectory { get { return _writesToWorkDirectory; } }
        // The model file a history begins by opening, which the journal cannot rebuild, so the header tells to
        // replay it with -f naming that file
        public string SessionStartFileName
        {
            get { return _sessionStartFileName; }
            set { _sessionStartFileName = value; }
        }
        public List<string> Refusals { get { return _refusals; } }
        public int EmittedCommandCount { get { return _emittedCommandCount; } }
        // True while nothing that builds the model has been emitted, so that a command which only makes sense at
        // the start of a session, such as a clear, can tell whether it is there. The compatibility commands an
        // older history starts with emit nothing, so they do not move the start.
        public bool IsAtSessionStart { get { return _emittedCommandCount == _sessionStartCommandCount; } }
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
            _sessionStartCommandCount = 0;
            _lineIndex = 0;
            _numOfLegacySplitFaces = 1;
            _workDirectory = null;
            _writesToWorkDirectory = false;
            _outputFileNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _usedOutputFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _removedNames = new Dictionary<string, HashSet<string>>();
        }


        // Methods                                                                                                                  
        public void BeginCommand(int commandIndex)
        {
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
            // A reason taken from an exception message may span lines, and every one of them must stay a comment
            foreach (string line in comment.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                Line("# " + line);
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
        public void CountSessionStartCommand()
        {
            _emittedCommandCount++;
            _sessionStartCommandCount++;
        }
        public string Value(object value)
        {
            return PythonValueFormatter.Format(value);
        }
        // A file the session read after writing it is read where the replay wrote it
        public string Path(string value)
        {
            string key = GetPathKey(value);
            string outputFileName;
            if (key != null && _outputFileNames.TryGetValue(key, out outputFileName))
                return "os.path.join(pmx.work_directory, " + PythonValueFormatter.Path(outputFileName) + ")";
            //
            return PythonValueFormatter.Path(value);
        }
        // A file the session saved or exported is written by the replay under its name in the work directory of
        // the replay, through the header's _output, which refuses a file there that the replay did not write. Two
        // files of the same name the session wrote in different folders are given numbered names.
        public string OutputPath(string fileName)
        {
            _writesToWorkDirectory = true;
            return "_output(" + PythonValueFormatter.Path(GetOutputFileName(fileName)) + ")";
        }
        private string GetOutputFileName(string fileName)
        {
            string key = GetPathKey(fileName);
            string outputFileName;
            if (key == null) return null;
            if (_outputFileNames.TryGetValue(key, out outputFileName)) return outputFileName;
            //
            string originalFileName = System.IO.Path.GetFileName(key);
            string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(originalFileName);
            string extension = System.IO.Path.GetExtension(originalFileName);
            outputFileName = originalFileName;
            for (int i = 2; _usedOutputFileNames.Contains(outputFileName); i++)
                outputFileName = nameWithoutExtension + "_" + i + extension;
            //
            _usedOutputFileNames.Add(outputFileName);
            _outputFileNames.Add(key, outputFileName);
            return outputFileName;
        }
        private static string GetPathKey(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            //
            string globalFileName = Tools.GetGlobalPath(fileName);
            try
            {
                return System.IO.Path.GetFullPath(globalFileName);
            }
            catch (Exception)
            {
                return globalFileName;
            }
        }
        // A replace keeps a reference it is not given, and the model keeps a reference to an item that a rename or a
        // remove took away after it was made, so a replace leaves such a reference out rather than name an item the
        // API does not find. The names taken away are kept per collection as the script writes them.
        public bool IsRemoved(string collection, string name)
        {
            if (collection == null || string.IsNullOrEmpty(name)) return false;
            //
            HashSet<string> removedNames;
            return _removedNames.TryGetValue(collection, out removedNames) &&
                   removedNames.Contains(PythonValueFormatter.Text(name));
        }
        public string KeptReference(string collection, string name, bool replace)
        {
            return replace && IsRemoved(collection, name) ? null : name;
        }
        public void RemoveNames(string collection, string[] names)
        {
            if (collection == null || names == null) return;
            //
            foreach (string name in names)
            {
                if (name != null) RemoveName(collection, PythonValueFormatter.Text(name));
            }
        }
        public void RenameName(string collection, string oldName, string newName)
        {
            if (collection == null || oldName == null || newName == null) return;
            //
            string oldText = PythonValueFormatter.Text(oldName);
            string newText = PythonValueFormatter.Text(newName);
            HashSet<string> removedNames;
            if (_removedNames.TryGetValue(collection, out removedNames)) removedNames.Remove(newText);
            if (!string.Equals(oldText, newText, StringComparison.OrdinalIgnoreCase)) RemoveName(collection, oldText);
        }
        // An imported file or a recorded script can add items under names the journal never writes
        public void ForgetRemovedNames()
        {
            _removedNames.Clear();
        }
        private void RemoveName(string collection, string nameText)
        {
            HashSet<string> removedNames;
            if (!_removedNames.TryGetValue(collection, out removedNames))
            {
                removedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _removedNames.Add(collection, removedNames);
            }
            removedNames.Add(nameText);
        }
        // An add or a replace naming an item brings its name back, a replace given a new name takes the old one
        // away, and a duplicate or a merge adds items under names it does not write
        private void TrackRemovedNames(string collection, string method, string[] arguments)
        {
            if (collection == null || method == null || arguments == null) return;
            //
            if (method == "duplicate" || method.StartsWith("merge"))
            {
                _removedNames.Remove(collection);
                return;
            }
            if (!method.StartsWith("add") && !method.StartsWith("replace")) return;
            //
            HashSet<string> removedNames;
            if (_removedNames.TryGetValue(collection, out removedNames))
            {
                foreach (string argument in arguments)
                {
                    if (argument != null) removedNames.Remove(argument);
                }
            }
            if (method.StartsWith("replace") && arguments.Length >= 2 && IsText(arguments[0]) && IsText(arguments[1]) &&
                !string.Equals(arguments[0], arguments[1], StringComparison.OrdinalIgnoreCase))
                RemoveName(collection, arguments[0]);
        }
        private static bool IsText(string argument)
        {
            return argument != null && argument.StartsWith("'");
        }
        public string Call(string target, string method, params string[] arguments)
        {
            TrackRemovedNames(target, method, arguments);
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
        // A positional argument a replace leaves out is held as a null, and every positional argument after it is
        // written by its keyword
        public static void AddPositional(List<string> arguments, string keyword, string value)
        {
            if (value != null && arguments.Contains(null)) arguments.Add(keyword + "=" + value);
            else arguments.Add(value);
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
