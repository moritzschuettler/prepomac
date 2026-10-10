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
                               "point_in_xy: list[float], coordinate_system_type: str = None, " +
                               "name_visible: bool = None",
                   Returns = "CoordinateSystem",
                   Example = "pmx.model.coordinate_systems.add('CS-1', [0,0,0], [1,0,0], [0,1,0])")]
        public CoordinateSystemAPI add(object name, object origin, object point_on_x, object point_in_xy,
                                       object coordinate_system_type = null, object name_visible = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string coordinateSystemType =
                PythonValueConverter.ToText(coordinate_system_type, "coordinate_system_type", true);
            _service.Add(nameText, coordinateSystemType, origin, point_on_x, point_in_xy, name_visible);
            return this[nameText];
        }

        [PythonAPI("Creates a coordinate system whose points come from selected mesh points, using command " +
                   "history. Every mode is OnPoint, BetweenTwoPoints or CircleCenter, needing 1, 2 or 3 " +
                   "selected points, and every mode is given together with its selection. A point may " +
                   "instead be given as coordinates, so that one point is picked and another typed. A " +
                   "point that is not given keeps the default coordinates.",
                   Arguments = "name: str, origin_created_from: str = None, origin_selection: Selection = None, " +
                               "point_on_x_created_from: str = None, point_on_x_selection: Selection = None, " +
                               "point_in_xy_created_from: str = None, point_in_xy_selection: Selection = None, " +
                               "origin: list[float] = None, point_on_x: list[float] = None, " +
                               "point_in_xy: list[float] = None, coordinate_system_type: str = None, " +
                               "name_visible: bool = None",
                   Returns = "CoordinateSystem",
                   Example = "pmx.model.coordinate_systems.add_from_selections('CS-1', 'OnPoint', selection, " +
                             "point_on_x=[1, 0, 0], point_in_xy=[0, 1, 0])")]
        public CoordinateSystemAPI add_from_selections(object name, object origin_created_from = null,
                                                       object origin_selection = null,
                                                       object point_on_x_created_from = null,
                                                       object point_on_x_selection = null,
                                                       object point_in_xy_created_from = null,
                                                       object point_in_xy_selection = null,
                                                       object origin = null, object point_on_x = null,
                                                       object point_in_xy = null,
                                                       object coordinate_system_type = null,
                                                       object name_visible = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string originCreatedFrom = PythonValueConverter.ToText(origin_created_from, "origin_created_from", true);
            SelectionAPI originSelection =
                PythonValueConverter.ToApiObject<SelectionAPI>(origin_selection, "origin_selection", true);
            string pointOnXCreatedFrom =
                PythonValueConverter.ToText(point_on_x_created_from, "point_on_x_created_from", true);
            SelectionAPI pointOnXSelection =
                PythonValueConverter.ToApiObject<SelectionAPI>(point_on_x_selection, "point_on_x_selection", true);
            string pointInXyCreatedFrom =
                PythonValueConverter.ToText(point_in_xy_created_from, "point_in_xy_created_from", true);
            SelectionAPI pointInXySelection =
                PythonValueConverter.ToApiObject<SelectionAPI>(point_in_xy_selection, "point_in_xy_selection", true);
            string coordinateSystemType =
                PythonValueConverter.ToText(coordinate_system_type, "coordinate_system_type", true);
            _service.AddFromSelections(nameText, coordinateSystemType, originCreatedFrom, originSelection,
                                       pointOnXCreatedFrom, pointOnXSelection, pointInXyCreatedFrom,
                                       pointInXySelection, origin, point_on_x, point_in_xy, name_visible);
            return this[nameText];
        }

        [PythonAPI("Replaces an existing coordinate system using command history, keeping every value that is " +
                   "not given.",
                   Arguments = "old_name: str, name: str = None, origin: list[float] = None, " +
                               "point_on_x: list[float] = None, point_in_xy: list[float] = None, " +
                               "coordinate_system_type: str = None, name_visible: bool = None",
                   Returns = "CoordinateSystem")]
        public CoordinateSystemAPI replace(object old_name, object name = null, object origin = null, object point_on_x = null,
                                           object point_in_xy = null, object coordinate_system_type = null,
                                           object name_visible = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string coordinateSystemType =
                PythonValueConverter.ToText(coordinate_system_type, "coordinate_system_type", true);
            _service.Replace(oldName, nameText, coordinateSystemType, origin, point_on_x, point_in_xy,
                             name_visible);
            return this[nameText == null ? oldName : nameText];
        }

        [PythonAPI("Replaces a coordinate system while retaining omitted point definitions. A point is " +
                   "given either by its mode and selection or by its coordinates.",
                   Arguments = "old_name: str, name: str = None, origin_created_from: str = None, " +
                               "origin_selection: Selection = None, point_on_x_created_from: str = None, " +
                               "point_on_x_selection: Selection = None, point_in_xy_created_from: str = None, " +
                               "point_in_xy_selection: Selection = None, origin: list[float] = None, " +
                               "point_on_x: list[float] = None, point_in_xy: list[float] = None, " +
                               "coordinate_system_type: str = None, name_visible: bool = None",
                   Returns = "CoordinateSystem")]
        public CoordinateSystemAPI replace_from_selections(object old_name, object name = null,
                                                           object origin_created_from = null,
                                                           object origin_selection = null,
                                                           object point_on_x_created_from = null,
                                                           object point_on_x_selection = null,
                                                           object point_in_xy_created_from = null,
                                                           object point_in_xy_selection = null,
                                                           object origin = null, object point_on_x = null,
                                                           object point_in_xy = null,
                                                           object coordinate_system_type = null,
                                                           object name_visible = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string originCreatedFrom = PythonValueConverter.ToText(origin_created_from, "origin_created_from", true);
            SelectionAPI originSelection =
                PythonValueConverter.ToApiObject<SelectionAPI>(origin_selection, "origin_selection", true);
            string pointOnXCreatedFrom =
                PythonValueConverter.ToText(point_on_x_created_from, "point_on_x_created_from", true);
            SelectionAPI pointOnXSelection =
                PythonValueConverter.ToApiObject<SelectionAPI>(point_on_x_selection, "point_on_x_selection", true);
            string pointInXyCreatedFrom =
                PythonValueConverter.ToText(point_in_xy_created_from, "point_in_xy_created_from", true);
            SelectionAPI pointInXySelection =
                PythonValueConverter.ToApiObject<SelectionAPI>(point_in_xy_selection, "point_in_xy_selection", true);
            string coordinateSystemType =
                PythonValueConverter.ToText(coordinate_system_type, "coordinate_system_type", true);
            _service.ReplaceFromSelections(oldName, nameText, coordinateSystemType, originCreatedFrom,
                                           originSelection, pointOnXCreatedFrom, pointOnXSelection,
                                           pointInXyCreatedFrom, pointInXySelection, origin, point_on_x,
                                           point_in_xy, name_visible);
            return this[nameText == null ? oldName : nameText];
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
