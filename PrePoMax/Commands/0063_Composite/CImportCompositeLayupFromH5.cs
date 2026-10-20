// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Globalization;
using PrePoMax;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CImportCompositeLayupFromH5 : PreprocessCommand, IImportFileCommand
    {
        // Variables
        private string _fileName;
        private string _layupName;
        private string _regionName;
        private double _drillDepth;
        private double _tolerance;


        // Properties
        public string FileName { get { return _fileName; } set { _fileName = value; } }


        // Constructor
        public CImportCompositeLayupFromH5(string fileName, string layupName, string regionName, double drillDepth,
                                           double tolerance)
            : base("Import composite layup from HDF5")
        {
            _fileName = Tools.GetLocalPath(fileName);
            _layupName = layupName;
            _regionName = regionName;
            _drillDepth = drillDepth;
            _tolerance = tolerance;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ImportCompositeLayupFromH5(Tools.GetGlobalPath(_fileName), _layupName, _regionName,
                                                _drillDepth, _tolerance);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName + "; " + _layupName + "; " +
                   (string.IsNullOrEmpty(_regionName) ? "All shell elements" : _regionName) + "; " +
                   _drillDepth.ToString(CultureInfo.InvariantCulture) + "; " +
                   _tolerance.ToString(CultureInfo.InvariantCulture);
        }
    }
}
