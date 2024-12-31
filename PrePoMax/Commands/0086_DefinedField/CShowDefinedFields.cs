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
    class CShowDefinedFields : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _stepName;
        private string[] _definedFieldNames;


        // Constructor                                                                                                              
        public CShowDefinedFields(string stepName, string[] definedFieldNames)
            : base("Show defined fields")
        {
            _stepName = stepName; 
            _definedFieldNames = definedFieldNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ShowDefinedFields(_stepName, _definedFieldNames);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _stepName + ": " + GetArrayAsString(_definedFieldNames);
        }
    }
}
