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
using System.Collections.Generic;

namespace CaeResults
{
    [Serializable]
    public class ResultHistoryOutputFromMergedHistories : ResultHistoryOutput
    {
        // Variables                                                                                                                
        private HistoryResultData _source1;
        private HistoryResultData _source2;
        private HistoryResultData _source3;
        private HistoryResultData _source4;


        // Properties                                                                                                               
        public HistoryResultData Source1 { get { return _source1; } set { _source1 = CloneHistorySource(value); } }
        public HistoryResultData Source2 { get { return _source2; } set { _source2 = CloneHistorySource(value); } }
        public HistoryResultData Source3 { get { return _source3; } set { _source3 = CloneHistorySource(value); } }
        public HistoryResultData Source4 { get { return _source4; } set { _source4 = CloneHistorySource(value); } }


        // Constructors                                                                                                             
        public ResultHistoryOutputFromMergedHistories(string name, HistoryResultData source1, HistoryResultData source2,
                                                      HistoryResultData source3, HistoryResultData source4)
            : base(name, "", RegionTypeEnum.None)
        {
            Source1 = source1;
            Source2 = source2;
            Source3 = source3;
            Source4 = source4;
        }


        // Methods                                                                                                                  
        public HistoryResultData[] GetSources()
        {
            List<HistoryResultData> sources = new List<HistoryResultData>();
            if (_source1 != null) sources.Add(_source1);
            if (_source2 != null) sources.Add(_source2);
            if (_source3 != null) sources.Add(_source3);
            if (_source4 != null) sources.Add(_source4);
            return sources.ToArray();
        }
        public override string[] GetParentNames()
        {
            return GetHistoryParentNames(_source1, _source2, _source3, _source4);
        }
    }
}
