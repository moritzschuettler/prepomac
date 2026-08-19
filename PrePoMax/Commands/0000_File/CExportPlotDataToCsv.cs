// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using CaeGlobals;
using System;

namespace PrePoMax.Commands
{
    [Serializable]
    class CExportPlotDataToCsv : PostprocessCommand, IExportFileCommand
    {
        private int _plotIndex;
        private string _fileName;
        private string _delimiter;

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        public CExportPlotDataToCsv(int plotIndex, string fileName, string delimiter)
            : base("Export plot data")
        {
            _plotIndex = plotIndex;
            _fileName = Tools.GetLocalPath(fileName);
            _delimiter = delimiter;
        }

        public override bool Execute(Controller receiver)
        {
            receiver.ExportPlotData(Tools.GetGlobalPath(_fileName), _plotIndex, _delimiter);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _plotIndex + ", " + _fileName;
        }
    }
}
