// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace PrePoMax.API.Python
{
    internal interface IPythonCommandGateway
    {
        void SetCurrentResults(string resultName);
        void AddResultFieldOutput(ResultFieldOutput resultFieldOutput);
        void ReplaceResultFieldOutput(string oldResultFieldOutputName, ResultFieldOutput resultFieldOutput);
        void ExportResultFieldOutput(string fileName, string resultName, FieldData fieldData);
        void RemoveResultFieldOutputs(string[] resultFieldOutputNames);
        void RemoveResultFieldOutputComponents(string fieldOutputName, string[] componentNames);
    }
}
