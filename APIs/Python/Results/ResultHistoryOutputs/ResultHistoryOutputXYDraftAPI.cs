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
        [PythonAPI("The X history component as [history_output_name, field_name, component_name], the form " +
                   "the x_source argument takes.", Returns = "StringList")]
        public StringListAPI x_source { get { return ResultHistoryOutputAPI.ToSourceNames(_xSource); } }
        [PythonAPI("The Y history component as [history_output_name, field_name, component_name], the form " +
                   "the y_source argument takes.", Returns = "StringList")]
        public StringListAPI y_source { get { return ResultHistoryOutputAPI.ToSourceNames(_ySource); } }


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
                   ", x_source=" + StringListAPI.ToDisplayString(_xSource.GetDisplayName()) +
                   ", y_source=" + StringListAPI.ToDisplayString(_ySource.GetDisplayName()) + ")";
        }
    }
}
