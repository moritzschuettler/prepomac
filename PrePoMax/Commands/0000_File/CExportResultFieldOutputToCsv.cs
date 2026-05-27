// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeGlobals;
using CaeResults;

namespace PrePoMax.Commands
{
    [Serializable]
    class CExportResultFieldOutputToCsv : PostprocessCommand, IExportFileCommand
    {
        // Variables                                                                                                                
        private string _fileName;
        private string _resultName;
        private FieldData _fieldData;


        // Properties                                                                                                               
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }


        // Constructor                                                                                                              
        public CExportResultFieldOutputToCsv(string fileName, string resultName, FieldData fieldData)
            :base("Export result field output")
        {
            _fileName = fileName;
            _resultName = resultName;
            _fieldData = fieldData.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ExportResultFieldOutput(_fileName, _resultName, _fieldData);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
