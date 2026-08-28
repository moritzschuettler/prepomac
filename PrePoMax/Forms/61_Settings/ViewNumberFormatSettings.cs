// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace PrePoMax.Settings
{
    [Serializable]
    public class ViewNumberFormatSettings : IViewSettings, IReset
    {
        // Variables
        private NumberFormatSettings _numberFormatSettings;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Number formatting
        [CategoryAttribute("Number formatting")]
        [OrderedDisplayName(0, 10, "Number format")]
        [DescriptionAttribute("Select the number format for displayed output values.")]
        public NumberFormatType NumberFormat
        {
            get { return _numberFormatSettings.NumberFormat; }
            set { _numberFormatSettings.NumberFormat = value; }
        }
        //
        [CategoryAttribute("Number formatting")]
        [OrderedDisplayName(1, 10, "Number of significant digits")]
        [DescriptionAttribute("Set the number of significant digits for displayed output values (2 ... 8).")]
        public int NumberOfSignificantDigits
        {
            get { return _numberFormatSettings.NumberOfSignificantDigits; }
            set { _numberFormatSettings.NumberOfSignificantDigits = value; }
        }


        // Constructors
        public ViewNumberFormatSettings(NumberFormatSettings numberFormatSettings)
        {
            _numberFormatSettings = numberFormatSettings;
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods
        public ISettings GetBase()
        {
            return _numberFormatSettings;
        }
        public void Reset()
        {
            _numberFormatSettings.Reset();
        }
    }
}
