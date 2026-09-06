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
        [PythonAPI("Adds IDs to the selection. Edges and surfaces use dict[part_id, list[item_id]]; " +
                   "nodes, elements, vertices, and parts use list[int].",
                   Arguments = "source: str, ids: list[int] or dict[int, list[int]], operation: str = None, " +
                               "geometry_select_mode: str = 'SelectId'", Returns = "Selection")]
        public SelectionAPI add_by_ids(string source, object ids, string operation = null,
                                       string geometry_select_mode = "SelectId")
        {
            _service.AddByIds(this, source, ids, operation, geometry_select_mode);
            return this;
        }
        [PythonAPI("Adds a pick to the selection. The point is a location in space, and the " +
                   "application selects what lies under it, the same way a mouse pick does. Possible " +
                   "select_by values are Node, Element, Edge, Surface, EdgeAngle, SurfaceAngle, Part, " +
                   "Geometry, GeometryVertex, GeometryEdge, GeometrySurface, GeometryEdgeAngle, " +
                   "GeometrySurfaceAngle and GeometryPart. The angle applies to the angle based modes. " +
                   "The nearest entity is picked, so a point that does not lie on the model still resolves.",
                   Arguments = "point: list[float], select_by: str, angle: float = None, " +
                               "direction: list[float] = None, part_ids: list[int] = None, " +
                               "completely_inside: bool = False, operation: str = None, precision: float = None, " +
                               "part_offsets: list[list[float]] = None", Returns = "Selection",
                   Example = "selection.add_by_pick([0.5, 0.5, 1.0], 'Surface')")]
        public SelectionAPI add_by_pick(object point, string select_by, object angle = null,
                                        object direction = null, object part_ids = null,
                                        object completely_inside = null, string operation = null,
                                        object precision = null, object part_offsets = null)
        {
            _service.AddByPick(this, point, select_by, angle, direction, part_ids, completely_inside,
                               operation, precision, part_offsets);
            return this;
        }

        [PythonAPI("Adds an area pick to the selection. The planes bound the volume that is selected, " +
                   "every row being [ox, oy, oz, nx, ny, nz] of one plane, and a normal points out of " +
                   "the volume. It is what a rubber band records, whose planes are the frustum it swept.",
                   Arguments = "planes: list[list[float]], select_by: str, angle: float = None, " +
                               "part_ids: list[int] = None, completely_inside: bool = False, operation: str = None, " +
                               "part_offsets: list[list[float]] = None",
                   Returns = "Selection")]
        public SelectionAPI add_by_area(object planes, string select_by, object angle = null,
                                        object part_ids = null, object completely_inside = null,
                                        string operation = null, object part_offsets = null)
        {
            _service.AddByArea(this, planes, select_by, angle, part_ids, completely_inside, operation, part_offsets);
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
        public SelectionAPI add_all(string operation = "Add")
        {
            _service.AddAll(this, operation);
            return this;
        }
        [PythonAPI("Resolves this selection against the current mesh without creating a model item.", Returns = "IntList")]
        public IntListAPI get_ids()
        {
            return new IntListAPI(_service.ResolveIds(this));
        }
        [PythonAPI("Returns the source item types used by this selection.", Returns = "StringList")]
        public StringListAPI get_sources()
        {
            return new StringListAPI(_items.Select(item => item.Source).Distinct().ToArray());
        }
        [PythonAPI("Carries the persisted selection-construction options used by the application during editing and replay.",
                   Arguments = "max_geometry_ids: int = None, max_item_ids: int = None, limit_to: str = None, " +
                               "shell_edges_only: bool = None, enable_shell_edge_faces: bool = None", Returns = "Selection")]
        public SelectionAPI configure(object max_geometry_ids = null, object max_item_ids = null, string limit_to = null,
                                      object shell_edges_only = null, object enable_shell_edge_faces = null)
        {
            CaeGlobals.Selection options = _options.DeepCopy(false, false);
            if (max_geometry_ids != null) options.MaxNumberOfGeometryIds = ToLimit(max_geometry_ids, "max_geometry_ids");
            if (max_item_ids != null) options.MaxNumberOfItemIds = ToLimit(max_item_ids, "max_item_ids");
            if (limit_to != null)
            {
                if (limit_to != "None" && limit_to != "FirstPart" && limit_to != "FirstGeometryType" &&
                    limit_to != "FirstMesherType")
                    throw new PythonAPIException("The argument 'limit_to' must be None, FirstPart, " +
                                                 "FirstGeometryType or FirstMesherType.");
                options.LimitSelectionToFirstPart = limit_to == "FirstPart";
                options.LimitSelectionToFirstGeometryType = limit_to == "FirstGeometryType";
                options.LimitSelectionToFirstMesherType = limit_to == "FirstMesherType";
            }
            if (shell_edges_only != null)
                options.LimitSelectionToShellEdges = PythonValueConverter.ToBool(shell_edges_only, "shell_edges_only");
            if (enable_shell_edge_faces != null)
                options.EnableShellEdgeFaceSelection =
                    PythonValueConverter.ToBool(enable_shell_edge_faces, "enable_shell_edge_faces");
            _options = options;
            return this;
        }
        [PythonAPI("Returns a copy of the persisted selection-construction options.", Returns = "dict")]
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
            if (limit != -1 && limit < 1) throw new PythonAPIException("Selection limits must be -1 (unlimited) or positive.");
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
