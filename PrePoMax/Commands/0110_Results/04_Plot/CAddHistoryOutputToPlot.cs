// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddHistoryOutputToPlot : PostprocessCommand
    {
        private int _plotIndex;
        private HistorySeriesSource _source;

        public CAddHistoryOutputToPlot(int plotIndex, HistorySeriesSource source)
            : base("Add history output to plot")
        {
            _plotIndex = plotIndex;
            _source = source.DeepCopy();
        }

        public override bool Execute(Controller receiver)
        {
            receiver.AddHistoryOutputToPlot(_plotIndex, _source.DeepCopy());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _plotIndex + ", " + _source;
        }
    }
}
