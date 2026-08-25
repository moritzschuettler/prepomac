// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;

namespace CaeResults
{
    [Serializable]
    public class ResultHistoryOutputFromExtractedEntry : ResultHistoryOutput
    {
        // Variables                                                                                                                
        public const string FieldName = "EXTRACTED";
        public const string ComponentName = "VALUE";

        private HistoryResultData _source;
        private string _entryName;


        // Properties                                                                                                               
        public HistoryResultData Source { get { return _source; } set { _source = CloneHistorySource(value); } }
        public string EntryName { get { return _entryName; } set { _entryName = value; } }


        // Constructors                                                                                                             
        public ResultHistoryOutputFromExtractedEntry(string name, HistoryResultData source, string entryName)
            : base(name, "", RegionTypeEnum.None)
        {
            Source = source;
            _entryName = entryName;
        }


        // Methods                                                                                                                  
        public override string[] GetParentNames()
        {
            return GetHistoryParentNames(_source);
        }
    }
}
