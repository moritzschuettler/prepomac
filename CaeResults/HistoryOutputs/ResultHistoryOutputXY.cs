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
    public class ResultHistoryOutputXY : ResultHistoryOutput
    {
        // Variables                                                                                                                
        public const string FieldName = "XY";
        public const string ComponentName = "VALUE";

        private HistoryResultData _xSource;
        private HistoryResultData _ySource;


        // Properties                                                                                                               
        public HistoryResultData XSource { get { return _xSource; } set { _xSource = CloneHistorySource(value); } }
        public HistoryResultData YSource { get { return _ySource; } set { _ySource = CloneHistorySource(value); } }


        // Constructors                                                                                                             
        public ResultHistoryOutputXY(string name, HistoryResultData xSource, HistoryResultData ySource)
            : base(name, "", RegionTypeEnum.None)
        {
            XSource = xSource;
            YSource = ySource;
        }


        // Methods                                                                                                                  
        public override string[] GetParentNames()
        {
            return GetHistoryParentNames(_xSource, _ySource);
        }
    }
}
