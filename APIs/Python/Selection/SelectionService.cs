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
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    internal class SelectionService
    {
        // Variables
        private readonly IMeshResolver _meshResolver;
        private readonly IPythonSelectionGateway _selectionGateway;


        // Constructors
        public SelectionService(IMeshResolver meshResolver, IPythonSelectionGateway selectionGateway)
        {
            _meshResolver = meshResolver;
            _selectionGateway = selectionGateway;
        }


        // Methods
        public SelectionAPI CreateByIds(string view, string target, string source, object ids,
                                        string geometrySelectMode = "SelectId")
        {
            ValidateView(view);
            ValidateTarget(target);
            SelectionAPI selection = new SelectionAPI(this, view, target);
            AddByIds(selection, source, ids, null, geometrySelectMode);
            return selection;
        }
        public SelectionAPI CreateByPick(string view, string target, object point, string selectBy,
                                         object angle, object direction, object partIds,
                                         object completelyInside, object precision = null, object partOffsets = null)
        {
            ValidateView(view);
            ValidateTarget(target);
            SelectionAPI selection = new SelectionAPI(this, view, target);
            AddByPick(selection, point, selectBy, angle, direction, partIds, completelyInside, null, precision, partOffsets);
            return selection;
        }
        public void AddByPick(SelectionAPI selection, object point, string selectBy, object angle,
                              object direction, object partIds, object completelyInside, string operation = null,
                              object precision = null, object partOffsets = null)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            //
            vtkSelectBy mode = ValidatePickMode(selectBy, angle);
            double[] pickedPoint = ToVector(point, "point");
            double[] pickDirection = direction == null ? null : ToVector(direction, "direction");
            int[] pickPartIds = partIds == null ? null : PythonIntListConverter.ToIntArray(partIds, "part_ids");
            double pickAngle = angle == null ? 0 : PythonValueConverter.ToFiniteDouble(angle, "angle");
            bool inside = completelyInside != null &&
                          PythonValueConverter.ToBool(completelyInside, "completely_inside");
            //
            selection.AddItem(new SelectionItemAPI(new SelectionPickAPI(pickedPoint, pickDirection, null,
                                                                         mode, pickAngle, pickPartIds,
                                                                         inside, ValidatePrecision(precision),
                                                                         ToPartOffsets(partOffsets, pickPartIds, true)),
                                                     ParseOperation(operation)));
        }
        public SelectionAPI CreateByArea(string view, string target, object planes, string selectBy,
                                         object angle, object partIds, object completelyInside, object partOffsets = null)
        {
            ValidateView(view);
            ValidateTarget(target);
            SelectionAPI selection = new SelectionAPI(this, view, target);
            AddByArea(selection, planes, selectBy, angle, partIds, completelyInside, null, partOffsets);
            return selection;
        }
        public void AddByArea(SelectionAPI selection, object planes, string selectBy, object angle,
                              object partIds, object completelyInside, string operation = null, object partOffsets = null)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            //
            vtkSelectBy mode = ValidatePickMode(selectBy, angle);
            double[][] planeParameters = PythonTableConverter.ToTable(planes, "planes", 6);
            // A single plane bounds nothing, so it would select either everything or nothing
            if (planeParameters.Length != 6)
                throw new PythonAPIException("The argument 'planes' must contain exactly 6 planes to " +
                                             "match the application frustum format.");
            foreach (double[] plane in planeParameters)
            {
                foreach (double value in plane) PythonValueConverter.ToFiniteDouble(value, "planes");
                if (plane[3] == 0 && plane[4] == 0 && plane[5] == 0)
                    throw new PythonAPIException("Plane normals must not be zero.");
            }
            //
            int[] pickPartIds = partIds == null ? null : PythonIntListConverter.ToIntArray(partIds, "part_ids");
            double pickAngle = angle == null ? 0 : PythonValueConverter.ToFiniteDouble(angle, "angle");
            bool inside = completelyInside != null &&
                          PythonValueConverter.ToBool(completelyInside, "completely_inside");
            //
            selection.AddItem(new SelectionItemAPI(new SelectionPickAPI(null, null, planeParameters, mode,
                                                                         pickAngle, pickPartIds, inside, -1,
                                                                         ToPartOffsets(partOffsets, pickPartIds, false)),
                                                     ParseOperation(operation)));
        }
        public SelectionAPI CreateByBox(string view, string target, object corner1, object corner2,
                                        string selectBy, object partIds, object completelyInside)
        {
            double[] first = ToVector(corner1, "corner1");
            double[] second = ToVector(corner2, "corner2");
            double[][] planes = new double[6][];
            for (int axis = 0; axis < 3; axis++)
            {
                double low = Math.Min(first[axis], second[axis]);
                double high = Math.Max(first[axis], second[axis]);
                if (high - low <= 0)
                    throw new PythonAPIException("The two corners must differ along every axis.");
                // A normal points out of the box, which is the side the application treats as outside
                double[] lowPlane = new double[6];
                double[] highPlane = new double[6];
                lowPlane[axis] = low;
                lowPlane[3 + axis] = -1;
                highPlane[axis] = high;
                highPlane[3 + axis] = 1;
                planes[2 * axis] = lowPlane;
                planes[2 * axis + 1] = highPlane;
            }
            //
            return CreateByArea(view, target, planes, selectBy, null, partIds, completelyInside);
        }
        public void AddInvert(SelectionAPI selection)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            ValidateAllTarget(selection);
            if (selection.count == 0)
                throw new PythonAPIException("An empty selection cannot be inverted, because there is " +
                                             "nothing to invert.");
            //
            selection.AddItem(new SelectionItemAPI());
        }
        public SelectionAPI CreateAll(string view, string target)
        {
            ValidateView(view);
            ValidateTarget(target);
            SelectionAPI selection = new SelectionAPI(this, view, target);
            AddAll(selection, "None");
            return selection;
        }
        public void AddAll(SelectionAPI selection, string operation)
        {
            ValidateAllTarget(selection);
            selection.AddItem(new SelectionItemAPI(ParseOperation(operation) ?? vtkSelectOperation.Add));
        }
        public int[] ResolveIds(SelectionAPI selection)
        {
            return Compile(selection, ResolveMesh(selection, null), selection.view, selection.target, true).Ids;
        }
        private static void ValidateAllTarget(SelectionAPI selection)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            if (selection.target != "nodes" && selection.target != "elements" && selection.target != "surfaces")
                throw new PythonAPIException("Select all and invert support nodes, elements and surfaces only.");
        }
        private static vtkSelectOperation? ParseOperation(string operation)
        {
            if (operation == null) return null;
            vtkSelectOperation parsed = PythonValueConverter.ToEnum<vtkSelectOperation>(operation, "operation");
            if (parsed == vtkSelectOperation.Invert)
                throw new PythonAPIException("Use invert() to invert a selection.");
            return parsed;
        }
        private static double ValidatePrecision(object precision)
        {
            if (precision == null) return -1;
            double value = PythonValueConverter.ToFiniteDouble(precision, "precision");
            if (value <= 0) throw new PythonAPIException("The argument 'precision' must be greater than zero.");
            return value;
        }
        private static double[][] ToPartOffsets(object offsets, int[] partIds, bool pointPick)
        {
            if (offsets == null) return null;
            if (partIds == null || (pointPick && partIds.Length != 1))
                throw new PythonAPIException("Part offsets require explicit part_ids; a point pick requires exactly one part.");
            // Rows carry their part ID so sorting the selection IDs cannot reorder their offsets.
            double[][] rows = PythonTableConverter.ToTable(offsets, "part_offsets", 4);
            Dictionary<int, double[]> byId = new Dictionary<int, double[]>();
            foreach (double[] row in rows)
            {
                int id = PythonValueConverter.ToInt(row[0], "part_offsets part ID");
                if (!partIds.Contains(id) || byId.ContainsKey(id))
                    throw new PythonAPIException("Each part_offsets row must name a different selected part ID.");
                double[] offset = new double[] { row[1], row[2], row[3] };
                foreach (double value in offset) PythonValueConverter.ToFiniteDouble(value, "part_offsets");
                byId.Add(id, offset);
            }
            if (byId.Count != partIds.Length)
                throw new PythonAPIException("Provide one [part_id, dx, dy, dz] row for every selected part.");
            return partIds.Select(id => byId[id]).ToArray();
        }
        private static vtkSelectBy ValidatePickMode(string selectBy, object angle)
        {
            vtkSelectBy mode = PythonValueConverter.ToEnum<vtkSelectBy>(selectBy, "select_by");
            if (mode == vtkSelectBy.Off || mode == vtkSelectBy.Default || mode == vtkSelectBy.Id ||
                mode == vtkSelectBy.Widget)
                throw new PythonAPIException("The select_by value " + mode + " cannot be used for a pick. " +
                                             "Use one of Node, Element, Edge, Surface, EdgeAngle, " +
                                             "SurfaceAngle, Part, Geometry, GeometryVertex, GeometryEdge, " +
                                             "GeometrySurface, GeometryEdgeAngle, GeometrySurfaceAngle or " +
                                             "GeometryPart.");
            // An angle based mode selects everything up to that angle, so it cannot do without one
            bool needsAngle = mode == vtkSelectBy.EdgeAngle || mode == vtkSelectBy.SurfaceAngle ||
                              mode == vtkSelectBy.GeometryEdgeAngle ||
                              mode == vtkSelectBy.GeometrySurfaceAngle;
            if (needsAngle && angle == null)
                throw new PythonAPIException("The select_by value " + mode + " needs an angle.");
            if (angle != null)
            {
                double value = PythonValueConverter.ToFiniteDouble(angle, "angle");
                if (!needsAngle || value < 0 || value > 180)
                    throw new PythonAPIException("An angle in [0, 180] is allowed only for an angle based mode.");
            }
            //
            return mode;
        }
        private static int[] GetAllPartIds(FeMesh mesh)
        {
            List<int> partIds = new List<int>();
            foreach (var entry in mesh.Parts)
                if (IsSelectable(entry.Value)) partIds.Add(entry.Value.PartId);
            //
            return partIds.ToArray();
        }
        private static double[] ToVector(object value, string argumentName)
        {
            double[] vector = PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (vector == null || vector.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            foreach (double component in vector) PythonValueConverter.ToFiniteDouble(component, argumentName);
            //
            return vector;
        }
        public void AddByIds(SelectionAPI selection, string source, object ids, string operation = null,
                             string geometrySelectMode = "SelectId")
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            ValidateSource(source);
            ValidateCombination(selection.target, source);
            GeometrySelectModeEnum mode = PythonValueConverter.ToEnum<GeometrySelectModeEnum>(geometrySelectMode,
                                                                                             "geometry_select_mode");
            if (mode != GeometrySelectModeEnum.SelectId && (source == "nodes" || source == "elements"))
                throw new PythonAPIException("The geometry_select_mode applies only to geometry source IDs.");
            if (source == "edges" || source == "surfaces")
            {
                SortedDictionary<int, int[]> itemIds =
                    PythonIntListMapConverter.ToIntListMap(ids, "ids");
                selection.AddItem(new SelectionItemAPI(source, itemIds, ParseOperation(operation), mode));
            }
            else
            {
                int[] itemIds = PythonIntListConverter.ToIntArray(ids, "ids");
                selection.AddItem(new SelectionItemAPI(source, itemIds, ParseOperation(operation), mode));
            }
        }
        public void Highlight(SelectionAPI selection)
        {
            if (_selectionGateway == null)
                throw new PythonAPIException("Selection highlighting is not available in this Python API context.");
            FeMesh mesh = ResolveMesh(selection, null);
            SelectionCompilation compilation = Compile(selection, mesh, null, (string)null);
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
        public SelectionCompilation CompileForView(SelectionAPI selection, string view, string target)
        {
            return CompileForView(selection, view, new string[] { target });
        }
        // Some model items accept a selection of more than one target, because the dialog does. A solid
        // section is the case this exists for: its dialog picks parts, and the controller turns a part
        // selection into an element set that follows those parts.
        public SelectionCompilation CompileForView(SelectionAPI selection, string view, string[] targets)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            FeMesh mesh = ResolveMesh(selection, null);
            return Compile(selection, mesh, view, targets);
        }
        private SelectionCompilation Compile(SelectionAPI selection, FeMesh mesh, string requiredView,
                                             string requiredTarget, bool allowEmpty = false)
        {
            return Compile(selection, mesh, requiredView,
                           requiredTarget == null ? null : new string[] { requiredTarget }, allowEmpty);
        }
        private SelectionCompilation Compile(SelectionAPI selection, FeMesh mesh, string requiredView,
                                             string[] requiredTargets, bool allowEmpty = false)
        {
            if (selection == null) throw new PythonAPIException("The selection must not be None.");
            if (requiredView != null && selection.view != requiredView)
                throw new PythonAPIException("The selection belongs to the '" + selection.view +
                                             "' view, but this operation requires a '" + requiredView +
                                             "' selection.");
            if (requiredTargets != null && !requiredTargets.Contains(selection.target))
                throw new PythonAPIException("The selection target is '" + selection.target +
                                             "', but this operation requires target '" +
                                             string.Join("' or '", requiredTargets) + "'.");
            if (mesh == null) throw new PythonAPIException("The '" + selection.view + "' view does not contain a mesh.");
            //
            CaeGlobals.Selection compiled = selection.CreateSelection();
            compiled.CurrentView = GetViewId(selection.view);
            compiled.SelectItem = ParseTarget(selection.target);
            HashSet<int> selectedIds = new HashSet<int>();
            bool needsApplication = false;
            //
            foreach (SelectionItemAPI item in selection.GetItems())
            {
                vtkSelectOperation operation = item.Operation ?? (compiled.Nodes.Count == 0
                                               ? vtkSelectOperation.None : vtkSelectOperation.Add);
                if (item.SelectAll)
                {
                    compiled.Add(new SelectionNodeIds(operation, true, new int[0]));
                    needsApplication = true;
                    continue;
                }
                // A pick names a point in space, so it is resolved by the application rather than here
                if (item.IsInvert)
                {
                    compiled.Add(new SelectionNodeInvert());
                    needsApplication = true;
                    continue;
                }
                if (item.IsPick)
                {
                    int[] partIds = item.Pick.PartIds ?? GetAllPartIds(mesh);
                    if (partIds.Length == 0) throw new PythonAPIException("No visible active parts are available for picking.");
                    foreach (int partId in partIds) ResolveSelectablePart(mesh, partId);
                    compiled.Add(item.Pick.CreateNode(operation, partIds));
                    needsApplication = true;
                    continue;
                }
                //
                bool geometryBased;
                int[] itemIds = CompileItem(item, selection.target, mesh, out geometryBased);
                if (geometryBased)
                {
                    compiled.Add(new SelectionNodeIds(operation, false, itemIds, true,
                                                      item.GeometryMode));
                    itemIds = mesh.GetIdsFromGeometryIds(itemIds, compiled.SelectItem, true);
                }
                else
                {
                    compiled.Add(new SelectionNodeIds(operation, false, itemIds));

                }
                if (operation == vtkSelectOperation.None) selectedIds.Clear();
                if (operation == vtkSelectOperation.Subtract) selectedIds.ExceptWith(itemIds);
                else if (operation == vtkSelectOperation.Intersect) selectedIds.IntersectWith(itemIds);
                else selectedIds.UnionWith(itemIds);
            }
            //
            int[] ids;
            if (needsApplication)
            {
                if (_selectionGateway == null)
                    throw new PythonAPIException("A picked or inverted selection is resolved by the " +
                                                 "application, which is not available in this Python API " +
                                                 "context.");
                ids = _selectionGateway.ResolveSelectionIds(compiled);
            }
            else ids = selectedIds.OrderBy(id => id).ToArray();
            if (ids.Length == 0 && !allowEmpty)
                throw new PythonAPIException("The selection does not resolve to any visible and active items in the '" +
                                             selection.view + "' view.");
            return new SelectionCompilation(compiled, ids);
        }
        private int[] CompileItem(SelectionItemAPI item, string target, FeMesh mesh, out bool geometryBased)
        {
            // Geometry IDs are what the application records for a geometry pick made in SelectId mode, so
            // they are passed through unchanged rather than built from per part item IDs
            if (item.Source == "geometry")
            {
                geometryBased = true;
                return item.Ids;
            }
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
            return _meshResolver.GetMesh(selection.view, resultName);
        }
        private static void ValidateView(string view)
        {
            if (view != "geometry" && view != "femodel" && view != "results")
                throw new PythonAPIException("The argument 'view' must be 'geometry', 'femodel', or 'results'.");
        }
        private static void ValidateTarget(string target)
        {
            if (target != "nodes" && target != "elements" && target != "surfaces" && target != "edges" &&
                target != "parts" && target != "geometry" && target != "geometry_surfaces")
                throw new PythonAPIException("The argument 'target' must be 'nodes', 'elements', 'surfaces', " +
                                             "'edges', 'parts', 'geometry', or 'geometry_surfaces'.");
        }
        private static void ValidateSource(string source)
        {
            if (source != "nodes" && source != "elements" && source != "vertices" && source != "edges" &&
                source != "surfaces" && source != "parts" && source != "geometry")
                throw new PythonAPIException("The argument 'source' must be 'nodes', 'elements', 'vertices', " +
                                             "'edges', 'surfaces', 'parts', or 'geometry'.");
        }
        private static void ValidateCombination(string target, string source)
        {
            // Geometry IDs already name the topology, so the application converts them to any target
            if (source == "geometry") return;
            //
            bool valid = (target == "nodes" && (source == "nodes" || source == "vertices" || source == "edges" ||
                                                source == "surfaces" || source == "parts")) ||
                         (target == "elements" && (source == "elements" || source == "surfaces" ||
                                                   source == "parts")) ||
                         (target == "surfaces" && (source == "surfaces" || source == "parts")) ||
                         (target == "edges" && source == "edges") ||
                         (target == "parts" && (source == "vertices" || source == "edges" ||
                                                source == "surfaces" || source == "parts")) ||
                         (target == "geometry" && (source == "vertices" || source == "edges" ||
                                                   source == "surfaces" || source == "parts")) ||
                         // A geometry surface is the face itself, so only faces name one
                         (target == "geometry_surfaces" && source == "surfaces");
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
            if (target == "geometry_surfaces") return vtkSelectItem.GeometrySurface;
            throw new PythonAPIException("Unsupported selection target '" + target + "'.");
        }
    }
}
