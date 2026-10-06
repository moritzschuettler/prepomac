// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python
{
    // The splitting tool of a split face or split solid operation. It is a detached description: its
    // selections are resolved against the geometry only when the split that takes it is applied, so the
    // same tool can be used again after the geometry has changed.
    public class SplitToolAPI : BaseAPI
    {
        // Variables
        private readonly GeometryFeature _geometryFeature;
        private readonly SelectionAPI _splitter;
        private readonly string _splitterArgumentName;
        private readonly SelectionAPI _directionSelection;


        // Properties
        [PythonAPI("The splitting operation, as the split dialogs name it, for example 'Plane by three points'.",
                   Returns = "str")]
        public string tool_type { get { return _geometryFeature.DisplayName; } }
        internal GeometryFeature GeometryFeature { get { return _geometryFeature; } }
        internal SelectionAPI Splitter { get { return _splitter; } }
        internal string SplitterArgumentName { get { return _splitterArgumentName; } }
        internal SelectionAPI DirectionSelection { get { return _directionSelection; } }


        // Constructors
        internal SplitToolAPI(GeometryFeature geometryFeature, SelectionAPI splitter, string splitterArgumentName,
                              SelectionAPI directionSelection)
        {
            _geometryFeature = geometryFeature;
            _splitter = splitter;
            _splitterArgumentName = splitterArgumentName;
            _directionSelection = directionSelection;
        }


        // Methods
        public override string ToString()
        {
            return "SplitTool(type=" + StringListAPI.ToDisplayString(_geometryFeature.DisplayName) + ")";
        }
    }
}
