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
    class CReplaceSection : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldSectionName;
        private Section _newSection;


        // Constructor                                                                                                              
        public CReplaceSection(string oldSectionName, Section newSection)
            : base("Edit section")
        {
            _oldSectionName = oldSectionName;
            _newSection = newSection.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceSection(_oldSectionName, _newSection.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonSectionEmitter.Replace(writer, _oldSectionName, _newSection);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldSectionName + ", " + _newSection.ToString();
        }
    }
}
