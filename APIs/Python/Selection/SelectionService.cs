// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeResults;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    internal class SelectionService
    {
        // Variables
        private readonly ResultResolver _resolver;
        private readonly IPythonSelectionGateway _selectionGateway;


        // Constructors
        public SelectionService(ResultResolver resolver, IPythonSelectionGateway selectionGateway)
        {
            _resolver = resolver;
            _selectionGateway = selectionGateway;
        }


        // Methods
        public SelectionAPI CreateByIds(string view, string target, string source, object ids)
        {
            ValidateView(view);
            ValidateTarget(target);
            SelectionAPI selection = new SelectionAPI(this, view, target);
            AddByIds(selection, source, ids);
            return selection;
        }
        public void AddByIds(SelectionAPI selection, string source, object ids)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            ValidateSource(source);
            ValidateCombination(selection.target, source);
            if (source == "edges" || source == "surfaces")
            {
                SortedDictionary<int, int[]> itemIds =
                    PythonIntListMapConverter.ToIntListMap(ids, "ids");
                selection.AddItem(new SelectionItemAPI(source, itemIds));
            }
            else
            {
                int[] itemIds = PythonIntListConverter.ToIntArray(ids, "ids");
                selection.AddItem(new SelectionItemAPI(source, itemIds));
            }
        }
        public void Highlight(SelectionAPI selection)
        {
            if (_selectionGateway == null)
                throw new PythonAPIException("Selection highlighting is not available in this Python API context.");
            FeMesh mesh = ResolveMesh(selection, null);
            SelectionCompilation compilation = Compile(selection, mesh, null, null);
            _selectionGateway.Highlight(compilation.Selection);
        }
        public void ClearHighlight()
        {
            if (_selectionGateway == null)
                throw new PythonAPIException("Selection highlighting is not available in this Python API context.");
            _selectionGateway.ClearHighlight();
        }
        public SelectionCompilation CompileForResult(SelectionAPI selection, string resultName, string target)
        {
            if (selection == null) throw new PythonAPIException("The history output selection must not be None.");
            FeMesh mesh = ResolveMesh(selection, resultName);
            return Compile(selection, mesh, "results", target);
        }
        private SelectionCompilation Compile(SelectionAPI selection, FeMesh mesh, string requiredView,
                                             string requiredTarget)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            if (requiredView != null && selection.view != requiredView)
                throw new PythonAPIException("The selection belongs to the '" + selection.view +
                                             "' view, but this operation requires a '" + requiredView +
                                             "' selection.");
            if (requiredTarget != null && selection.target != requiredTarget)
                throw new PythonAPIException("The selection target is '" + selection.target +
                                             "', but this operation requires target '" + requiredTarget + "'.");
            if (mesh == null) throw new PythonAPIException("The '" + selection.view + "' view does not contain a mesh.");
            //
            CaeGlobals.Selection compiled = new CaeGlobals.Selection();
            compiled.CurrentView = GetViewId(selection.view);
            compiled.SelectItem = ParseTarget(selection.target);
            HashSet<int> selectedIds = new HashSet<int>();
            //
            foreach (SelectionItemAPI item in selection.GetItems())
            {
                bool geometryBased;
                int[] itemIds = CompileItem(item, selection.target, mesh, out geometryBased);
                vtkSelectOperation operation = compiled.Nodes.Count == 0
                                               ? vtkSelectOperation.None
                                               : vtkSelectOperation.Add;
                if (geometryBased)
                {
                    compiled.Add(new SelectionNodeIds(operation, false, itemIds, true,
                                                      GeometrySelectModeEnum.SelectId));
                    selectedIds.UnionWith(mesh.GetIdsFromGeometryIds(itemIds, compiled.SelectItem, true));
                }
                else
                {
                    compiled.Add(new SelectionNodeIds(operation, false, itemIds));
                    selectedIds.UnionWith(itemIds);
                }
            }
            //
            int[] ids = selectedIds.OrderBy(id => id).ToArray();
            if (ids.Length == 0)
                throw new PythonAPIException("The selection does not resolve to any visible and active items in the '" +
                                             selection.view + "' view.");
            return new SelectionCompilation(compiled, ids);
        }
        private int[] CompileItem(SelectionItemAPI item, string target, FeMesh mesh, out bool geometryBased)
        {
            if (item.Source == "nodes")
            {
                ValidateDirectIds(item.Ids, mesh, "nodes");
                geometryBased = false;
                return item.Ids;
            }
            if (item.Source == "elements")
            {
                ValidateDirectIds(item.Ids, mesh, "elements");
                geometryBased = false;
                return item.Ids;
            }
            if (item.Source == "parts" && target == "parts")
            {
                ValidateDirectIds(item.Ids, mesh, "parts");
                geometryBased = false;
                return item.Ids;
            }
            // Geometry-based sources preserve their topology during command replay.
            geometryBased = true;
            if (item.Source == "vertices") return GetVertexGeometryIds(item.Ids, mesh);
            if (item.Source == "edges") return GetEdgeGeometryIds(item.PartIdItemIds, mesh);
            if (item.Source == "surfaces") return GetSurfaceGeometryIds(item.PartIdItemIds, mesh);
            if (item.Source == "parts") return GetPartGeometryIds(item.Ids, mesh);
            throw new PythonAPIException("Unsupported selection source '" + item.Source + "'.");
        }
        private static int[] GetVertexGeometryIds(int[] vertexNodeIds, FeMesh mesh)
        {
            HashSet<int> geometryIds = new HashSet<int>();
            foreach (int vertexNodeId in vertexNodeIds)
            {
                bool found = false;
                foreach (var entry in mesh.Parts)
                {
                    BasePart part = entry.Value;
                    if (!IsSelectable(part) || part.Visualization == null ||
                        part.Visualization.VertexNodeIds == null) continue;
                    for (int i = 0; i < part.Visualization.VertexNodeIds.Length; i++)
                    {
                        if (part.Visualization.VertexNodeIds[i] == vertexNodeId)
                        {
                            geometryIds.Add(FeMesh.GetGeometryId(i, (int)GeometryType.Vertex, part.PartId));
                            found = true;
                        }
                    }
                }
                if (!found)
                    throw new PythonAPIException("The visible vertex ID " + vertexNodeId +
                                                 " does not exist in the selected view.");
            }
            return geometryIds.OrderBy(id => id).ToArray();
        }
        private static int[] GetEdgeGeometryIds(SortedDictionary<int, int[]> partIdEdgeIds, FeMesh mesh)
        {
            List<int> geometryIds = new List<int>();
            foreach (var entry in partIdEdgeIds)
            {
                BasePart part = ResolveSelectablePart(mesh, entry.Key);
                int edgeCount = part.Visualization != null ? part.Visualization.EdgeCount : 0;
                foreach (int edgeId in entry.Value)
                {
                    if (edgeId > edgeCount)
                        throw new PythonAPIException("Edge ID " + edgeId + " does not exist on part ID " +
                                                     entry.Key + ".");
                    geometryIds.Add(FeMesh.GetGeometryId(edgeId - 1, (int)GeometryType.Edge, entry.Key));
                }
            }
            return geometryIds.Distinct().OrderBy(id => id).ToArray();
        }
        private static int[] GetSurfaceGeometryIds(SortedDictionary<int, int[]> partIdSurfaceIds, FeMesh mesh)
        {
            List<int> geometryIds = new List<int>();
            foreach (var entry in partIdSurfaceIds)
            {
                BasePart part = ResolveSelectablePart(mesh, entry.Key);
                int faceCount = part.Visualization != null ? part.Visualization.FaceCount : 0;
                GeometryType geometryType = GetSurfaceGeometryType(part);
                foreach (int surfaceId in entry.Value)
                {
                    if (surfaceId > faceCount)
                        throw new PythonAPIException("Surface ID " + surfaceId + " does not exist on part ID " +
                                                     entry.Key + ".");
                    geometryIds.Add(FeMesh.GetGeometryId(surfaceId - 1, (int)geometryType, entry.Key));
                }
            }
            return geometryIds.Distinct().OrderBy(id => id).ToArray();
        }
        private static int[] GetPartGeometryIds(int[] partIds, FeMesh mesh)
        {
            List<int> geometryIds = new List<int>();
            foreach (int partId in partIds)
            {
                ResolveSelectablePart(mesh, partId);
                geometryIds.Add(FeMesh.GetGeometryId(0, (int)GeometryType.Part, partId));
            }
            return geometryIds.ToArray();
        }
        private static void ValidateDirectIds(int[] ids, FeMesh mesh, string source)
        {
            HashSet<int> visibleIds = new HashSet<int>();
            foreach (var entry in mesh.Parts)
            {
                BasePart part = entry.Value;
                if (!IsSelectable(part)) continue;
                if (source == "nodes" && part.NodeLabels != null) visibleIds.UnionWith(part.NodeLabels);
                else if (source == "elements" && part.Labels != null) visibleIds.UnionWith(part.Labels);
                else if (source == "parts") visibleIds.Add(part.PartId);
            }
            foreach (int id in ids)
            {
                if (!visibleIds.Contains(id))
                    throw new PythonAPIException("The visible " + source.TrimEnd('s') + " ID " + id +
                                                 " does not exist in the selected view.");
            }
        }
        private static BasePart ResolveSelectablePart(FeMesh mesh, int partId)
        {
            BasePart part = mesh.GetPartFromId(partId);
            if (part == null)
                throw new PythonAPIException("Part ID " + partId + " does not exist in the selected view.");
            if (!IsSelectable(part))
                throw new PythonAPIException("Part ID " + partId + " is not visible and active.");
            return part;
        }
        private static bool IsSelectable(BasePart part)
        {
            return part != null && part.Visible && part.Active;
        }
        private static GeometryType GetSurfaceGeometryType(BasePart part)
        {
            if (part.PartType == PartType.Shell || part.PartType == PartType.SolidAsShell)
                return GeometryType.ShellFrontSurface;
            if (part.PartType == PartType.Solid) return GeometryType.SolidSurface;
            throw new PythonAPIException("Part ID " + part.PartId + " does not contain selectable surfaces.");
        }
        private FeMesh ResolveMesh(SelectionAPI selection, string resultName)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            if (selection.view == "results")
            {
                string name = resultName ?? _resolver.GetCurrentResultName();
                if (name == null) throw new PythonAPIException("No current result is selected.");
                FeResults result = _resolver.ResolveResult(name);
                if (resultName == null && !_resolver.IsCurrentResult(name))
                    throw new PythonAPIException("No current result is selected.");
                return result.Mesh;
            }
            if (_selectionGateway == null)
                throw new PythonAPIException("The '" + selection.view +
                                             "' view is not available in this Python API context.");
            return _selectionGateway.GetMesh(selection.view);
        }
        private static void ValidateView(string view)
        {
            if (view != "geometry" && view != "femodel" && view != "results")
                throw new PythonAPIException("The argument 'view' must be 'geometry', 'femodel', or 'results'.");
        }
        private static void ValidateTarget(string target)
        {
            if (target != "nodes" && target != "elements" && target != "surfaces" &&
                target != "edges" && target != "parts" && target != "geometry")
                throw new PythonAPIException("The argument 'target' must be 'nodes', 'elements', 'surfaces', " +
                                             "'edges', 'parts', or 'geometry'.");
        }
        private static void ValidateSource(string source)
        {
            if (source != "nodes" && source != "elements" && source != "vertices" && source != "edges" &&
                source != "surfaces" && source != "parts")
                throw new PythonAPIException("The argument 'source' must be 'nodes', 'elements', 'vertices', " +
                                             "'edges', 'surfaces', or 'parts'.");
        }
        private static void ValidateCombination(string target, string source)
        {
            bool valid = (target == "nodes" && (source == "nodes" || source == "vertices" || source == "edges" ||
                                                source == "surfaces" || source == "parts")) ||
                         (target == "elements" && (source == "elements" || source == "surfaces" ||
                                                   source == "parts")) ||
                         (target == "surfaces" && (source == "surfaces" || source == "parts")) ||
                         (target == "edges" && source == "edges") ||
                         (target == "parts" && (source == "vertices" || source == "edges" ||
                                                source == "surfaces" || source == "parts")) ||
                         (target == "geometry" && (source == "vertices" || source == "edges" ||
                                                   source == "surfaces" || source == "parts"));
            if (!valid)
                throw new PythonAPIException("Selection source '" + source + "' cannot be converted to target '" +
                                             target + "'.");
        }
        private int GetViewId(string view)
        {
            if (_selectionGateway == null)
                throw new PythonAPIException("Selection compilation is not available in this Python API context.");
            return _selectionGateway.GetViewId(view);
        }
        private static vtkSelectItem ParseTarget(string target)
        {
            if (target == "nodes") return vtkSelectItem.Node;
            if (target == "elements") return vtkSelectItem.Element;
            if (target == "surfaces") return vtkSelectItem.Surface;
            if (target == "edges") return vtkSelectItem.GeometryEdge;
            if (target == "parts") return vtkSelectItem.Part;
            if (target == "geometry") return vtkSelectItem.Geometry;
            throw new PythonAPIException("Unsupported selection target '" + target + "'.");
        }
    }
}
