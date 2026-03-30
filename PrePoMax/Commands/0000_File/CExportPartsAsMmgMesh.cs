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
    class CExportPartsAsMmgMesh : PreprocessCommand, IExportFileCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private string _fileName;
        private bool _combine;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        

        // Constructor                                                                                                              
        public CExportPartsAsMmgMesh(string[] partNames, string fileName, bool combine = false)
            :base("Export parts as mmg .mesh file")
        {
            _partNames = partNames;
            _fileName = Tools.GetLocalPath(fileName);
            _combine = combine;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ExportPartsAsMmgMesh(_partNames, Tools.GetGlobalPath(_fileName).ToLower(), _combine);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName + ": " + _partNames.ToShortString();
        }
    }
}
