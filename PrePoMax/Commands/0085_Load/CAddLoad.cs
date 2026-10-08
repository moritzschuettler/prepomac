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
    class CAddLoad : PreprocessCommand, IFileDependentCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private Load _load;


        // Properties                                                                                                               
        public string FileName
        {
            get
            {
                if (_load is ImportedPressure ip) return ip.FileName;
                else if (_load is ImportedSTLoad ist) return ist.FileName;
                else return null;
            }
            set
            {
                if (_load is ImportedPressure ip) ip.FileName = value;
                else if (_load is ImportedSTLoad ist) ist.FileName = value;
            }
        }


        // Constructor                                                                                                              
        public CAddLoad(string stepName, Load load)
            : base("Add load")
        {
            _stepName = stepName;
            _load = load.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddLoad(_stepName, _load.DeepClone());
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonLoadEmitter.Add(writer, _stepName, _load);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _load.ToString();
        }
    }
}
