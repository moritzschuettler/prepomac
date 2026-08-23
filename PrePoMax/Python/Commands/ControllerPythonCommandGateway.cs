// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeResults;

namespace PrePoMax.Python
{
    internal class ControllerPythonCommandGateway : IPythonCommandGateway
    {
        // Variables
        private readonly Controller _controller;


        // Constructors
        public ControllerPythonCommandGateway(Controller controller)
        {
            _controller = controller;
        }


        // Methods
        public void SetCurrentResults(string resultName)
        {
            _controller.SetCurrentResultsCommand(resultName);
        }
        public void AddResultFieldOutput(ResultFieldOutput resultFieldOutput)
        {
            _controller.AddResultFieldOutputCommand(resultFieldOutput);
        }
        public void ReplaceResultFieldOutput(string oldResultFieldOutputName, ResultFieldOutput resultFieldOutput)
        {
            _controller.ReplaceResultFieldOutputCommand(oldResultFieldOutputName, resultFieldOutput);
        }
        public void ExportResultFieldOutput(string fileName, string resultName, FieldData fieldData)
        {
            _controller.ExportResultFieldOutputCommand(fileName, resultName, fieldData);
        }
        public void RemoveResultFieldOutputs(string[] resultFieldOutputNames)
        {
            _controller.RemoveResultFieldOutputsCommand(resultFieldOutputNames);
        }
        public void RemoveResultFieldOutputComponents(string fieldOutputName, string[] componentNames)
        {
            _controller.RemoveResultFieldOutputComponentsCommand(fieldOutputName, componentNames);
        }
        public void AddResultHistoryOutput(ResultHistoryOutput resultHistoryOutput)
        {
            _controller.AddResultHistoryOutputCommand(resultHistoryOutput);
        }
        public void ReplaceResultHistoryOutput(string oldResultHistoryOutputName, ResultHistoryOutput resultHistoryOutput)
        {
            _controller.ReplaceResultHistoryOutputCommand(oldResultHistoryOutputName, resultHistoryOutput);
        }
        public void DuplicateResultHistoryOutputs(string[] resultHistoryOutputNames)
        {
            _controller.DuplicateResultHistoryOutputsCommand(resultHistoryOutputNames);
        }
        public void ExportResultHistoryOutput(string fileName, string[] resultHistoryOutputNames, string delimiter)
        {
            ExportHistoryResultSetFileProperties properties = new ExportHistoryResultSetFileProperties();
            properties.FileName = fileName;
            properties.HistoryOutputNames = resultHistoryOutputNames;
            properties.Delimiter = delimiter;
            _controller.ExportResultHistoryOutputCommand(properties);
        }
        public void RemoveResultHistoryOutputs(string[] resultHistoryOutputNames)
        {
            _controller.RemoveResultHistoryOutputsCommand(resultHistoryOutputNames);
        }
        public void RemoveResultHistoryFields(string historyOutputName, string[] fieldNames)
        {
            _controller.RemoveResultHistoryFieldsCommand(historyOutputName, fieldNames);
        }
        public void RemoveResultHistoryComponents(string historyOutputName, string fieldName,
                                                  string[] componentNames)
        {
            _controller.RemoveResultHistoryFieldsCommand(historyOutputName, fieldName, componentNames);
        }
    }
}
