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
    class CExtractSurface : PreprocessCommand
    {
        // Variables
        private SurfaceExtraction _surfaceExtraction;


        // Constructor
        public CExtractSurface(SurfaceExtraction surfaceExtraction)
            : base("Extract surface")
        {
            _surfaceExtraction = surfaceExtraction.DeepClone();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ExtractSurface(_surfaceExtraction.DeepClone());
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            string selection = PythonSelectionEmitter.Emit(writer, _surfaceExtraction.CreationData);
            writer.Statement(writer.Call("pmx.model.geometry.parts", "extract_surface", selection,
                                         PythonValueFormatter.Number(_surfaceExtraction.Offset)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            int numOfSurfaces = _surfaceExtraction.CreationIds == null ? 0 : _surfaceExtraction.CreationIds.Length;
            return base.GetCommandString() + ": " + numOfSurfaces + " surfaces";
        }
    }
}
