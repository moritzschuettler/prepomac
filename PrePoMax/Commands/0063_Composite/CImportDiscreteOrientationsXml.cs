// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using CaeModel;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [PmxType(50050)]
    class CImportDiscreteOrientationsXml : PreprocessCommand, IImportFileCommand
    {
        // Variables
        [PmxKey(0)] private string _layupName;
        [PmxKey(1)] private string _fileName;


        // Properties
        public string FileName { get { return _fileName; } set { _fileName = value; } }


        // Constructor
        public CImportDiscreteOrientationsXml(string layupName, string fileName)
            : base("Import discrete orientations XML")
        {
            _layupName = layupName;
            _fileName = Tools.GetLocalPath(fileName);
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ImportDiscreteOrientationsXml(_layupName, Tools.GetGlobalPath(_fileName));
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _layupName + " | " + _fileName;
        }
    }
}
