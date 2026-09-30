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
    class COffsetFace : PreprocessCommand
    {
        // Variables
        private FaceOffset _faceOffset;


        // Constructor
        public COffsetFace(FaceOffset faceOffset)
            : base("Offset face")
        {
            _faceOffset = faceOffset.DeepClone();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.OffsetFace(_faceOffset.DeepClone());
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            string selection = PythonSelectionEmitter.Emit(writer, _faceOffset.CreationData);
            writer.Statement(writer.Call("pmx.model.geometry.parts", "offset_face", selection,
                                         PythonValueFormatter.Number(_faceOffset.Offset)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            int numOfSurfaces = _faceOffset.CreationIds == null ? 0 : _faceOffset.CreationIds.Length;
            return base.GetCommandString() + ": " + numOfSurfaces + " surfaces";
        }
    }
}
