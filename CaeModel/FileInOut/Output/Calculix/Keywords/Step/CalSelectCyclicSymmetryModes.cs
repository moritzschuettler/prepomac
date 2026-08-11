// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Text;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalSelectCyclicSymmetryModes : CalculixKeyword
    {
        private FrequencyStep _step;

        public CalSelectCyclicSymmetryModes(FrequencyStep step)
        {
            _step = step;
        }

        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder("*Select cyclic symmetry modes");
            if (_step.CyclicSymmetryModeMin != int.MinValue)
                sb.AppendFormat(", Nmin={0}", _step.CyclicSymmetryModeMin);
            if (_step.CyclicSymmetryModeMax != int.MinValue)
                sb.AppendFormat(", Nmax={0}", _step.CyclicSymmetryModeMax);
            sb.AppendLine();
            return sb.ToString();
        }

        public override string GetDataString()
        {
            return "";
        }
    }
}
