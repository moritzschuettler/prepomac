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
    class CExportPartsAsGmshMesh : PreprocessCommand, IExportFileCommand
    {
        // Variables                                                                                                                
        private string _fileName;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        

        // Constructor                                                                                                              
        public CExportPartsAsGmshMesh(string fileName)
            :base("Export geometry parts to Gmsh .msh file")
        {
            _fileName = Tools.GetLocalPath(fileName);
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ExportPartsAsGmshMesh(Tools.GetGlobalPath(_fileName));
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            string fileName = Tools.GetGlobalPath(_fileName);
            MeshExportFormatEnum inferredFormat;
            string format = null;
            if (!ExportFormatTools.TryGetMeshFormat(fileName, out inferredFormat) || inferredFormat != MeshExportFormatEnum.Gmsh)
                format = "format=" + PythonValueFormatter.Format(MeshExportFormatEnum.Gmsh.ToString());
            writer.Statement(writer.Call("pmx", "export_mesh", writer.Call("pmx.model.parts", "get_names"), writer.OutputPath(fileName), format));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
