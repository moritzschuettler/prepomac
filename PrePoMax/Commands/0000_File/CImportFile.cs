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
    class CImportFile : PreprocessCommand, IImportFileCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private string _fileName;
        private bool _onlyMaterials;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        public bool OnlyMaterials { get { return _onlyMaterials; } }


        // Constructor                                                                                                              
        public CImportFile(string fileName, bool onlyMaterials)
            :base("Import file")
        {
            _fileName = Tools.GetLocalPath(fileName);
            _onlyMaterials = onlyMaterials;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ImportFile(Tools.GetGlobalPath(_fileName), _onlyMaterials);
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            string fileName = receiver.GetFileNameToImport("Replace file: " + _fileName, _onlyMaterials);
            if (fileName != null) _fileName = Tools.GetLocalPath(fileName);
            return Execute(receiver);
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            string onlyMaterials = _onlyMaterials ? "only_materials=True" : null;
            // An older import split the periodic faces of the CAD parts it added, which an import no longer does
            bool splitPeriodicFaces = SplitsPeriodicFacesAutomatically && writer.NumOfLegacySplitFaces > 1 &&
                                      IsCadFile(_fileName);
            if (splitPeriodicFaces)
                writer.Assign("part_names_before_import", "set(pmx.model.geometry.parts.get_names())");
            writer.Statement(writer.Call("pmx", "import_file", writer.Path(Tools.GetGlobalPath(_fileName)),
                                         onlyMaterials));
            if (splitPeriodicFaces)
                writer.Statement(writer.Call("pmx.model.geometry.parts", "split_periodic_faces",
                                             "[name for name in pmx.model.geometry.parts.get_names() " +
                                             "if name not in part_names_before_import]",
                                             "number_of_faces=" + writer.Value(writer.NumOfLegacySplitFaces)));
            writer.CountEmittedCommand();
        }
        private static bool IsCadFile(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName).ToLower();
            return extension == ".stp" || extension == ".step" || extension == ".igs" || extension == ".iges" ||
                   extension == ".brep";
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
