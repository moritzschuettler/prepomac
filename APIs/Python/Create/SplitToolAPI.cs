// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Collections.Generic;

namespace APIs.Python
{
    // The splitting tool of a split face or split solid operation. It is a detached description: its
    // selections are resolved against the geometry only when the split that takes it is applied, so the
    // same tool can be used again after the geometry has changed.
    public class SplitToolAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ToolTypes = new Dictionary<Type, string>
        {
            { typeof(SplitByLineTwoPoints), "line_by_two_points" },
            { typeof(SplitByPlaneThreePoints), "plane_by_three_points" },
            { typeof(SplitByPointAndNormal), "plane_by_point_and_normal" },
            { typeof(SplitByExtendedFace), "extend_face" },
            { typeof(SplitByExtrudedEdges), "extrude_edges" }
        };
        private readonly GeometryFeature _geometryFeature;
        private readonly SelectionAPI _splitter;
        private readonly string _splitterArgumentName;
        private readonly SelectionAPI _directionSelection;


        // Properties
        [PythonAPI("The splitting tool, named after the pmx.create method that makes it without split_tool_, " +
                   "for example 'plane_by_three_points' for split_tool_plane_by_three_points.", Returns = "str")]
        public string tool_type { get { return GetToolType(_geometryFeature); } }
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


        // Static methods
        private static string GetToolType(GeometryFeature geometryFeature)
        {
            string toolType;
            if (ToolTypes.TryGetValue(geometryFeature.GetType(), out toolType)) return toolType;
            //
            throw new PythonAPIException("The splitting tool " + geometryFeature.GetType().Name + " is not made " +
                                         "by any pmx.create method.");
        }


        // Methods
        public override string ToString()
        {
            return "SplitTool(type=" + StringListAPI.ToDisplayString(GetToolType(_geometryFeature)) + ")";
        }
    }
}
