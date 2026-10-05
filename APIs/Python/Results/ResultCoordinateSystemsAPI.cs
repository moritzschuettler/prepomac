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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    // The coordinate systems of a result mesh. A result field transformation is expressed in one of
    // them, so they are part of the result rather than a display convenience.
    public class ResultCoordinateSystemsAPI : BaseAPI, IReadOnlyList<string>
    {
        // Variables
        private readonly MeshDataService _meshService;
        private readonly string _resultName;
        private readonly SelectionService _selectionService;
        private readonly IPythonCommandGateway _commandGateway;
        private readonly ResultResolver _resultResolver;


        // Properties
        // The collection interface is what makes len() and iteration work on the Python side
        public int Count { get { return GetNames().Length; } }


        // Constructors
        internal ResultCoordinateSystemsAPI(MeshDataService meshService, string resultName,
                                            SelectionService selectionService,
                                            IPythonCommandGateway commandGateway, ResultResolver resultResolver)
        {
            _meshService = meshService;
            _resultName = resultName;
            _selectionService = selectionService;
            _commandGateway = commandGateway;
            _resultResolver = resultResolver;
        }


        // Methods
        public int __len__()
        {
            return Count;
        }
        string IReadOnlyList<string>.this[int index]
        {
            get
            {
                string[] names = GetNames();
                if (index < 0) index = names.Length + index;
                if (index < 0 || index >= names.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return names[index];
            }
        }
        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)GetNames()).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        [PythonAPI("Returns the names of the result coordinate systems.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(GetNames());
        }
        [PythonAPI("Returns true if the result mesh contains the result coordinate system. Names are compared " +
                   "without regard to case.", Arguments = "name: str", Returns = "bool")]
        public bool contains(string name)
        {
            return name != null && PythonNameComparer.Contains(GetNames(), name);
        }
        [PythonAPI("Returns the nine stored coordinates of a result coordinate system, as the values or " +
                   "equations they were entered with: the origin, the point on X and the point in XY.",
                   Arguments = "name: str", Returns = "list[str]")]
        public string[] get_definition(string name)
        {
            CoordinateSystem system = Resolve(name);
            //
            return new string[] { system.X1.String, system.Y1.String, system.Z1.String,
                                  system.X2.String, system.Y2.String, system.Z2.String,
                                  system.X3.String, system.Y3.String, system.Z3.String };
        }
        [PythonAPI("Returns the type of a result coordinate system: Rectangular or Cylindrical.",
                   Arguments = "name: str", Returns = "str")]
        public string get_coordinate_system_type(string name)
        {
            return Resolve(name).Type.ToString();
        }
        [PythonAPI("Returns whether the name of a result coordinate system is drawn next to its symbol.",
                   Arguments = "name: str", Returns = "bool")]
        public bool get_name_visible(string name)
        {
            return Resolve(name).NameVisible;
        }
        [PythonAPI("Returns how the three points of a result coordinate system were created: Coordinates, " +
                   "OnPoint, BetweenTwoPoints or CircleCenter, for the origin, the point on X and the " +
                   "point in XY.", Arguments = "name: str", Returns = "list[str]")]
        public string[] get_created_from(string name)
        {
            CoordinateSystem system = Resolve(name);
            //
            return new string[] { system.CenterCreatedFrom.ToString(), system.PointXCreatedFrom.ToString(),
                                  system.PointXYCreatedFrom.ToString() };
        }
        [PythonAPI("Adds a result coordinate system using command history.",
                   Arguments = "name: str, origin: list[float], point_on_x: list[float], " +
                               "point_in_xy: list[float], coordinate_system_type: str = None, " +
                               "name_visible: bool = None",
                   Returns = "None",
                   Example = "pmx.results.current.mesh.coordinate_systems.add('CS-1', [0,0,0], [1,0,0], [0,1,0])")]
        public void add(string name, object origin, object point_on_x, object point_in_xy,
                        string coordinate_system_type = null, object name_visible = null)
        {
            ValidateName(name, null);
            CoordinateSystem system = new CoordinateSystem(name, GetIsTwoDimensional());
            ApplyType(system, coordinate_system_type);
            ApplyPointOrCoordinates(system, null, null, origin, 1, true);
            ApplyPointOrCoordinates(system, null, null, point_on_x, 2, true);
            ApplyPointOrCoordinates(system, null, null, point_in_xy, 3, true);
            ApplyNameVisible(system, name_visible);
            ValidateAxes(system);
            //
            EnsureCommandsAvailable();
            _commandGateway.AddResultCoordinateSystem(system);
        }
        [PythonAPI("Adds a result coordinate system whose points may be selected on the result mesh, using " +
                   "command history. A point is given either as coordinates or as a mode and a selection, " +
                   "where the mode is OnPoint, BetweenTwoPoints or CircleCenter. A point that is not " +
                   "given keeps the default coordinates.",
                   Arguments = "name: str, coordinate_system_type: str = None, origin: list[float] = None, " +
                               "origin_created_from: str = None, origin_selection: Selection = None, " +
                               "point_on_x: list[float] = None, point_on_x_created_from: str = None, " +
                               "point_on_x_selection: Selection = None, point_in_xy: list[float] = None, " +
                               "point_in_xy_created_from: str = None, " +
                               "point_in_xy_selection: Selection = None, name_visible: bool = None",
                   Returns = "None")]
        public void add_from_selections(string name, string coordinate_system_type = null, object origin = null,
                                        string origin_created_from = null, SelectionAPI origin_selection = null,
                                        object point_on_x = null, string point_on_x_created_from = null,
                                        SelectionAPI point_on_x_selection = null, object point_in_xy = null,
                                        string point_in_xy_created_from = null,
                                        SelectionAPI point_in_xy_selection = null, object name_visible = null)
        {
            ValidateName(name, null);
            CoordinateSystem system = new CoordinateSystem(name, GetIsTwoDimensional());
            ApplyType(system, coordinate_system_type);
            ApplyPointOrCoordinates(system, origin_created_from, origin_selection, origin, 1, false);
            ApplyPointOrCoordinates(system, point_on_x_created_from, point_on_x_selection, point_on_x, 2, false);
            ApplyPointOrCoordinates(system, point_in_xy_created_from, point_in_xy_selection, point_in_xy, 3, false);
            ApplyNameVisible(system, name_visible);
            ValidateAxes(system);
            //
            EnsureCommandsAvailable();
            _commandGateway.AddResultCoordinateSystem(system);
        }
        [PythonAPI("Replaces a result coordinate system using command history. The three points are the " +
                   "whole definition and are always given; the name and the type may be left out.",
                   Arguments = "old_name: str, name: str = None, origin: list[float] = None, " +
                               "point_on_x: list[float] = None, point_in_xy: list[float] = None, " +
                               "coordinate_system_type: str = None, name_visible: bool = None",
                   Returns = "None")]
        public void replace(string old_name, string name = null, object origin = null, object point_on_x = null,
                            object point_in_xy = null, string coordinate_system_type = null,
                            object name_visible = null)
        {
            if (origin == null || point_on_x == null || point_in_xy == null)
                throw new PythonAPIException("The arguments 'origin', 'point_on_x' and 'point_in_xy' must all " +
                                             "be given.");
            CoordinateSystem system = CloneForReplace(old_name, name);
            ApplyType(system, coordinate_system_type);
            ApplyPointOrCoordinates(system, null, null, origin, 1, true);
            ApplyPointOrCoordinates(system, null, null, point_on_x, 2, true);
            ApplyPointOrCoordinates(system, null, null, point_in_xy, 3, true);
            ApplyNameVisible(system, name_visible);
            ValidateAxes(system);
            //
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultCoordinateSystem(old_name, system);
        }
        [PythonAPI("Replaces a result coordinate system the way add_from_selections builds one. A result " +
                   "replacement re-specifies the whole definition, so every point takes either " +
                   "coordinates or a mode and a selection.",
                   Arguments = "old_name: str, name: str = None, coordinate_system_type: str = None, " +
                               "origin: list[float] = None, origin_created_from: str = None, " +
                               "origin_selection: Selection = None, point_on_x: list[float] = None, " +
                               "point_on_x_created_from: str = None, point_on_x_selection: Selection = None, " +
                               "point_in_xy: list[float] = None, point_in_xy_created_from: str = None, " +
                               "point_in_xy_selection: Selection = None, name_visible: bool = None",
                   Returns = "None")]
        public void replace_from_selections(string old_name, string name = null,
                                            string coordinate_system_type = null, object origin = null,
                                            string origin_created_from = null,
                                            SelectionAPI origin_selection = null, object point_on_x = null,
                                            string point_on_x_created_from = null,
                                            SelectionAPI point_on_x_selection = null, object point_in_xy = null,
                                            string point_in_xy_created_from = null,
                                            SelectionAPI point_in_xy_selection = null,
                                            object name_visible = null)
        {
            CoordinateSystem system = CloneForReplace(old_name, name);
            ApplyType(system, coordinate_system_type);
            ApplyPointOrCoordinates(system, origin_created_from, origin_selection, origin, 1, true);
            ApplyPointOrCoordinates(system, point_on_x_created_from, point_on_x_selection, point_on_x, 2, true);
            ApplyPointOrCoordinates(system, point_in_xy_created_from, point_in_xy_selection, point_in_xy, 3, true);
            ApplyNameVisible(system, name_visible);
            ValidateAxes(system);
            //
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultCoordinateSystem(old_name, system);
        }
        [PythonAPI("Duplicates result coordinate systems using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void duplicate(object names)
        {
            string[] resolved = ResolveNames(names);
            EnsureCommandsAvailable();
            _commandGateway.DuplicateResultCoordinateSystems(resolved);
        }
        [PythonAPI("Renames a result coordinate system using command history.",
                   Arguments = "old_name: str, new_name: str", Returns = "None")]
        public void rename(string old_name, string new_name)
        {
            CoordinateSystem system = Resolve(old_name);
            PythonNameComparer.ValidateFormat(new_name, "new_name", system.Name);
            if (string.Equals(system.Name, new_name)) return;
            if (!PythonNameComparer.AreSame(new_name, system.Name) && PythonNameComparer.Contains(GetNames(), new_name))
                throw new PythonAPIException("The result coordinate system '" + new_name + "' already exists.");
            //
            EnsureCommandsAvailable();
            _commandGateway.RenameResultItem(system, new_name);
        }
        [PythonAPI("Removes result coordinate systems using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void remove(object names)
        {
            string[] resolved = ResolveNames(names);
            EnsureCommandsAvailable();
            _commandGateway.RemoveResultCoordinateSystems(resolved);
        }
        [PythonAPI("Hides result coordinate systems using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            string[] resolved = ResolveNames(names);
            EnsureCommandsAvailable();
            _commandGateway.HideResultCoordinateSystems(resolved);
        }
        [PythonAPI("Shows result coordinate systems using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void show(object names)
        {
            string[] resolved = ResolveNames(names);
            EnsureCommandsAvailable();
            _commandGateway.ShowResultCoordinateSystems(resolved);
        }
        public override string ToString()
        {
            string[] names = GetNames();
            return "CoordinateSystems(count=" + names.Length + ", names=" +
                   new StringListAPI(names).ToString() + ")";
        }
        //
        private FeResults ResolveResult()
        {
            if (!_resultResolver.IsCurrentResult(_resultName))
                throw new PythonAPIException("The result '" + _resultName + "' is no longer the current one.");
            //
            return _meshService.ResolveResultWithMesh(_resultName);
        }
        private CoordinateSystem Resolve(string name)
        {
            FeResults result = ResolveResult();
            CoordinateSystem system;
            if (result.Mesh == null || !result.Mesh.CoordinateSystems.TryGetValue(name, out system))
                throw new PythonAPIException("The result coordinate system '" + name + "' does not exist.");
            //
            return system;
        }
        private string[] GetNames()
        {
            FeResults result = ResolveResult();
            //
            return result.Mesh == null ? new string[0] : result.Mesh.CoordinateSystems.Keys.ToArray();
        }
        private string[] ResolveNames(object value)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(value, "names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'names' must not be empty.");
            //
            string[] existing = GetNames();
            foreach (string name in names)
            {
                if (!existing.Contains(name))
                    throw new PythonAPIException("The result coordinate system '" + name + "' does not exist.");
            }
            //
            return names;
        }
        private void ValidateName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            if (PythonNameComparer.Contains(GetNames(), name))
                throw new PythonAPIException("The result coordinate system '" + name + "' already exists.");
        }
        private void EnsureCommandsAvailable()
        {
            if (_commandGateway == null)
                throw new PythonAPIException("Result modifications are not available in this Python API context.");
            //
            ResolveResult();
        }
        // A new system lies in the plane of a two dimensional model, the way the dialog creates it from the model space
        private bool GetIsTwoDimensional()
        {
            return _commandGateway != null && _commandGateway.GetIsModelTwoDimensional();
        }
        private CoordinateSystem CloneForReplace(string oldName, string name)
        {
            CoordinateSystem existing = Resolve(oldName);
            string newName = name == null ? oldName : name;
            ValidateName(newName, oldName);
            CoordinateSystem system = existing.DeepClone();
            system.Name = newName;
            //
            return system;
        }
        private static void ApplyType(CoordinateSystem system, string coordinateSystemType)
        {
            if (coordinateSystemType == null) return;
            //
            system.Type = PythonValueConverter.ToEnum<CoordinateSystemTypeEnum>(coordinateSystemType,
                                                                                "coordinate_system_type");
        }
        // The name visibility is part of the stored definition, so a creation carries the same value its
        // replacement can
        private static void ApplyNameVisible(CoordinateSystem system, object nameVisible)
        {
            if (nameVisible == null) return;
            //
            system.NameVisible = PythonValueConverter.ToBool(nameVisible, "name_visible");
        }
        // A replacement re-specifies the whole definition, so applying a point also normalizes how that
        // point was created and drops the creation data the previous mode left behind
        private void ApplyPointOrCoordinates(CoordinateSystem system, string createdFrom, SelectionAPI selection,
                                             object coordinates, int point, bool required)
        {
            string coordinatesArgument = GetCoordinatesArgumentName(point);
            if (coordinates != null)
            {
                if (createdFrom != null || selection != null)
                    throw new PythonAPIException("The point '" + coordinatesArgument + "' must use either " +
                                                 "coordinates or a selection, not both.");
                ApplyCoordinates(system, coordinates, point);
                return;
            }
            if (createdFrom == null && selection == null)
            {
                // A creation keeps the default of a point it is not told about, the way the dialog does;
                // a replacement re-specifies the whole definition, so it needs every point
                if (!required) return;
                throw new PythonAPIException("The point '" + coordinatesArgument + "' must be given as " +
                                             "coordinates or as a mode and a selection.");
            }
            if (createdFrom == null || selection == null)
                throw new PythonAPIException("The mode and the selection of the point '" + coordinatesArgument +
                                             "' must be given together.");
            //
            CsPointCreatedFromEnum mode =
                PythonValueConverter.ToEnum<CsPointCreatedFromEnum>(createdFrom, coordinatesArgument +
                                                                                 "_created_from");
            if (mode == CsPointCreatedFromEnum.Coordinates)
                throw new PythonAPIException("The mode of the point '" + coordinatesArgument + "' must be " +
                                             "OnPoint, BetweenTwoPoints or CircleCenter when a selection is given.");
            //
            SelectionCompilation compilation = CompileResultSelection(selection);
            int expected = mode == CsPointCreatedFromEnum.OnPoint
                           ? 1 : (mode == CsPointCreatedFromEnum.BetweenTwoPoints ? 2 : 3);
            if (compilation.Ids.Length != expected)
                throw new PythonAPIException("The mode " + mode + " needs exactly " + expected +
                                             " selected points, but " + compilation.Ids.Length + " were given.");
            //
            if (point == 1)
            {
                system.CenterCreatedFrom = mode;
                system.CenterCreationData = compilation.Selection;
                system.CenterCreationIds = compilation.Ids;
            }
            else if (point == 2)
            {
                system.PointXCreatedFrom = mode;
                system.PointXCreationData = compilation.Selection;
                system.PointXCreationIds = compilation.Ids;
            }
            else
            {
                system.PointXYCreatedFrom = mode;
                system.PointXYCreationData = compilation.Selection;
                system.PointXYCreationIds = compilation.Ids;
            }
        }
        private SelectionCompilation CompileResultSelection(SelectionAPI selection)
        {
            if (_selectionService == null)
                throw new PythonAPIException("Selections are not available in this Python API context.");
            //
            return _selectionService.CompileForResult(selection, _resultName, "nodes");
        }
        private static string GetCoordinatesArgumentName(int point)
        {
            return point == 1 ? "origin" : (point == 2 ? "point_on_x" : "point_in_xy");
        }
        private static void ApplyCoordinates(CoordinateSystem system, object value, int point)
        {
            string argumentName = GetCoordinatesArgumentName(point);
            EquationString[] equations = PythonValueConverter.ToEquationArray(value, argumentName, 3);
            if (point == 1)
            {
                system.CenterCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                system.X1.Equation = equations[0];
                system.Y1.Equation = equations[1];
                system.Z1.Equation = equations[2];
            }
            else if (point == 2)
            {
                system.PointXCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                system.X2.Equation = equations[0];
                system.Y2.Equation = equations[1];
                system.Z2.Equation = equations[2];
            }
            else
            {
                system.PointXYCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                system.X3.Equation = equations[0];
                system.Y3.Equation = equations[1];
                system.Z3.Equation = equations[2];
            }
        }
        // A point that comes from a selection is resolved on the result mesh before the axes are
        // checked, so a definition the application would reject never reaches the command history
        private void ValidateAxes(CoordinateSystem system)
        {
            CoordinateSystemValidator.ValidateAxes(system, ResolveResult().Mesh);
        }
    }
}
