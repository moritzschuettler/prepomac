// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using CaeResults;
using System.Drawing;

namespace APIs.Python
{
    public interface IPythonCommandGateway
    {
        // Results
        void SetCurrentResults(string resultName);
        void RemoveCurrentResults();
        void ClearResults();
        // Result parts
        void HideResultParts(string[] names);
        void ShowResultParts(string[] names);
        void SetResultPartColor(string[] names, Color color);
        void ResetResultPartColor(string[] names);
        void SetResultPartTransparency(string[] names, byte alpha);
        void SetResultPartRepresentation(string[] names, bool resultRepresentation);
        void ReplaceResultPartProperties(string oldName, PartProperties properties);
        void MergeResultParts(string[] names);
        void RemoveResultParts(string[] names);
        // Result reference points
        void AddResultReferencePoint(FeReferencePoint point);
        void ReplaceResultReferencePoint(string oldName, FeReferencePoint point);
        void HideResultReferencePoints(string[] names);
        void ShowResultReferencePoints(string[] names);
        void DuplicateResultReferencePoints(string[] names);
        void RemoveResultReferencePoints(string[] names);
        // Result coordinate systems
        void AddResultCoordinateSystem(CoordinateSystem coordinateSystem);
        void ReplaceResultCoordinateSystem(string oldName, CoordinateSystem coordinateSystem);
        void HideResultCoordinateSystems(string[] names);
        void ShowResultCoordinateSystems(string[] names);
        void DuplicateResultCoordinateSystems(string[] names);
        void RemoveResultCoordinateSystems(string[] names);
        // Result field outputs
        void DuplicateResultFieldOutputs(string[] names);
        void AddResultFieldOutput(ResultFieldOutput resultFieldOutput);
        void ReplaceResultFieldOutput(string oldResultFieldOutputName, ResultFieldOutput resultFieldOutput);
        void ExportResultFieldOutput(string fileName, string resultName, FieldData fieldData);
        void RemoveResultFieldOutputs(string[] resultFieldOutputNames);
        void RemoveResultFieldOutputComponents(string fieldOutputName, string[] componentNames);
        void AddResultHistoryOutput(ResultHistoryOutput resultHistoryOutput);
        void ReplaceResultHistoryOutput(string oldResultHistoryOutputName, ResultHistoryOutput resultHistoryOutput);
        void DuplicateResultHistoryOutputs(string[] resultHistoryOutputNames);
        void ExportResultHistoryOutput(string fileName, string[] resultHistoryOutputNames, string delimiter);
        void RemoveResultHistoryOutputs(string[] resultHistoryOutputNames);
        void RemoveResultHistoryFields(string historyOutputName, string[] fieldNames);
        void RemoveResultHistoryComponents(string historyOutputName, string fieldName, string[] componentNames);
    }
}
