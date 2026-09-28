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
    class CSplitGeometry : PreprocessCommand
    {
        // Variables                                                                                                                
        private GeometryFeature _geometryFeature;


        // Properties
        public SplitToolSymbol ToolSymbol { get { return _geometryFeature.ToolSymbol; } }


        // Constructor                                                                                                              
        public CSplitGeometry(GeometryFeature geometryFeature)
            : base(geometryFeature.SplitType == SplitTypeEnum.Shell ?
                   "Split shell" : "Split solid")
        {
            _geometryFeature = geometryFeature.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            // The symbol is made from the geometry this execution meets, so a regenerate replaces it and a
            // split that fails before making it leaves none
            GeometryFeature geometryFeature = _geometryFeature.DeepClone();
            geometryFeature.ToolSymbol = null;
            try
            {
                receiver.SplitGeometry(geometryFeature);
            }
            finally
            {
                _geometryFeature.ToolSymbol = geometryFeature.ToolSymbol;
            }
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Comment("The geometry splitting operations are not yet available in the Python API.");
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _geometryFeature.DisplayName;
        }
    }
}
