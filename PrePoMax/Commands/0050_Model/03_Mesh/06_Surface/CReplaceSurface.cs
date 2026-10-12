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
    class CReplaceSurface : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _oldSurfaceName;
        private FeSurface _newSurface;

        // Constructor                                                                                                              
        public CReplaceSurface(string oldSurfaceName, FeSurface newSurface)
            : base("Edit surface")
        {
            _oldSurfaceName = oldSurfaceName;
            _newSurface = newSurface.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceSurface(_oldSurfaceName, _newSurface.DeepClone(), true);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            // A replace keeps the type it is not given, so the Element type is written out too
            string surfaceType = "surface_type=" + PythonValueFormatter.Text(_newSurface.Type.ToString());
            if (_newSurface.CreatedFrom == FeSurfaceCreatedFrom.NodeSet)
            {
                // A replace keeps the node set it is not given, so one a rename or a remove took away is left out
                string nodeSetName = writer.KeptReference("pmx.model.node_sets", _newSurface.CreatedFromNodeSetName,
                                                          true);
                writer.Statement(writer.Call("pmx.model.surfaces", "replace_from_node_set",
                                             PythonValueFormatter.Text(_oldSurfaceName),
                                             PythonValueFormatter.Text(_newSurface.Name),
                                             nodeSetName == null ? null : PythonValueFormatter.Text(nodeSetName),
                                             surfaceType));
                PythonModelItemEmitter.EmitState(writer, "pmx.model.surfaces", _newSurface);
                writer.CountEmittedCommand();
            }
            else if (_newSurface.CreatedFrom == FeSurfaceCreatedFrom.Selection)
            {
                string selectionVariable = PythonSelectionEmitter.Emit(writer, _newSurface.CreationData);
                writer.Statement(writer.Call("pmx.model.surfaces", "replace",
                                             PythonValueFormatter.Text(_oldSurfaceName),
                                             PythonValueFormatter.Text(_newSurface.Name), selectionVariable,
                                             surfaceType));
                PythonModelItemEmitter.EmitState(writer, "pmx.model.surfaces", _newSurface);
                writer.CountEmittedCommand();
            }
            else writer.Unsupported(this, "a surface created from " + _newSurface.CreatedFrom +
                                          " has no API form");
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldSurfaceName + ", " + _newSurface.ToString();
        }
    }
}
