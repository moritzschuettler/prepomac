// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Python.Journal;
using PrePoMax.Forms;
using System;


namespace PrePoMax.Commands
{
    [Serializable]
    class CExportTo3mf : PreprocessCommand, IExportFileCommand
    {
        // Variables                                                                                                                
        private Export3mfFileProperties _export3mfFileProperties;


        // Properties                                                                                                               
        public string FileName
        {
            get { return _export3mfFileProperties.FileName; }
            set { _export3mfFileProperties.FileName = value; }
        }
        

        // Constructor                                                                                                              
        public CExportTo3mf(Export3mfFileProperties export3mfFileProperties)
            :base("Export model to 3mf")
        {
            _export3mfFileProperties = export3mfFileProperties.DeepClone();
            _export3mfFileProperties.FileName = Tools.GetLocalPath(_export3mfFileProperties.FileName);
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            Export3mfFileProperties export3mfFileProperties = _export3mfFileProperties.DeepClone();
            export3mfFileProperties.FileName = Tools.GetGlobalPath(_export3mfFileProperties.FileName);
            receiver.ExportTo3mf(export3mfFileProperties);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx", "export_to_3mf",
                                         writer.Path(Tools.GetGlobalPath(_export3mfFileProperties.FileName)),
                                         "edge_thickness_ratio=" +
                                         PythonValueFormatter.Number(_export3mfFileProperties.EdgeThicknessRatio)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _export3mfFileProperties.FileName;
        }
    }
}
