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
    class CSplitAFaceUsingTwoPoints : PreprocessCommand
    {
        // Variables                                                                                                                
        private GeometrySelection _surfaceSelection;
        private GeometrySelection _verticesSelection;


        // Constructor                                                                                                              
        public CSplitAFaceUsingTwoPoints(GeometrySelection surfaceSelection, GeometrySelection verticesSelection)
            : base("Split a face using two points")
        {
            _surfaceSelection = surfaceSelection.DeepClone();
            _verticesSelection = verticesSelection.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SplitAFaceUsingTwoPoints(_surfaceSelection.DeepClone(), _verticesSelection.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Comment("Split a face using two points has no Python form. Its successor is split_face " +
                           "with pmx.create.split_tool_line_by_two_points.");
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Surface: " + _surfaceSelection.ToString() + ", Vertices: " + _verticesSelection.ToString();
        }
    }
}
