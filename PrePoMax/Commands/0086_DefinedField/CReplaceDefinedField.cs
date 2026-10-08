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
using CaeGlobals;
using PrePoMax.Python.Journal;

namespace PrePoMax.Commands
{
    [Serializable]
    class CReplaceDefinedField : PreprocessCommand, IFileDependentCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private string _stepName;
        private string _oldDefinedFieldName;
        private DefinedField _newDefinedField;


        // Properties                                                                                                               
        public string FileName
        {
            get
            {
                if (_newDefinedField is DefinedTemperature dt && dt.Type == DefinedTemperatureTypeEnum.FromFile) return dt.FileName;
                else return null;
            }
            set
            {
                if (_newDefinedField is DefinedTemperature dt && dt.Type == DefinedTemperatureTypeEnum.FromFile) dt.FileName = value;
            }
        }


        // Constructor                                                                                                              
        public CReplaceDefinedField(string stepName, string oldDefinedFieldName, DefinedField newDefinedField)
            : base("Edit defined field")
        {
            _stepName = stepName;
            _oldDefinedFieldName = oldDefinedFieldName;
            _newDefinedField = newDefinedField.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceDefinedField(_stepName, _oldDefinedFieldName, _newDefinedField.DeepClone());
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            if (FileName != null)
            {
                string fileName = receiver.GetFileNameToImport("Replace file: " + FileName, "Calculix result files|*.frd");
                if (fileName != null) FileName = fileName;
            }
            return Execute(receiver);
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonOutputEmitter.DefinedField(writer, _stepName, _oldDefinedFieldName, _newDefinedField);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + _oldDefinedFieldName + ", " + _newDefinedField.ToString();
        }
    }
}
