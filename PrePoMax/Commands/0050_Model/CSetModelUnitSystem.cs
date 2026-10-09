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
    // Compatibility v1.1.1 - the command was renamed to CSetNewModelProperties
    [Serializable]
    class CSetModelUnitSystem : PreprocessCommand
    {
        // Variables                                                                                                                
        private UnitSystemType _unitSystemType;


        // Constructor                                                                                                              
        public CSetModelUnitSystem(UnitSystemType unitSystemType)
            : base("Set unit system")
        {
            _unitSystemType = unitSystemType;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetNewModelProperties(ModelSpaceEnum.ThreeD, _unitSystemType);
            return true;
        }

        // The command sets the three dimensional model space as well, so the journal does too
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Assign("pmx.model.properties.model_space",
                          PythonValueFormatter.Text(ModelSpaceEnum.ThreeD.ToString()));
            writer.Assign("pmx.model.properties.unit_system",
                          PythonValueFormatter.Text(_unitSystemType.ToString()));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _unitSystemType.GetDescription();
        }
    }
}
