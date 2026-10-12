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
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
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
    class CExportResultParts : PostprocessCommand, IExportFileCommand
    {
        // Variables
        private string[] _partNames;
        private string _fileName;
        private ResultsExportFormatEnum _format;
        [OptionalField] private string _resultName;
        [OptionalField] private int _stepId;
        [OptionalField] private int _incrementId;
        [OptionalField] private float _deformationFactor;


        // Properties
        public string FileName { get { return _fileName; } set { _fileName = value; } }


        // Constructor
        public CExportResultParts(string[] partNames, string fileName, ResultsExportFormatEnum format, string resultName,
                                  int stepId, int incrementId, float deformationFactor)
            :base("Export result parts to " + ExportFormatTools.GetExtension(format) + " file")
        {
            _partNames = partNames;
            _fileName = Tools.GetLocalPath(fileName);
            _format = format;
            _resultName = resultName;
            _stepId = stepId;
            _incrementId = incrementId;
            _deformationFactor = deformationFactor;
        }


        // Methods
        // A command recorded before the result, the increment and the deformation scale factor were stored exports
        // what the view shows
        public override bool Execute(Controller receiver)
        {
            if (_resultName == null) receiver.ExportResultParts(_partNames, Tools.GetGlobalPath(_fileName), _format);
            else receiver.ExportResultParts(_partNames, Tools.GetGlobalPath(_fileName), _format, _resultName, _stepId,
                                            _incrementId, _deformationFactor);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            // The extension names the format, so the argument is written only when it would be inferred differently
            string fileName = Tools.GetGlobalPath(_fileName);
            ResultsExportFormatEnum inferredFormat;
            string format = null;
            if (!ExportFormatTools.TryGetResultsFormat(fileName, out inferredFormat) || inferredFormat != _format)
                format = "format=" + PythonValueFormatter.Format(_format.ToString());
            List<string> arguments = new List<string>();
            arguments.Add(PythonValueFormatter.List(_partNames));
            arguments.Add(writer.OutputPath(fileName));
            arguments.Add(format);
            if (_resultName != null)
            {
                arguments.Add("result_name=" + PythonValueFormatter.Path(_resultName));
                arguments.Add("step_id=" + _stepId);
                arguments.Add("increment_id=" + _incrementId);
                // The shortest text of the single precision factor reads back as the same factor
                double deformationFactor = double.Parse(_deformationFactor.ToString("R", CultureInfo.InvariantCulture),
                                                        CultureInfo.InvariantCulture);
                arguments.Add("deformation_factor=" + PythonValueFormatter.Number(deformationFactor));
            }
            writer.Statement(writer.Call("pmx", "export_results", arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            string increment = _resultName == null ? "" : " (step " + _stepId + ", increment " + _incrementId +
                                                          ", scale factor " + _deformationFactor + ")";
            return base.GetCommandString() + _fileName + ": " + _partNames.ToShortString() + increment;
        }
    }
}
