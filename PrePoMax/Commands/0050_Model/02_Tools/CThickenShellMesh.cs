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


namespace PrePoMax.Commands
{
    [Serializable]
    class CThickenShellMesh : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private double _thickness;
        private int _numberOfLayers;
        private double _offset;
        private bool _keepModelEdges;


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

        public override string GetCommandString()
        {
            return base.GetCommandString() + "Thickness: " + _thickness.ToString() +
                " Number of layers: " + _numberOfLayers.ToString();
        }
    }
}
