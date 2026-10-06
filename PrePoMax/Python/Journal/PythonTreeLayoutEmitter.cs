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
    // gesture that produced it, and the API offers only the gestures, so the layout is rebuilt as a
    // depth first run of add_group calls: a group carries its own id, its parent's id, its position
    // and the items directly under it, which is exactly what add_group takes.
    public static class PythonTreeLayoutEmitter
    {
        // Methods                                                                                                                  
        public static void Emit(PythonJournalWriter writer, ViewGeometryModelResults view, Type itemType,
                                string parentName, OrderedDictionaryItemLayout<string> layout)
        {
            string viewName = GetViewName(view);
            string itemTypeName = ControllerPythonModelGateway.GetTreeItemTypeName(itemType);
            string stepName = string.IsNullOrEmpty(parentName)
                              ? null : "step_name=" + PythonValueFormatter.Text(parentName);
            //
            if (layout == null || layout.Nodes == null || layout.Nodes.Count == 0)
            {
                writer.Comment("The tree layout of " + itemTypeName + " carries no groups");
                return;
            }
            EmitGroups(writer, layout.Nodes, null, viewName, itemTypeName, stepName);
        }
        private static void EmitGroups(PythonJournalWriter writer,
                                       IEnumerable<OrderedDictionaryItemNode<string>> nodes,
                                       string parentGroupId, string viewName, string itemTypeName,
                                       string stepName)
        {
            int index = 0;
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (!node.IsGroup)
                {
                    index++;
                    continue;
                }
                // A group is created before its children, so a child can name it as its parent
                List<string> itemNames = new List<string>();
                foreach (OrderedDictionaryItemNode<string> child in node.Children)
                {
                    if (child.IsItem) itemNames.Add(child.Key);
                }
                writer.Statement(writer.Call("pmx.model.tree", "add_group",
                                             PythonValueFormatter.Text(viewName),
                                             PythonValueFormatter.Text(itemTypeName),
                                             PythonValueFormatter.Text(node.Name),
                                             "group_id=" + PythonValueFormatter.Text(node.Id.ToString()),
                                             parentGroupId == null
                                             ? null : "parent_group_id=" + PythonValueFormatter.Text(parentGroupId),
                                             "target_index=" + PythonValueFormatter.Format(index),
                                             "item_names=" + PythonValueFormatter.List(itemNames),
                                             stepName));
                EmitGroups(writer, node.Children, node.Id.ToString(), viewName, itemTypeName, stepName);
                index++;
            }
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
