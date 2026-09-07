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
    class CRemeshElements : PreprocessCommand
    {
        // Variables                                                                                                                
        private RemeshingParameters _remeshingParameters;


        // Constructor                                                                                                              
        public CRemeshElements(RemeshingParameters remeshingParameters)
            : base("Remesh elements")
        {
            _remeshingParameters = remeshingParameters.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            return receiver.RemeshElements(_remeshingParameters);
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            string region = PythonRegionEmitter.Emit(writer, _remeshingParameters.RegionName,
                                                     _remeshingParameters.RegionType,
                                                     _remeshingParameters.CreationData);
            writer.Statement(writer.Call("pmx.model.mesh", "remesh_elements", region,
                                         "max_size=" + PythonValueFormatter.Number(_remeshingParameters.MaxH),
                                         "min_size=" + PythonValueFormatter.Number(_remeshingParameters.MinH),
                                         "hausdorff=" +
                                         PythonValueFormatter.Number(_remeshingParameters.Hausdorff)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _remeshingParameters;
        }
    }
}
