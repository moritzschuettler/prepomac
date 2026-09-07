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
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    class CUpdateNodalCoordinatesFromFile : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _fileName;


        // Constructor                                                                                                              
        public CUpdateNodalCoordinatesFromFile(string fileName)
            : base("Update nodal coordinates from file")
        {
            _fileName = fileName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.UpdateNodalCoordinatesFromFile(_fileName);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.mesh", "update_nodal_coordinates_from_file",
                                         writer.Path(Tools.GetGlobalPath(_fileName))));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
