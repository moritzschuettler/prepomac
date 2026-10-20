// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Text;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalCompositeSolidSection : CalculixKeyword
    {
        // Variables
        private string _elementSetName;
        private string _materialName;
        private string _orientationName;


        // Constructor
        public CalCompositeSolidSection(string elementSetName, string materialName, string orientationName)
        {
            _elementSetName = elementSetName;
            _materialName = materialName;
            _orientationName = orientationName;
        }


        // Methods
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Solid section, Elset={0}, Material={1}", _elementSetName, _materialName);
            if (!string.IsNullOrEmpty(_orientationName))
                sb.AppendFormat(", Orientation={0}", _orientationName);
            sb.AppendLine();
            return sb.ToString();
        }

        public override string GetDataString()
        {
            return "";
        }
    }
}
