// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python.Model
{
    public class CoordinateSystemsAPI : ModelItemsAPI<CoordinateSystem, CoordinateSystemAPI>
    {
        // Variables
        private readonly CoordinateSystemService _service;


        // Constructors
        internal CoordinateSystemsAPI(CoordinateSystemService service)
            : base("coordinate system")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Creates a coordinate system from three points using command history. The type is " +
                   "Rectangular or Cylindrical.",
                   Arguments = "name: str, origin: list[float], point_on_x: list[float], " +
                               "point_in_xy: list[float], coordinate_system_type: str = None",
                   Returns = "CoordinateSystem",
                   Example = "pmx.model.coordinate_systems.add('CS-1', [0,0,0], [1,0,0], [0,1,0])")]
        public CoordinateSystemAPI add(string name, object origin, object point_on_x, object point_in_xy,
                                       string coordinate_system_type = null)
        {
            _service.Add(name, coordinate_system_type, origin, point_on_x, point_in_xy);
            return this[name];
        }

        [PythonAPI("Creates a coordinate system whose points come from selected mesh points, using command " +
                   "history. Every mode is OnPoint, BetweenTwoPoints or CircleCenter, needing 1, 2 or 3 " +
                   "selected points, and every mode is given together with its selection. A point that is " +
                   "not given keeps the default coordinates.",
                   Arguments = "name: str, origin_created_from: str = None, origin_selection: Selection = None, " +
                               "point_on_x_created_from: str = None, point_on_x_selection: Selection = None, " +
                               "point_in_xy_created_from: str = None, point_in_xy_selection: Selection = None, " +
                               "coordinate_system_type: str = None", Returns = "CoordinateSystem")]
        public CoordinateSystemAPI add_from_selections(string name, string origin_created_from = null,
                                                        SelectionAPI origin_selection = null,
                                                        string point_on_x_created_from = null,
                                                        SelectionAPI point_on_x_selection = null,
                                                        string point_in_xy_created_from = null,
                                                        SelectionAPI point_in_xy_selection = null,
                                                        string coordinate_system_type = null)
        {
            _service.AddFromSelections(name, coordinate_system_type, origin_created_from, origin_selection,
                                       point_on_x_created_from, point_on_x_selection, point_in_xy_created_from,
                                       point_in_xy_selection);
            return this[name];
        }

        [PythonAPI("Replaces an existing coordinate system using command history.",
                   Arguments = "old_name: str, name: str, origin: list[float], point_on_x: list[float], " +
                               "point_in_xy: list[float], coordinate_system_type: str = None",
                   Returns = "CoordinateSystem")]
        public CoordinateSystemAPI replace(string old_name, string name, object origin, object point_on_x,
                                           object point_in_xy, string coordinate_system_type = null)
        {
            _service.Replace(old_name, name, coordinate_system_type, origin, point_on_x, point_in_xy);
            return this[name];
        }

        [PythonAPI("Hides the named coordinate systems using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            _service.Hide(ResolveNames(names, "names"));
        }

        [PythonAPI("Shows the named coordinate systems using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void show(object names)
        {
            _service.Show(ResolveNames(names, "names"));
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override CoordinateSystem GetItem(string name)
        {
            return _service.GetCoordinateSystem(name);
        }
        protected override CoordinateSystemAPI CreateItemAPI(string name)
        {
            return new CoordinateSystemAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(CoordinateSystem item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "CoordinateSystems(count=" + names.Length + ", names=" +
                   new StringListAPI(names).ToString() + ")";
        }
    }
}
