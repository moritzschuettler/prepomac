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

namespace APIs.Python
{
    public abstract class ResultHistoryOutputDraftAPI : BaseAPI
    {
        // Variables
        private readonly string _name;
        private readonly object _region;
        private ResultHistoryOutputFilterAPI _filter1;
        private ResultHistoryOutputFilterAPI _filter2;


        // Properties
        [PythonAPI("The result history output name.", Returns = "str")]
        public string name { get { return _name; } }
        [PythonAPI("The selection or read-only result mesh region used by the history output, or None for an " +
                   "equation output.", Returns = "Selection, NodeSet, ElementSet, Surface, or None")]
        public object region { get { return _region; } }
        [PythonAPI("The optional first history-output filter.", Returns = "HistoryOutputFilter or None")]
        public ResultHistoryOutputFilterAPI filter1
        {
            get { return _filter1; }
            set { _filter1 = value; }
        }
        [PythonAPI("The optional second history-output filter.", Returns = "HistoryOutputFilter or None")]
        public ResultHistoryOutputFilterAPI filter2
        {
            get { return _filter2; }
            set { _filter2 = value; }
        }


        // Constructors
        protected ResultHistoryOutputDraftAPI(string name, object region)
        {
            _name = name;
            _region = region is SelectionAPI selection ? selection.DeepCopy() : region;
        }


        // Methods
        internal ResultHistoryOutput CreateBase(SelectionService selectionService, string resultName)
        {
            if (_filter1 == null && _filter2 != null)
                throw new PythonAPIException("The draft filter2 cannot be used without filter1.");
            ResultHistoryOutput output = CreateHistoryOutput();
            if (_region is SelectionAPI selection)
            {
                SelectionCompilation compilation = selectionService.CompileForResult(selection, resultName,
                                                                                      GetRequiredTarget());
                output.RegionType = CaeGlobals.RegionTypeEnum.Selection;
                output.RegionName = "";
                output.CreationData = compilation.Selection;
                output.CreationIds = compilation.Ids;
            }
            else if (_region is ResultMeshRegionAPI meshRegion)
            {
                meshRegion.ValidateForResult(resultName);
                output.RegionType = meshRegion.RegionType;
                output.RegionName = meshRegion.name;
            }
            else if (_region != null)
                throw new PythonAPIException("The history output region type '" + _region.GetType().Name +
                                             "' is not supported.");
            if (_filter1 != null) output.Filter1 = _filter1.CreateBase();
            if (_filter2 != null) output.Filter2 = _filter2.CreateBase();
            return output;
        }
        protected abstract ResultHistoryOutput CreateHistoryOutput();
        protected abstract string GetRequiredTarget();
        public override string ToString()
        {
            return "HistoryOutputDraft(name=" + StringListAPI.ToDisplayString(_name) + ")";
        }
    }
}
