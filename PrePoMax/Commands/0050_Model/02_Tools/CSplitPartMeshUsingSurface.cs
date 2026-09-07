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
    class CSplitPartMeshUsingSurface : PreprocessCommand
    {
        // Variables                                                                                                                
        private SplitPartMeshData _splitPartMeshData;


        // Constructor                                                                                                              
        public CSplitPartMeshUsingSurface(SplitPartMeshData splitPartMeshData)
            : base("Split part mesh using surface")
        {
            _splitPartMeshData = splitPartMeshData;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            return receiver.SplitPartMeshUsingSurface(_splitPartMeshData);
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            string basePart = PythonRegionEmitter.Emit(writer, _splitPartMeshData.BasePartRegionName,
                                                       _splitPartMeshData.BasePartRegionType,
                                                       _splitPartMeshData.BasePartCreationData);
            string splitter = PythonRegionEmitter.Emit(writer, _splitPartMeshData.SplitterSurfaceRegionName,
                                                       _splitPartMeshData.SplitterSurfaceRegionType,
                                                       _splitPartMeshData.SplitterSurfaceCreationData);
            writer.Statement(writer.Call("pmx.model.mesh", "split_part_mesh_using_surface", basePart, splitter,
                                         "offset=" + PythonValueFormatter.Number(_splitPartMeshData.Offset),
                                         "max_size=" + PythonValueFormatter.Number(_splitPartMeshData.MaxH),
                                         "min_size=" + PythonValueFormatter.Number(_splitPartMeshData.MinH),
                                         "hausdorff=" +
                                         PythonValueFormatter.Number(_splitPartMeshData.Hausdorff),
                                         "exact=" + PythonValueFormatter.Format(_splitPartMeshData.Exact)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "By selection";
        }
    }
}

