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
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CExportToAbaqus : PreprocessCommand, IExportFileCommand
    {
        // Variables                                                                                                                
        private string _fileName;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        

        // Constructor                                                                                                              
        public CExportToAbaqus(string fileName)
            :base("Export model to Abaqus")
        {
            _fileName = Tools.GetLocalPath(fileName);
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ExportToAbaqus(Tools.GetGlobalPath(_fileName));
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
