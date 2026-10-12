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
    // The model tree grouping. A layout command stores the whole resulting arrangement rather than the
    // gesture that produced it, so it is written as the one call that sets a whole layout: replayed after
    // the same history it sets the same groups, ids and order, and a layout without groups removes them.
    public static class PythonTreeLayoutEmitter
    {
        // Methods                                                                                                                  
        public static void Emit(PythonJournalWriter writer, ViewGeometryModelResults view, Type itemType,
                                string parentName, OrderedDictionaryItemLayout<string> layout,
                                Dictionary<Guid, string> compoundPartGroupNames)
        {
            string viewName = GetViewName(view);
            string itemTypeName = ControllerPythonModelGateway.GetTreeItemTypeName(itemType);
            string stepName = string.IsNullOrEmpty(parentName)
                              ? null : "step_name=" + PythonValueFormatter.Text(parentName);
            //
            writer.Statement(writer.Call("pmx.model.tree", "set_layout",
                                         PythonValueFormatter.Text(viewName),
                                         PythonValueFormatter.Text(itemTypeName),
                                         FormatEntries(layout == null ? null : layout.Nodes, compoundPartGroupNames),
                                         stepName));
        }
        // The group a compound part's mesh is gathered in gets a new id whenever the compound is made, so it is
        // written as the id the compound carries when the journal runs
        private static string FormatEntries(IEnumerable<OrderedDictionaryItemNode<string>> nodes,
                                            Dictionary<Guid, string> compoundPartGroupNames)
        {
            List<string> entries = new List<string>();
            if (nodes != null)
            {
                foreach (OrderedDictionaryItemNode<string> node in nodes)
                {
                    if (node.IsItem)
                    {
                        entries.Add(PythonValueFormatter.Text(node.Key));
                        continue;
                    }
                    //
                    string compoundPartName;
                    string groupId;
                    if (compoundPartGroupNames != null &&
                        compoundPartGroupNames.TryGetValue(node.Id, out compoundPartName))
                        groupId = "pmx.model.geometry.parts.get(" + PythonValueFormatter.Text(compoundPartName) +
                                  ").mesh_group_id";
                    else groupId = PythonValueFormatter.Text(node.Id.ToString());
                    entries.Add("{'group_name': " + PythonValueFormatter.Text(node.Name) +
                                ", 'group_id': " + groupId +
                                ", 'items': " + FormatEntries(node.Children, compoundPartGroupNames) + "}");
                }
            }
            return "[" + string.Join(", ", entries) + "]";
        }
        private static string GetViewName(ViewGeometryModelResults view)
        {
            if (view == ViewGeometryModelResults.Geometry) return "geometry";
            if (view == ViewGeometryModelResults.Model) return "femodel";
            if (view == ViewGeometryModelResults.Results) return "results";
            //
            throw new NotSupportedException("the tree view " + view + " has no name in the API");
        }
    }
}
