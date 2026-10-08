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
    class CReplaceDistribution : PreprocessCommand, IFileDependentCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private string _oldDistributionName;
        private Distribution _newDistribution;


        // Properties                                                                                                               
        public string FileName
        {
            get { return _newDistribution is DistributionFromFile dff ? dff.FileName : null; }
            set { if (_newDistribution is DistributionFromFile dff) dff.FileName = value; }
        }


        // Constructor                                                                                                              
        public CReplaceDistribution(string oldDistributionName, Distribution newDistribution)
            : base("Edit distribution")
        {
            _oldDistributionName = oldDistributionName;
            _newDistribution = newDistribution.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceDistribution(_oldDistributionName, _newDistribution.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonFieldEmitter.Distribution(writer, _oldDistributionName, _newDistribution);
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
            return base.GetCommandString() + _oldDistributionName + ", " + _newDistribution.ToString();
        }
    }
}
