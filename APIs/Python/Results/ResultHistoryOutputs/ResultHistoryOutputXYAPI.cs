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
    public class ResultHistoryOutputXYAPI : ResultHistoryOutputAPI
    {
        // Properties
        [PythonAPI("The X history component as [history_output_name, field_name, component_name], the form " +
                   "the x_source argument takes.", Returns = "StringList")]
        public StringListAPI x_source { get { return ToSourceNames(ResolveXY().XSource); } }
        [PythonAPI("The Y history component as [history_output_name, field_name, component_name], the form " +
                   "the y_source argument takes.", Returns = "StringList")]
        public StringListAPI y_source { get { return ToSourceNames(ResolveXY().YSource); } }


        // Constructors
        internal ResultHistoryOutputXYAPI(string resultName, string historyOutputName,
                                          ResultHistoryOutputDataService resultHistoryService)
            : base(resultName, historyOutputName, resultHistoryService)
        {
        }


        // Methods
        private ResultHistoryOutputXY ResolveXY()
        {
            ResultHistoryOutputXY historyOutput = Resolve() as ResultHistoryOutputXY;
            if (historyOutput == null)
                throw new PythonAPIException("The history output '" + _historyOutputName +
                                             "' is not an XY history output.");
            return historyOutput;
        }
        public override string ToString()
        {
            ResultHistoryOutputXY historyOutput = ResolveXY();
            return "HistoryOutputXY(name=" + StringListAPI.ToDisplayString(_historyOutputName) +
                   ", x_source=" + StringListAPI.ToDisplayString(historyOutput.XSource.GetDisplayName()) +
                   ", y_source=" + StringListAPI.ToDisplayString(historyOutput.YSource.GetDisplayName()) + ")";
        }
    }
}
