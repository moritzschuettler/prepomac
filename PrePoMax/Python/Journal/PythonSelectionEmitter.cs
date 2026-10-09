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
using CaeGlobals;

namespace PrePoMax.Python.Journal
{
    // Turns a recorded Selection back into the pmx.create.selection_* calls that rebuild it. The nodes
    // are emitted in the order they were recorded rather than in an algebraically equivalent one,
    // because a set operation depends on what the nodes before it selected.
    public static class PythonSelectionEmitter
    {
        // Methods                                                                                                                  
        public static string Emit(PythonJournalWriter writer, Selection selection)
        {
            if (selection == null || selection.Nodes == null || selection.Nodes.Count == 0)
                throw new NotSupportedException("the selection carries no nodes");
            //
            string view = GetViewName(selection.CurrentView);
            string target = GetTargetName(selection.SelectItem);
            List<string> calls = new List<string>();
            //
            for (int i = 0; i < selection.Nodes.Count; i++)
            {
                if (i == 0) calls.Add(GetCreateCall(selection.Nodes[i], view, target));
                else calls.Add(GetAddCall(selection.Nodes[i], target));
            }
            string options = GetOptionsCall(selection);
            if (options != null) calls.Add(options);
            // An identical selection reuses the variable the earlier statements bound
            string key = "selection|" + view + "|" + target + "|" + string.Join(";", calls);
            string existing = writer.Scope.GetVariableName(key);
            if (existing != null) return existing;
            //
            string variableName = writer.Scope.CreateVariableName("selection", key);
            writer.Assign(variableName, calls[0]);
            for (int i = 1; i < calls.Count; i++) writer.Statement(variableName + "." + calls[i]);
            //
            return variableName;
        }
        private static string GetCreateCall(SelectionNode node, string view, string target)
        {
            string viewValue = PythonValueFormatter.Text(view);
            string targetValue = PythonValueFormatter.Text(target);
            // The first node of a selection is applied to an empty set, so the API leaves its operation
            // implicit; a first node that subtracts or intersects has no meaning to reproduce
            if (node.SelectOperation != vtkSelectOperation.None && node.SelectOperation != vtkSelectOperation.Add)
                throw new NotSupportedException("the first selection node carries the operation " +
                                                node.SelectOperation);
            //
            SelectionNodeIds idsNode = node as SelectionNodeIds;
            if (idsNode != null)
            {
                if (idsNode.SelectAll)
                    return PythonJournalWriter.Join("pmx.create.selection_all", viewValue, targetValue);
                //
                return PythonJournalWriter.Join("pmx.create.selection_by_ids", viewValue, targetValue,
                                                PythonValueFormatter.Text(GetIdsSource(idsNode, target)),
                                                PythonValueFormatter.List(idsNode.ItemIds),
                                                GetGeometrySelectMode(idsNode, false));
            }
            SelectionNodeMouse mouseNode = node as SelectionNodeMouse;
            if (mouseNode != null)
            {
                if (mouseNode.PlaneParameters != null)
                    return PythonJournalWriter.Join("pmx.create.selection_by_area", viewValue, targetValue,
                                                    GetPlanes(mouseNode), GetSelectBy(mouseNode),
                                                    GetAngle(mouseNode), GetPartIds(mouseNode),
                                                    GetCompletelyInside(mouseNode), null,
                                                    GetPartOffsets(mouseNode));
                //
                return PythonJournalWriter.Join("pmx.create.selection_by_pick", viewValue, targetValue,
                                                GetPoint(mouseNode), GetSelectBy(mouseNode), GetAngle(mouseNode),
                                                GetDirection(mouseNode), GetPartIds(mouseNode),
                                                GetCompletelyInside(mouseNode), null, GetPrecision(mouseNode),
                                                GetPartOffsets(mouseNode));
            }
            if (node is SelectionNodeInvert)
                throw new NotSupportedException("a selection cannot begin with an invert");
            //
            throw new NotSupportedException("the selection node type " + node.GetType().Name + " has no Python form");
        }
        private static string GetAddCall(SelectionNode node, string target)
        {
            string operation = PythonValueFormatter.Text(node.SelectOperation.ToString());
            //
            SelectionNodeIds idsNode = node as SelectionNodeIds;
            if (idsNode != null)
            {
                if (idsNode.SelectAll) return PythonJournalWriter.Join("add_all", operation);
                //
                return PythonJournalWriter.Join("add_by_ids",
                                                PythonValueFormatter.Text(GetIdsSource(idsNode, target)),
                                                PythonValueFormatter.List(idsNode.ItemIds),
                                                "operation=" + operation,
                                                GetGeometrySelectMode(idsNode, true));
            }
            SelectionNodeMouse mouseNode = node as SelectionNodeMouse;
            if (mouseNode != null)
            {
                if (mouseNode.PlaneParameters != null)
                    return PythonJournalWriter.Join("add_by_area", GetPlanes(mouseNode), GetSelectBy(mouseNode),
                                                    GetAngle(mouseNode), GetPartIds(mouseNode),
                                                    GetCompletelyInside(mouseNode), "operation=" + operation,
                                                    GetPartOffsets(mouseNode));
                //
                return PythonJournalWriter.Join("add_by_pick", GetPoint(mouseNode), GetSelectBy(mouseNode),
                                                GetAngle(mouseNode), GetDirection(mouseNode), GetPartIds(mouseNode),
                                                GetCompletelyInside(mouseNode), "operation=" + operation,
                                                GetPrecision(mouseNode), GetPartOffsets(mouseNode));
            }
            if (node is SelectionNodeInvert) return "invert()";
            //
            throw new NotSupportedException("the selection node type " + node.GetType().Name + " has no Python form");
        }
        // Only the sources the application stores unchanged can be given back to it. A geometry pick made
        // in the Selection by ID mode is recorded as geometry IDs, and node, element and part IDs stand
        // for themselves; a raw surface or edge ID has no source of its own.
        private static string GetIdsSource(SelectionNodeIds node, string target)
        {
            if (node.IsGeometryBased) return "geometry";
            if (target == "nodes" || target == "elements" || target == "parts") return target;
            //
            throw new NotSupportedException("a selection of raw '" + target + "' IDs has no API source");
        }
        private static string GetGeometrySelectMode(SelectionNodeIds node, bool named)
        {
            if (!node.IsGeometryBased || node.GeometrySelectMode == GeometrySelectModeEnum.SelectId) return null;
            //
            string value = PythonValueFormatter.Text(node.GeometrySelectMode.ToString());
            return named ? "geometry_select_mode=" + value : value;
        }
        private static string GetPoint(SelectionNodeMouse node)
        {
            if (node.PickedPoint == null) throw new NotSupportedException("the recorded pick carries no point");
            //
            return PythonValueFormatter.List(node.PickedPoint);
        }
        private static string GetPlanes(SelectionNodeMouse node)
        {
            List<string> rows = new List<string>();
            foreach (double[] plane in node.PlaneParameters) rows.Add(PythonValueFormatter.List(plane));
            //
            return "[" + string.Join(", ", rows) + "]";
        }
        private static string GetSelectBy(SelectionNodeMouse node)
        {
            if (node.SelectBy == vtkSelectBy.Off || node.SelectBy == vtkSelectBy.Default ||
                node.SelectBy == vtkSelectBy.Id || node.SelectBy == vtkSelectBy.Widget)
                throw new NotSupportedException("the pick mode " + node.SelectBy + " cannot be replayed");
            //
            return PythonValueFormatter.Text(node.SelectBy.ToString());
        }
        private static string GetAngle(SelectionNodeMouse node)
        {
            bool needsAngle = node.SelectBy == vtkSelectBy.EdgeAngle || node.SelectBy == vtkSelectBy.SurfaceAngle ||
                              node.SelectBy == vtkSelectBy.GeometryEdgeAngle ||
                              node.SelectBy == vtkSelectBy.GeometrySurfaceAngle;
            if (!needsAngle) return null;
            //
            return "angle_deg=" + PythonValueFormatter.Number(node.Angle);
        }
        private static string GetDirection(SelectionNodeMouse node)
        {
            if (node.SelectionDirection == null) return null;
            //
            return "direction=" + PythonValueFormatter.List(node.SelectionDirection);
        }
        // The recorded part IDs are emitted rather than left out, because a pick searches only the parts
        // it names, and the API would otherwise fill in every part of the mesh
        private static string GetPartIds(SelectionNodeMouse node)
        {
            if (node.PartIds == null || node.PartIds.Length == 0) return null;
            //
            return "part_ids=" + PythonValueFormatter.List(node.PartIds);
        }
        private static string GetCompletelyInside(SelectionNodeMouse node)
        {
            if (!node.CompletelyInside) return null;
            //
            return "completely_inside=True";
        }
        // The application takes the precision from the current view the first time a point pick is
        // resolved, so a recorded one is emitted to keep the pick independent of the view it replays in
        private static string GetPrecision(SelectionNodeMouse node)
        {
            if (node.Precision <= 0) return null;
            //
            return "precision=" + PythonValueFormatter.Number(node.Precision);
        }
        // The node stores one offset per selected part in the order of its part IDs, while the API takes
        // [part_id, dx, dy, dz] rows, so that sorting the selection cannot reorder them
        private static string GetPartOffsets(SelectionNodeMouse node)
        {
            if (node.PartOffsets == null || node.PartOffsets.Length == 0) return null;
            if (node.PartIds == null || node.PartIds.Length != node.PartOffsets.Length)
                throw new NotSupportedException("the recorded part offsets do not match the picked parts");
            //
            List<string> rows = new List<string>();
            for (int i = 0; i < node.PartOffsets.Length; i++)
            {
                double[] offset = node.PartOffsets[i];
                if (offset == null || offset.Length < 3)
                    throw new NotSupportedException("a recorded part offset is incomplete");
                //
                rows.Add("[" + node.PartIds[i] + ", " + PythonValueFormatter.Number(offset[0]) + ", " +
                         PythonValueFormatter.Number(offset[1]) + ", " + PythonValueFormatter.Number(offset[2]) + "]");
            }
            //
            return "part_offsets=[" + string.Join(", ", rows) + "]";
        }
        // Only the options that differ from a new selection are emitted, so that the common case stays
        // one readable line
        private static string GetOptionsCall(Selection selection)
        {
            Selection defaults = new Selection();
            List<string> arguments = new List<string>();
            //
            if (selection.MaxNumberOfGeometryIds != defaults.MaxNumberOfGeometryIds)
                arguments.Add("max_geometry_ids=" + selection.MaxNumberOfGeometryIds);
            if (selection.MaxNumberOfItemIds != defaults.MaxNumberOfItemIds)
                arguments.Add("max_item_ids=" + selection.MaxNumberOfItemIds);
            if (selection.LimitSelectionToFirstPart) arguments.Add("limit_to='FirstPart'");
            else if (selection.LimitSelectionToFirstGeometryType) arguments.Add("limit_to='FirstGeometryType'");
            else if (selection.LimitSelectionToFirstMesherType) arguments.Add("limit_to='FirstMesherType'");
            if (selection.LimitSelectionToShellEdges) arguments.Add("shell_edges_only=True");
            if (selection.EnableShellEdgeFaceSelection) arguments.Add("enable_shell_edge_faces=True");
            //
            if (arguments.Count == 0) return null;
            //
            return "configure(" + string.Join(", ", arguments) + ")";
        }
        // A selection that never had its view set - a search contact pairs item of an older history, or
        // a v0.5.2 one - is resolved by the controller in the FE model view, so it replays there
        private static string GetViewName(int currentView)
        {
            if (currentView == -1) return "femodel";
            if (currentView == (int)ViewGeometryModelResults.Geometry) return "geometry";
            if (currentView == (int)ViewGeometryModelResults.Model) return "femodel";
            if (currentView == (int)ViewGeometryModelResults.Results) return "results";
            //
            throw new NotSupportedException("the selection was not recorded in a view that can be named");
        }
        private static string GetTargetName(vtkSelectItem selectItem)
        {
            if (selectItem == vtkSelectItem.Node) return "nodes";
            if (selectItem == vtkSelectItem.Element) return "elements";
            if (selectItem == vtkSelectItem.Surface) return "surfaces";
            if (selectItem == vtkSelectItem.GeometryEdge) return "edges";
            if (selectItem == vtkSelectItem.Part) return "parts";
            if (selectItem == vtkSelectItem.Geometry) return "geometry";
            if (selectItem == vtkSelectItem.GeometrySurface) return "geometry_surfaces";
            //
            throw new NotSupportedException("the selection target " + selectItem + " has no API form");
        }
    }
}
