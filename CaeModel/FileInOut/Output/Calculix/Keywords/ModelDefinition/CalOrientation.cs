// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Text;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalOrientation : CalculixKeyword
    {
        // Variables
        private string _name;
        private double[] _axis1;
        private double[] _axis2;


        // Properties


        // Constructor
        public CalOrientation(string name, double[] axis1, double[] axis2)
        {
            _name = name;
            _axis1 = axis1;
            _axis2 = axis2;
        }


        // Methods
        public override string GetKeywordString()
        {
            return string.Format("*Orientation, Name={0}, System=Rectangular{1}", _name, Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_axis1[0].ToCalculiX16String()).Append(", ");
            sb.Append(_axis1[1].ToCalculiX16String()).Append(", ");
            sb.Append(_axis1[2].ToCalculiX16String()).Append(", ");
            sb.Append(_axis2[0].ToCalculiX16String()).Append(", ");
            sb.Append(_axis2[1].ToCalculiX16String()).Append(", ");
            sb.Append(_axis2[2].ToCalculiX16String());
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
