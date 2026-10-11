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
    class CSetNewModelProperties : PreprocessCommand
    {
        // Variables                                                                                                                
        private ModelSpaceEnum _modelSpace;
        private UnitSystemType _unitSystemType;


        // Properties                                                                                                               
        public ModelSpaceEnum ModelSpace { get { return _modelSpace; } }
        public UnitSystemType UnitSystemType { get { return _unitSystemType; } }


        // Constructor                                                                                                              
        public CSetNewModelProperties(ModelSpaceEnum modelSpace, UnitSystemType unitSystemType)
            : base("Set new model properties")
        {
            _modelSpace = modelSpace;
            _unitSystemType = unitSystemType;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetNewModelProperties(_modelSpace, _unitSystemType);
            return true;
        }

        // The dialog sets both at once while the API carries one property each, so this is the single
        // place where one recorded command becomes two. The API changes a defined unit system of a model that holds
        // something only while the model space is undefined, so the unit system is set first. The API refuses
        // Undefined, which a new model already has, so an undefined value is left out
        public override void ToPython(PythonJournalWriter writer)
        {
            if (_unitSystemType != UnitSystemType.Undefined)
                writer.Assign("pmx.model.properties.unit_system",
                              PythonValueFormatter.Text(_unitSystemType.ToString()));
            if (_modelSpace != ModelSpaceEnum.Undefined)
                writer.Assign("pmx.model.properties.model_space", PythonValueFormatter.Text(_modelSpace.ToString()));
            if (_unitSystemType == UnitSystemType.Undefined && _modelSpace == ModelSpaceEnum.Undefined)
                writer.Comment("The model space and the unit system stay undefined");
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Model space: " + _modelSpace.GetDisplayedName() 
                                           + ", Unit system: " + _unitSystemType.GetDescription();
        }
    }
}
