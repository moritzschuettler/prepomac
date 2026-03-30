// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeModel;
using CaeResults;
using PrePoMax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddResultHistoryOutput : PostprocessCommand
    {
        // Variables                                                                                                                
        private ResultHistoryOutput _resultHistoryOutput;
        
        
        // Properties                                                                                                               
        public ResultHistoryOutput ResultHistoryOutput
        {
            get { return _resultHistoryOutput; }
            set { _resultHistoryOutput = value; }
        }


        // Constructor                                                                                                              
        public CAddResultHistoryOutput(ResultHistoryOutput resultHistoryOutput)
            :base("Add result history output")
        {
            _resultHistoryOutput = resultHistoryOutput.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddResultHistoryOutput(_resultHistoryOutput.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _resultHistoryOutput.ToString();
        }
    }
}
