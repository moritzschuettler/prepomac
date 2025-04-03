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
    class CExportCADGeometryPartsAsBrep : PreprocessCommand, IExportFileCommand
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private string _fileName;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        

        // Constructor                                                                                                              
        public CExportCADGeometryPartsAsBrep(string[] partNames, string fileName)
            :base("Export CAD geometry parts to .brep files")
        {
            _partNames = partNames;
            _fileName = Tools.GetLocalPath(fileName);
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ExportCADGeometryPartsAsBrep(_partNames, Tools.GetGlobalPath(_fileName));
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName + ": " + _partNames.ToShortString();
        }
    }
}
