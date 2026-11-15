// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    [PmxType(10538)]
    public enum NumberFormatType
    {
        Scientific,
        General
    }

    [Serializable]
    [PmxType(10539)]
    public class NumberFormatSettings : ISettings
    {
        // Variables
        [PmxKey(0)] private NumberFormatType _numberFormat;
        [PmxKey(1)] private int _numberOfSignificantDigits;


        // Properties
        public NumberFormatType NumberFormat { get { return _numberFormat; } set { _numberFormat = value; } }
        public int NumberOfSignificantDigits
        {
            get { return _numberOfSignificantDigits; }
            set
            {
                _numberOfSignificantDigits = value;
                if (_numberOfSignificantDigits < 2) _numberOfSignificantDigits = 2;
                if (_numberOfSignificantDigits > 8) _numberOfSignificantDigits = 8;
            }
        }


        // Constructors
        public NumberFormatSettings()
        {
            Reset();
        }


        // Methods
        public void CheckValues()
        {
        }
        public void Reset()
        {
            _numberFormat = NumberFormatType.General;
            _numberOfSignificantDigits = 4;
        }
        public string GetNumberFormat()
        {
            if (_numberFormat == NumberFormatType.General)
                return "G" + _numberOfSignificantDigits.ToString();
            else
                return "E" + (_numberOfSignificantDigits - 1).ToString();
        }
    }
}
