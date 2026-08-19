// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CCombineHistoryOutputs : PostprocessCommand
    {
        private string _outputName;
        private HistorySeriesSource _xSource;
        private HistorySeriesSource _ySource;

        public CCombineHistoryOutputs(string outputName, HistorySeriesSource xSource, HistorySeriesSource ySource)
            : base("Combine history outputs")
        {
            _outputName = outputName;
            _xSource = xSource.DeepCopy();
            _ySource = ySource.DeepCopy();
        }

        public override bool Execute(Controller receiver)
        {
            receiver.CombineHistoryOutputs(_outputName, _xSource.DeepCopy(), _ySource.DeepCopy());
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _outputName + ", X: " + _xSource + ", Y: " + _ySource;
        }
    }
}
