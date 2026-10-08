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
    class CAddDistribution : PreprocessCommand, IFileDependentCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private Distribution _distribution;


        // Properties                                                                                                               
        public string FileName
        {
            get { return _distribution is DistributionFromFile dff ? dff.FileName : null; }
            set { if (_distribution is DistributionFromFile dff) dff.FileName = value; }
        }


        // Constructor                                                                                                              
        public CAddDistribution(Distribution distribution)
            :base("Add distribution")
        {
            _distribution = distribution.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddDistribution(_distribution.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonFieldEmitter.Distribution(writer, null, _distribution);
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            if (FileName != null)
            {
                string fileName = receiver.GetFileNameToImport("Replace file: " + FileName, "Text files|*.txt|All files|*.*");
                if (fileName != null) FileName = fileName;
            }
            return Execute(receiver);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _distribution.ToString();
        }
    }
}
