// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultHistoryOutputXYDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly HistoryResultData _xSource;
        private readonly HistoryResultData _ySource;


        // Properties
        [PythonAPI("The X history component as 'history_output_name.field_name.component_name'.", Returns = "str")]
        public string x_source { get { return _xSource.GetDisplayName(); } }
        [PythonAPI("The Y history component as 'history_output_name.field_name.component_name'.", Returns = "str")]
        public string y_source { get { return _ySource.GetDisplayName(); } }


        // Constructors
        internal ResultHistoryOutputXYDraftAPI(string name, HistoryResultData xSource, HistoryResultData ySource)
            : base(name, null)
        {
            _xSource = xSource;
            _ySource = ySource;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            return new ResultHistoryOutputXY(name, _xSource, _ySource);
        }
        protected override string GetRequiredTarget()
        {
            return null;
        }
        public override string ToString()
        {
            return "HistoryOutputXYDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", x_source=" + StringListAPI.ToDisplayString(x_source) +
                   ", y_source=" + StringListAPI.ToDisplayString(y_source) + ")";
        }
    }
}
