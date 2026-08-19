// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddHistoryOutputToNewPlot : PostprocessCommand
    {
        private HistorySeriesSource _source;

        public CAddHistoryOutputToNewPlot(HistorySeriesSource source)
            : base("Add history output to new plot")
        {
            _source = source.DeepCopy();
        }

        public override bool Execute(Controller receiver)
        {
            receiver.AddHistoryOutputToNewPlot(_source.DeepCopy());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _source;
        }
    }
}
