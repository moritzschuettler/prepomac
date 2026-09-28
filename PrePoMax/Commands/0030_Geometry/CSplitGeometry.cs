// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using PrePoMax.Python.Journal;


namespace PrePoMax.Commands
{
    [Serializable]
    class CSplitGeometry : PreprocessCommand
    {
        // Variables                                                                                                                
        private GeometryFeature _geometryFeature;


        // Properties
        public SplitToolSymbol ToolSymbol { get { return _geometryFeature.ToolSymbol; } }


        // Constructor                                                                                                              
        public CSplitGeometry(GeometryFeature geometryFeature)
            : base(geometryFeature.SplitType == SplitTypeEnum.Shell ?
                   "Split shell" : "Split solid")
        {
            _geometryFeature = geometryFeature.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            // The symbol is made from the geometry this execution meets, so a regenerate replaces it and a
            // split that fails before making it leaves none
            GeometryFeature geometryFeature = _geometryFeature.DeepClone();
            geometryFeature.ToolSymbol = null;
            try
            {
                receiver.SplitGeometry(geometryFeature);
            }
            finally
            {
                _geometryFeature.ToolSymbol = geometryFeature.ToolSymbol;
            }
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            string region = PythonSelectionEmitter.Emit(writer, _geometryFeature.CreationData);
            string tool = GetToolCall(writer);
            string fuzzyTolerance = _geometryFeature.FuzzyValue > 0 ?
                                    "fuzzy_tolerance=" + PythonValueFormatter.Number(_geometryFeature.FuzzyValue) : null;
            string method = _geometryFeature.SplitType == SplitTypeEnum.Shell ? "split_shell" : "split_solid";
            writer.Statement(writer.Call("pmx.model.geometry.parts", method, region, tool, fuzzyTolerance));
            writer.CountEmittedCommand();
        }
        // The picks are emitted rather than what was read from them, so that the replay reads the tool out of
        // the geometry it meets
        private string GetToolCall(PythonJournalWriter writer)
        {
            string splitter = _geometryFeature.SplitterData != null ?
                              PythonSelectionEmitter.Emit(writer, _geometryFeature.SplitterData) : null;
            //
            if (_geometryFeature is SplitByLineTwoPoints twoPoints)
                return writer.Call("pmx.create", "split_tool_line_by_two_points",
                                   splitter == null ? "points=" + PythonValueFormatter.List(twoPoints.Points) : null,
                                   splitter == null ? null : "splitter=" + splitter);
            if (_geometryFeature is SplitByPlaneThreePoints threePoints)
                return writer.Call("pmx.create", "split_tool_plane_by_three_points",
                                   splitter == null ? "points=" + PythonValueFormatter.List(threePoints.Points) : null,
                                   splitter == null ? null : "splitter=" + splitter, GetOffset(threePoints.Offset));
            // A pick gives the point or the normal or both, and what it does not give was typed, so both
            // values go with the picks; the ones the picks give are the same values again
            if (_geometryFeature is SplitByPointAndNormal pointAndNormal)
                return writer.Call("pmx.create", "split_tool_plane_by_point_and_normal",
                                   "point=" + PythonValueFormatter.List(pointAndNormal.Point),
                                   "normal=" + PythonValueFormatter.List(pointAndNormal.Normal),
                                   splitter == null ? null : "splitter=" + splitter, GetOffset(pointAndNormal.Offset));
            if (_geometryFeature is SplitByExtendedFace extendedFace)
                return writer.Call("pmx.create", "split_tool_extend_face", splitter, GetOffset(extendedFace.Offset));
            if (_geometryFeature is SplitByExtrudedEdges extrudedEdges)
            {
                string direction;
                if (extrudedEdges.DirectionType == SplitExtrusionDirectionTypeEnum.Vector)
                    direction = "direction=" + PythonValueFormatter.List(extrudedEdges.Direction);
                else
                    direction = "direction_selection=" + PythonSelectionEmitter.Emit(writer, extrudedEdges.DirectionData);
                string length = extrudedEdges.LengthType == SplitExtrusionLengthTypeEnum.Value ?
                                "length=" + PythonValueFormatter.Number(extrudedEdges.Length) : null;
                string bothDirections = extrudedEdges.BothDirections ? null : "both_directions=False";
                return writer.Call("pmx.create", "split_tool_extrude_edges", splitter, direction, length, bothDirections);
            }
            throw new NotSupportedException("the splitting tool " + _geometryFeature.GetType().Name +
                                            " has no Python form");
        }
        private static string GetOffset(double offset)
        {
            return offset == 0 ? null : "offset=" + PythonValueFormatter.Number(offset);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _geometryFeature.DisplayName;
        }
    }
}
