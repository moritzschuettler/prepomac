// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CRemovePlot : PostprocessCommand
    {
        private int _plotIndex;

        public CRemovePlot(int plotIndex)
            : base("Remove plot")
        {
            _plotIndex = plotIndex;
        }

        public override bool Execute(Controller receiver)
        {
            receiver.RemovePlot(_plotIndex);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _plotIndex;
        }
    }
}
