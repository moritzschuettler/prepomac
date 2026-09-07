// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;

namespace PrePoMax.Python.Journal
{
    // What an export produced. The refusals are carried out of the exporter rather than only written
    // into the file, so that a caller can report them and a test can fail on them.
    public class PythonJournalReport
    {
        // Variables                                                                                                                
        private readonly string _fileName;
        private readonly int _commandCount;
        private readonly int _emittedCommandCount;
        private readonly List<string> _refusals;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } }
        public int CommandCount { get { return _commandCount; } }
        public int EmittedCommandCount { get { return _emittedCommandCount; } }
        public int RefusedCommandCount { get { return _refusals.Count; } }
        public List<string> Refusals { get { return _refusals; } }


        // Constructors                                                                                                             
        public PythonJournalReport(string fileName, int commandCount, int emittedCommandCount, List<string> refusals)
        {
            _fileName = fileName;
            _commandCount = commandCount;
            _emittedCommandCount = emittedCommandCount;
            _refusals = refusals == null ? new List<string>() : new List<string>(refusals);
        }


        // Methods                                                                                                                  
        public string GetSummary()
        {
            string summary = _commandCount + " commands, " + RefusedCommandCount + " not emitted";
            if (_fileName != null) summary = "Session journal: " + _fileName + " - " + summary;
            //
            return summary;
        }
    }
}
