// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    public class SelectionAPI : BaseAPI
    {
        // Variables
        private readonly SelectionService _service;
        private readonly string _view;
        private readonly string _target;
        private readonly List<SelectionItemAPI> _items;
        private CaeGlobals.Selection _options;


        // Properties
        [PythonAPI("The selection view. Possible values are 'geometry', 'femodel', and 'results'.", Returns = "str")]
        public string view { get { return _view; } }
        [PythonAPI("The selected target item type.", Returns = "str")]
        public string target { get { return _target; } }
        [PythonAPI("The number of source IDs in the selection.", Returns = "int")]
        public int count { get { return _items.Sum(item => item.Count); } }


        // Constructors
        internal SelectionAPI(SelectionService service, string view, string target)
        {
            _service = service;
            _view = view;
            _target = target;
            _items = new List<SelectionItemAPI>();
            _options = new CaeGlobals.Selection();
        }


        // Methods
        [PythonAPI("Adds IDs to the selection. Edges and surfaces use dict[part_id, list[item_id]], an item ID " +
                   "counting from 1 on its part; " +
                   "nodes, elements, vertices, parts, and geometry use list[int]. The 'geometry' source " +
                   "takes recorded geometry IDs unchanged; one naming a part that does not exist, no geometry " +
                   "type, or an item its part does not have is refused naming 'ids' when the selection is used.",
                   Arguments = "source: str, ids: list[int] or dict[int, list[int]], operation: str = None, " +
                               "geometry_select_mode: str = 'SelectId'", Returns = "Selection")]
        public SelectionAPI add_by_ids(object source, object ids, object operation = null,
                                       object geometry_select_mode = null)
        {
            string sourceText = PythonValueConverter.ToText(source, "source", false);
            string operationText = PythonValueConverter.ToText(operation, "operation", true);
            string geometrySelectMode =
                PythonValueConverter.ToTextOrDefault(geometry_select_mode, "SelectId", "geometry_select_mode");
            _service.AddByIds(this, sourceText, ids, operationText, geometrySelectMode);
            return this;
        }
        [PythonAPI("Adds a pick to the selection. The point is a location in space, and the " +
                   "application selects what lies under it, the same way a mouse pick does. Possible " +
                   "select_by values are Node, Element, Edge, Surface, EdgeAngle, SurfaceAngle, Part, " +
                   "Geometry, GeometryVertex, GeometryEdge, GeometrySurface, GeometryEdgeAngle, " +
                   "GeometrySurfaceAngle and GeometryPart. The angle_deg applies to the angle based modes. " +
                   "The nearest entity is picked, so a point that does not lie on the model still resolves.",
                   Arguments = "point: list[float], select_by: str, angle_deg: float = None, " +
                               "direction: list[float] = None, part_ids: list[int] = None, " +
                               "completely_inside: bool = False, operation: str = None, precision: float = None, " +
                               "part_offsets: list[list[float]] = None", Returns = "Selection",
                   Example = "selection.add_by_pick([0.5, 0.5, 1.0], 'Surface')")]
        public SelectionAPI add_by_pick(object point, object select_by, object angle_deg = null,
                                        object direction = null, object part_ids = null,
                                        object completely_inside = null, object operation = null,
                                        object precision = null, object part_offsets = null)
        {
            string selectBy = PythonValueConverter.ToText(select_by, "select_by", false);
            string operationText = PythonValueConverter.ToText(operation, "operation", true);
            _service.AddByPick(this, point, selectBy, angle_deg, direction, part_ids, completely_inside,
                               operationText, precision, part_offsets);
            return this;
        }

        [PythonAPI("Adds an area pick to the selection. The planes bound the volume that is selected, " +
                   "every row being [ox, oy, oz, nx, ny, nz] of one plane, and a normal points out of " +
                   "the volume. It is what a rubber band records, whose planes are the frustum it swept.",
                   Arguments = "planes: list[list[float]], select_by: str, angle_deg: float = None, " +
                               "part_ids: list[int] = None, completely_inside: bool = False, operation: str = None, " +
                               "part_offsets: list[list[float]] = None",
                   Returns = "Selection")]
        public SelectionAPI add_by_area(object planes, object select_by, object angle_deg = null,
                                        object part_ids = null, object completely_inside = null,
                                        object operation = null, object part_offsets = null)
        {
            string selectBy = PythonValueConverter.ToText(select_by, "select_by", false);
            string operationText = PythonValueConverter.ToText(operation, "operation", true);
            _service.AddByArea(this, planes, selectBy, angle_deg, part_ids, completely_inside, operationText,
                               part_offsets);
            return this;
        }
        [PythonAPI("Adds an axis aligned box to the selection, which is the six plane form of add_by_area. " +
                   "The box is given by its two opposite corners.",
                   Arguments = "corner1: list[float], corner2: list[float], select_by: str, " +
                               "part_ids: list[int] = None, completely_inside: bool = False, operation: str = None, " +
                               "part_offsets: list[list[float]] = None",
                   Returns = "Selection",
                   Example = "selection.add_by_box([0, 0, 0.9], [1, 1, 1.1], 'Node', operation='Subtract')")]
        public SelectionAPI add_by_box(object corner1, object corner2, object select_by, object part_ids = null,
                                       object completely_inside = null, object operation = null,
                                       object part_offsets = null)
        {
            string selectBy = PythonValueConverter.ToText(select_by, "select_by", false);
            string operationText = PythonValueConverter.ToText(operation, "operation", true);
            _service.AddByBox(this, corner1, corner2, selectBy, part_ids, completely_inside, operationText,
                              part_offsets);
            return this;
        }

        [PythonAPI("Inverts what the selection has selected so far. Everything of the target type that " +
                   "is not selected becomes selected, and the other way round.", Returns = "Selection")]
        public SelectionAPI invert()
        {
            _service.AddInvert(this);
            return this;
        }

        [PythonAPI("Adds every visible active node, element or surface. Operation is None, Add, Subtract or Intersect.",
                   Arguments = "operation: str = 'Add'", Returns = "Selection")]
        public SelectionAPI add_all(object operation = null)
        {
            string operationText = PythonValueConverter.ToTextOrDefault(operation, "Add", "operation");
            _service.AddAll(this, operationText);
            return this;
        }
        [PythonAPI("Resolves this selection against the current mesh without creating a model item.", Returns = "IntList")]
        public IntListAPI get_ids()
        {
            return new IntListAPI(_service.ResolveIds(this));
        }
        [PythonAPI("Resolves this selection and reports what it found - the count, the IDs, the parts, the " +
                   "bounding box and the centroid - without creating a model item and without recording " +
                   "anything. A count of zero and a centroid in the wrong place are how a wrong pick is " +
                   "caught before it becomes a boundary condition.", Returns = "SelectionPreview",
                   Example = "pmx.create.selection_by_pick('femodel', 'nodes', [0, 0, 1], 'Surface').preview()")]
        public SelectionPreviewAPI preview()
        {
            return _service.Preview(this);
        }
        [PythonAPI("Returns the source item types used by this selection.", Returns = "StringList")]
        public StringListAPI get_sources()
        {
            return new StringListAPI(_items.Select(item => item.Source).Distinct().ToArray());
        }
        [PythonAPI("Sets the persisted selection-construction options the application applies to every pick of " +
                   "this selection, during editing and replay, and returns the selection. max_geometry_ids and " +
                   "max_item_ids cap the number of geometry ids and of items the selection holds: a pick that " +
                   "would exceed the cap is dropped, a cap of 1 makes each pick replace the previous one, and " +
                   "-1, the default of a new selection, means unlimited. limit_to keeps every pick on the parts " +
                   "of the first one: None (the default) takes any part, FirstPart only the first part, " +
                   "FirstGeometryType only parts of its part type, such as solid or shell, and FirstMesherType " +
                   "only parts meshed by its mesher. shell_edges_only limits a pick to shell edge faces and " +
                   "enable_shell_edge_faces lets a pick take them; both are off by default. A value that is not " +
                   "given keeps the current option.",
                   Arguments = "max_geometry_ids: int = None, max_item_ids: int = None, limit_to: str = None, " +
                               "shell_edges_only: bool = None, enable_shell_edge_faces: bool = None", Returns = "Selection")]
        public SelectionAPI configure(object max_geometry_ids = null, object max_item_ids = null, object limit_to = null,
                                      object shell_edges_only = null, object enable_shell_edge_faces = null)
        {
            string limitToText = PythonValueConverter.ToText(limit_to, "limit_to", true);
            CaeGlobals.Selection options = _options.DeepCopy(false, false);
            if (max_geometry_ids != null) options.MaxNumberOfGeometryIds = ToLimit(max_geometry_ids, "max_geometry_ids");
            if (max_item_ids != null) options.MaxNumberOfItemIds = ToLimit(max_item_ids, "max_item_ids");
            if (limitToText != null)
            {
                SelectionLimitToEnum limitTo =
                    PythonValueConverter.ToEnum<SelectionLimitToEnum>(limitToText, "limit_to");
                options.LimitSelectionToFirstPart = limitTo == SelectionLimitToEnum.FirstPart;
                options.LimitSelectionToFirstGeometryType = limitTo == SelectionLimitToEnum.FirstGeometryType;
                options.LimitSelectionToFirstMesherType = limitTo == SelectionLimitToEnum.FirstMesherType;
            }
            if (shell_edges_only != null)
                options.LimitSelectionToShellEdges = PythonValueConverter.ToBool(shell_edges_only, "shell_edges_only");
            if (enable_shell_edge_faces != null)
                options.EnableShellEdgeFaceSelection =
                    PythonValueConverter.ToBool(enable_shell_edge_faces, "enable_shell_edge_faces");
            _options = options;
            return this;
        }
        [PythonAPI("Returns a copy of the persisted selection-construction options.", Returns = "dict[str, Any]")]
        public Dictionary<string, object> get_options()
        {
            return new Dictionary<string, object>
            {
                { "max_geometry_ids", _options.MaxNumberOfGeometryIds },
                { "max_item_ids", _options.MaxNumberOfItemIds },
                { "limit_to", _options.LimitSelectionToFirstPart ? "FirstPart" :
                              (_options.LimitSelectionToFirstGeometryType ? "FirstGeometryType" :
                              (_options.LimitSelectionToFirstMesherType ? "FirstMesherType" : "None")) },
                { "shell_edges_only", _options.LimitSelectionToShellEdges },
                { "enable_shell_edge_faces", _options.EnableShellEdgeFaceSelection }
            };
        }
        private static int ToLimit(object value, string name)
        {
            int limit = PythonValueConverter.ToInt(value, name);
            if (limit != -1 && limit < 1)
                throw new PythonAPIException("The argument '" + name + "' must be -1 (unlimited) or positive.");
            return limit;
        }
        internal CaeGlobals.Selection CreateSelection()
        {
            return _options.DeepCopy(false, false);
        }
        internal void AddItem(SelectionItemAPI item)
        {
            _items.Add(item);
        }
        internal SelectionItemAPI[] GetItems()
        {
            return _items.ToArray();
        }
        internal SelectionAPI DeepCopy()
        {
            SelectionAPI copy = new SelectionAPI(_service, _view, _target);
            copy._options = _options.DeepCopy(false, false);
            foreach (SelectionItemAPI item in _items) copy.AddItem(item.DeepCopy());
            return copy;
        }
        public override string ToString()
        {
            StringListAPI sources = new StringListAPI(_items.Select(item => item.Source).Distinct().ToArray());
            return "Selection(view=" + StringListAPI.ToDisplayString(_view) +
                   ", target=" + StringListAPI.ToDisplayString(_target) +
                   ", sources=" + sources.ToString() + ", count=" + count + ")";
        }
    }
}
