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
    class CExportMeshParts : PreprocessCommand, IExportFileCommand
    {
        // Variables
        private string[] _partNames;
        private string _fileName;
        private MeshExportFormatEnum _format;


        // Properties
        public string FileName { get { return _fileName; } set { _fileName = value; } }


        // Constructor
        public CExportMeshParts(string[] partNames, string fileName, MeshExportFormatEnum format)
            :base("Export mesh parts to " + ExportFormatTools.GetExtension(format) + " file")
        {
            _partNames = partNames;
            _fileName = Tools.GetLocalPath(fileName);
            _format = format;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ExportMeshParts(_partNames, Tools.GetGlobalPath(_fileName), _format);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            // The extension names the format, so the argument is written only when it would be inferred differently
            string fileName = Tools.GetGlobalPath(_fileName);
            MeshExportFormatEnum inferredFormat;
            string format = null;
            if (!ExportFormatTools.TryGetMeshFormat(fileName, out inferredFormat) || inferredFormat != _format)
                format = "format=" + PythonValueFormatter.Format(_format.ToString());
            writer.Statement(writer.Call("pmx", "export_mesh", PythonValueFormatter.List(_partNames), writer.Path(fileName),
                                         format));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName + ": " + _partNames.ToShortString();
        }
    }
}
