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
using System.Runtime.Serialization;


namespace PrePoMax.Commands
{
    [PmxType(10268)]
    class CSetMeshingParameters : PreprocessCommand, ICommandWithDialog
    {
        // Variables                                                                                                                


        // Constructors                                                                                                             
        public CSetMeshingParameters(string[] partNames, MeshingParameters meshingParameters)
            : base("Set meshing parameters")
        {
            //
            // this class is needed for compatibility for version v1.3.5
            //
        }
        public CSetMeshingParameters(SerializationInfo info, StreamingContext context)
            : base("")
        {
            // Compatibility for version v1.3.5
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            // Compatibility for version v1.3.5
            return true;
        }
        public bool ExecuteWithDialog(Controller receiver)
        {
            // Compatibility for version v1.3.5
            return Execute(receiver);
        }
        // The command carries no data and its Execute does nothing, so there is nothing to replay
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Comment("Set meshing parameters (v1.3.5) - the command carries no data");
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            // Compatibility for version v1.3.5
            return base.GetCommandString();
        }
    }
}
