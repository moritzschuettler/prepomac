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
    [PmxType(10136)]
    class CAddSection : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private Section _section;


        // Constructor                                                                                                              
        public CAddSection(Section section)
            : base("Add section")
        {
            _section = section.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddSection(_section.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonSectionEmitter.Add(writer, _section);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _section.ToString();
        }
    }
}
