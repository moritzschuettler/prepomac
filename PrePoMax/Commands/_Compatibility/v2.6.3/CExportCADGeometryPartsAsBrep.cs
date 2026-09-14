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
using FileInOut.Output;
using PrePoMax.Python.Journal;


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
        public override void ToPython(PythonJournalWriter writer)
        {
            string fileName = Tools.GetGlobalPath(_fileName);
            GeometryExportFormatEnum inferredFormat;
            string format = null;
            if (!ExportFormatTools.TryGetGeometryFormat(fileName, out inferredFormat) || inferredFormat != GeometryExportFormatEnum.Brep)
                format = "format=" + PythonValueFormatter.Format(GeometryExportFormatEnum.Brep.ToString().ToLower());
            writer.Statement(writer.Call("pmx", "export_geometry", PythonValueFormatter.List(_partNames), writer.Path(fileName), format));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName + ": " + _partNames.ToShortString();
        }
    }
}
