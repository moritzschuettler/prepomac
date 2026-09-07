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
    class CAddSurface : PreprocessCommand
    {
        // Variables                                                                                                                
        private FeSurface _surface;
        private bool _update;


        // Constructor                                                                                                              
        public CAddSurface(FeSurface surface, bool update)
            : base("Add surface")
        {
            _surface = surface.DeepClone();
            _update = update;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddSurface(_surface.DeepClone(), _update);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            if (_surface.CreatedFrom == FeSurfaceCreatedFrom.NodeSet)
            {
                writer.Statement(writer.Call("pmx.model.surfaces", "add_from_node_set",
                                             PythonValueFormatter.Text(_surface.Name),
                                             PythonValueFormatter.Text(_surface.CreatedFromNodeSetName)));
                PythonModelItemEmitter.EmitState(writer, "pmx.model.surfaces", _surface);
                writer.CountEmittedCommand();
            }
            else if (_surface.CreatedFrom == FeSurfaceCreatedFrom.Selection)
            {
                PythonModelItemEmitter.AddFromSelection(writer, "pmx.model.surfaces", _surface,
                                                        _surface.CreationData);
            }
            else writer.Unsupported(this, "a surface created from " + _surface.CreatedFrom + " has no API form");
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _surface.ToString();
        }
    }
}
