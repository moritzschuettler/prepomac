// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Text;
using CaeGlobals;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalCyclicSymmetry : CalculixKeyword
    {
        private CyclicSymmetry _cyclicSymmetry;
        private string _elementSetName;

        public CalCyclicSymmetry(CyclicSymmetry cyclicSymmetry, string elementSetName)
        {
            _cyclicSymmetry = cyclicSymmetry;
            _elementSetName = elementSetName;
        }

        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Cyclic symmetry model, N={0}, Tie={1}",
                            _cyclicSymmetry.NumberOfSectors, _cyclicSymmetry.Name);
            if (_cyclicSymmetry.NumberOfGraphSectors != 1)
                sb.AppendFormat(", Ngraph={0}", _cyclicSymmetry.NumberOfGraphSectors);
            if (_elementSetName != null) sb.AppendFormat(", Elset={0}", _elementSetName);
            if (!_cyclicSymmetry.CheckSectorAngle) sb.Append(", Check=No");
            sb.AppendLine();
            return sb.ToString();
        }

        public override string GetDataString()
        {
            double[] pointA = _cyclicSymmetry.GetAxisPoint();
            double[] pointB = _cyclicSymmetry.GetSecondAxisPoint();
            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}{6}",
                                 pointA[0].ToCalculiX16String(), pointA[1].ToCalculiX16String(),
                                 pointA[2].ToCalculiX16String(), pointB[0].ToCalculiX16String(),
                                 pointB[1].ToCalculiX16String(), pointB[2].ToCalculiX16String(),
                                 Environment.NewLine);
        }
    }
}
