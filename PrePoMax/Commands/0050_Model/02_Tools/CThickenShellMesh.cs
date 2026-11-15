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


namespace PrePoMax.Commands
{
    [PmxType(10082)]
    class CThickenShellMesh : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _partNames;
        [PmxKey(1)] private double _thickness;
        [PmxKey(2)] private int _numberOfLayers;
        [PmxKey(3)] private double _offset;
        [PmxKey(4)] private bool _keepModelEdges;


        // Constructor                                                                                                              
        public CThickenShellMesh(string[] partNames, double thickness, int numberOfLayers, double offset, bool keepModelEdges)
            : base("Thicken shell mesh")
        {
            _partNames = partNames;
            _thickness = thickness;
            _numberOfLayers = numberOfLayers;
            _offset = offset;
            _keepModelEdges = keepModelEdges;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ThickenShellMesh(_partNames, _thickness, _numberOfLayers, _offset, _keepModelEdges);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.mesh", "thicken_shell_mesh", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.Number(_thickness),
                                         "number_of_layers=" + PythonValueFormatter.Format(_numberOfLayers),
                                         "offset=" + PythonValueFormatter.Number(_offset),
                                         "keep_model_edges=" + PythonValueFormatter.Format(_keepModelEdges)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Thickness: " + _thickness.ToString() +
                " Number of layers: " + _numberOfLayers.ToString();
        }
    }
}
