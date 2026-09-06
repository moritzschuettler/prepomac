// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python;
using CaeMesh;
using CaeResults;
using System.Drawing;

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
        public void RemoveCurrentResults()
        {
            _controller.RemoveCurrentResultsCommand();
        }
        public void ClearResults()
        {
            _controller.ClearResultsCommand();
        }
        // Result parts
        public void HideResultParts(string[] names)
        {
            _controller.HideResultPartsCommand(names);
        }
        public void ShowResultParts(string[] names)
        {
            _controller.ShowResultPartsCommand(names);
        }
        public void SetResultPartColor(string[] names, Color color)
        {
            _controller.SetColorForResultPartsCommand(names, color);
        }
        public void ResetResultPartColor(string[] names)
        {
            _controller.ResetColorForResultPartsCommand(names);
        }
        public void SetResultPartTransparency(string[] names, byte alpha)
        {
            _controller.SetTransparencyForResultPartsCommand(names, alpha);
        }
        public void SetResultPartRepresentation(string[] names, bool resultRepresentation)
        {
            _controller.SetResultRepresentationForResultPartsCommand(names, resultRepresentation);
        }
        public void ReplaceResultPartProperties(string oldName, PartProperties properties)
        {
            _controller.ReplaceResultPartPropertiesCommand(oldName, properties);
        }
        public void MergeResultParts(string[] names)
        {
            _controller.MergeResultPartsCommand(names);
        }
        public void RemoveResultParts(string[] names)
        {
            _controller.RemoveResultPartsCommand(names);
        }
        // Result reference points
        public void AddResultReferencePoint(FeReferencePoint point)
        {
            _controller.AddResultReferencePointCommand(point);
        }
        public void ReplaceResultReferencePoint(string oldName, FeReferencePoint point)
        {
            _controller.ReplaceResultReferencePointCommand(oldName, point);
        }
        public void HideResultReferencePoints(string[] names)
        {
            _controller.HideResultReferencePointsCommand(names);
        }
        public void ShowResultReferencePoints(string[] names)
        {
            _controller.ShowResultReferencePointsCommand(names);
        }
        public void DuplicateResultReferencePoints(string[] names)
        {
            _controller.DuplicateResultReferencePointsCommand(names);
        }
        public void RemoveResultReferencePoints(string[] names)
        {
            _controller.RemoveResultReferencePointsCommand(names);
        }
        // Result coordinate systems
        public void AddResultCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            _controller.AddResultCoordinateSystemCommand(coordinateSystem);
        }
        public void ReplaceResultCoordinateSystem(string oldName, CoordinateSystem coordinateSystem)
        {
            _controller.ReplaceResultCoordinateSystemCommand(oldName, coordinateSystem);
        }
        public void HideResultCoordinateSystems(string[] names)
        {
            _controller.HideResultCoordinateSystemsCommand(names);
        }
        public void ShowResultCoordinateSystems(string[] names)
        {
            _controller.ShowResultCoordinateSystemsCommand(names);
        }
        public void DuplicateResultCoordinateSystems(string[] names)
        {
            _controller.DuplicateResultCoordinateSystemsCommand(names);
        }
        public void RemoveResultCoordinateSystems(string[] names)
        {
            _controller.RemoveResultCoordinateSystemsCommand(names);
        }
        // Result field outputs
        public void DuplicateResultFieldOutputs(string[] names)
        {
            _controller.DuplicateResultFieldOutputsCommand(names);
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
