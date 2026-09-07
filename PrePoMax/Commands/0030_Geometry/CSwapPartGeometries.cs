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
    class CSwapPartGeometries : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _partName1;
        private string _partName2;


        // Constructor                                                                                                              
        public CSwapPartGeometries(string partName1, string partName2)
            : base("Swap part geometries")
        {
            _partName1 = partName1;
            _partName2 = partName2;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SwapGeometryPartGeometries(_partName1, _partName2);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.geometry.parts", "swap_geometries", PythonValueFormatter.Text(_partName1),
                                         PythonValueFormatter.Text(_partName2)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _partName1 + ", " + _partName2;
        }
    }
}
