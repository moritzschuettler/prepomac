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

namespace PrePoMax.Commands
{
    [Serializable]
    class CReplaceDistribution : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldDistributionName;
        private Distribution _newDistribution;


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
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldDistributionName + ", " + _newDistribution.ToString();
        }
    }
}
