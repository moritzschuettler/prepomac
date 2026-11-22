// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    [PmxType(10548)]
    class CChangeElementOrder : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _partNames;
        [PmxKey(1)] private bool _secondOrder;


        // Constructor                                                                                                              
        public CChangeElementOrder(string[] partNames, bool secondOrder)
            : base("Change element order")
        {
            _partNames = partNames;
            _secondOrder = secondOrder;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ChangeElementOrder(_partNames, _secondOrder);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.mesh", "change_element_order", PythonValueFormatter.List(_partNames),
                                         "second_order=" + PythonValueFormatter.Format(_secondOrder)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Parts: " + string.Join(", ", _partNames) +
                " Element order: " + (_secondOrder ? "Parabolic" : "Linear");
        }
    }
}
