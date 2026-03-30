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
using PrePoMax.Forms;


namespace PrePoMax.Commands
{
    [Serializable]
    class CMergeCoincidentNodes : PreprocessCommand
    {
        // Variables                                                                                                                
        private MergeCoincidentNodes _mergeCoincidentNodes;


        // Constructor                                                                                                              
        public CMergeCoincidentNodes(MergeCoincidentNodes mergeCoincidentNodes)
            : base("Merge coincident nodes")
        {
            _mergeCoincidentNodes = mergeCoincidentNodes.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.MergeCoincidentNodes(_mergeCoincidentNodes.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            string nodeIds = "all nodes";
            if (_mergeCoincidentNodes.GeometryIds != null) nodeIds = _mergeCoincidentNodes.GeometryIds.ToShortString(10);
            //
            return base.GetCommandString() + nodeIds;
        }
    }
}
