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
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [Serializable]
    class CReplaceLoad : PreprocessCommand, IFileDependentCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string _oldLoadName;
        private Load _newLoad;


        // Properties                                                                                                               
        public string FileName
        {
            get
            {
                if (_newLoad is ImportedPressure ip) return ip.FileName;
                else if (_newLoad is ImportedSTLoad ist) return ist.FileName;
                else return null;
            }
            set
            {
                if (_newLoad is ImportedPressure ip) ip.FileName = value;
                else if (_newLoad is ImportedSTLoad ist) ist.FileName = value;
            }
        }


        // Constructor                                                                                                              
        public CReplaceLoad(string stepName, string oldLoadName, Load newLoad)
            : base("Edit load")
        {
            _stepName = stepName;
            _oldLoadName = oldLoadName;
            _newLoad = newLoad.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceLoad(_stepName, _oldLoadName, _newLoad.DeepClone());
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonLoadEmitter.Replace(writer, _stepName, _oldLoadName, _newLoad);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _oldLoadName + ", " + _newLoad.ToString();
        }
    }
}
