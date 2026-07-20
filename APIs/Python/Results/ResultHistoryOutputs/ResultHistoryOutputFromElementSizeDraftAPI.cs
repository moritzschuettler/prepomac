// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeResults;

namespace APIs.Python
{
    public class ResultHistoryOutputFromElementSizeDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly ElementSizeTypeEnum _sizeType;
        private readonly string _deformationVariableName;


        // Properties
        [PythonAPI("The element size type. Possible values are 'area' and 'volume'.", Returns = "str")]
        public string size_type { get { return _sizeType == ElementSizeTypeEnum.Area ? "area" : "volume"; } }
        [PythonAPI("The deformation variable name.", Returns = "str")]
        public string deformation_variable_name { get { return _deformationVariableName; } }


        // Constructors
        internal ResultHistoryOutputFromElementSizeDraftAPI(string name, ElementSizeTypeEnum sizeType,
                                                             object region,
                                                             string deformationVariableName)
            : base(name, region)
        {
            _sizeType = sizeType;
            _deformationVariableName = deformationVariableName;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            return new ResultHistoryOutputFromElementSize(name, _sizeType, "", RegionTypeEnum.None,
                                                          _deformationVariableName);
        }
        protected override string GetRequiredTarget()
        {
            return _sizeType == ElementSizeTypeEnum.Area ? "surfaces" : "elements";
        }
        public override string ToString()
        {
            return "HistoryOutputFromElementSizeDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", size_type=" + StringListAPI.ToDisplayString(size_type) + ")";
        }
    }
}
