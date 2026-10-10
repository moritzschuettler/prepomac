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
    public class ResultCoordinateSystemsAPI : BaseAPI, IReadOnlyList<ResultCoordinateSystemAPI>
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
        public ResultCoordinateSystemAPI __getitem__(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            return this[nameText];
        }
        public ResultCoordinateSystemAPI this[string name]
        {
            get { return new ResultCoordinateSystemAPI(ResolveCoordinateSystem(name).Name, this); }
        }
        // Explicit, so that the Python side sees a single indexer and resolves collection[name] without
        // an ambiguous overload
        ResultCoordinateSystemAPI IReadOnlyList<ResultCoordinateSystemAPI>.this[int index]
        {
            get
            {
                string[] names = GetNames();
                if (index < 0) index = names.Length + index;
                if (index < 0 || index >= names.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[names[index]];
            }
        }
        public IEnumerator<ResultCoordinateSystemAPI> GetEnumerator()
        {
            foreach (string name in GetNames()) yield return this[name];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool __contains__(object name)
        {
            return contains(name);
        }
        [PythonAPI("Returns the names of the result coordinate systems.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(GetNames());
        }
        [PythonAPI("Returns true if the result mesh contains the result coordinate system. Names are compared " +
                   "without regard to case.", Arguments = "name: str", Returns = "bool")]
        public bool contains(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", true);
            return nameText != null && PythonNameComparer.Contains(GetNames(), nameText);
        }
        [PythonAPI("Returns the result coordinate system with the specified name.", Arguments = "name: str",
                   Returns = "CoordinateSystem")]
        public ResultCoordinateSystemAPI get(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            return this[nameText];
        }
        [PythonAPI("Returns the first result coordinate system.", Returns = "CoordinateSystem")]
        public ResultCoordinateSystemAPI first()
        {
            string[] names = GetNames();
            if (names.Length == 0) throw new PythonAPIException("The coordinate systems collection is empty.");
            return this[names[0]];
        }
        [PythonAPI("Returns the last result coordinate system.", Returns = "CoordinateSystem")]
        public ResultCoordinateSystemAPI last()
        {
            string[] names = GetNames();
            if (names.Length == 0) throw new PythonAPIException("The coordinate systems collection is empty.");
            return this[names[names.Length - 1]];
        }
        [PythonAPI("Adds a result coordinate system using command history.",
                   Arguments = "name: str, origin: list[float], point_on_x: list[float], " +
                               "point_in_xy: list[float], coordinate_system_type: str = None, " +
                               "name_visible: bool = None",
                   Returns = "CoordinateSystem",
                   Example = "pmx.results.current.mesh.coordinate_systems.add('CS-1', [0,0,0], [1,0,0], [0,1,0])")]
        public ResultCoordinateSystemAPI add(object name, object origin, object point_on_x, object point_in_xy,
                                             object coordinate_system_type = null, object name_visible = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string coordinateSystemType =
                PythonValueConverter.ToText(coordinate_system_type, "coordinate_system_type", true);
            ValidateName(nameText, null);
            CoordinateSystem system = new CoordinateSystem(nameText, GetIsTwoDimensional());
            ApplyType(system, coordinateSystemType);
            ApplyPointOrCoordinates(system, null, null, origin, 1, true);
            ApplyPointOrCoordinates(system, null, null, point_on_x, 2, true);
            ApplyPointOrCoordinates(system, null, null, point_in_xy, 3, true);
            ApplyNameVisible(system, name_visible);
            ValidateAxes(system);
            //
            EnsureCommandsAvailable();
            _commandGateway.AddResultCoordinateSystem(system);
            return this[nameText];
        }
        [PythonAPI("Adds a result coordinate system whose points may be selected on the result mesh, using " +
                   "command history. A point is given either as coordinates or as a mode and a selection, " +
                   "where the mode is OnPoint, BetweenTwoPoints or CircleCenter. A point that is not " +
                   "given keeps the default coordinates.",
                   Arguments = "name: str, origin_created_from: str = None, origin_selection: Selection = None, " +
                               "point_on_x_created_from: str = None, point_on_x_selection: Selection = None, " +
                               "point_in_xy_created_from: str = None, point_in_xy_selection: Selection = None, " +
                               "origin: list[float] = None, point_on_x: list[float] = None, " +
                               "point_in_xy: list[float] = None, coordinate_system_type: str = None, " +
                               "name_visible: bool = None",
                   Returns = "CoordinateSystem")]
        public ResultCoordinateSystemAPI add_from_selections(object name, object origin_created_from = null,
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
            ValidateName(nameText, null);
            CoordinateSystem system = new CoordinateSystem(nameText, GetIsTwoDimensional());
            ApplyType(system, coordinateSystemType);
            ApplyPointOrCoordinates(system, originCreatedFrom, originSelection, origin, 1, false);
            ApplyPointOrCoordinates(system, pointOnXCreatedFrom, pointOnXSelection, point_on_x, 2, false);
            ApplyPointOrCoordinates(system, pointInXyCreatedFrom, pointInXySelection, point_in_xy, 3, false);
            ApplyNameVisible(system, name_visible);
            ValidateAxes(system);
            //
            EnsureCommandsAvailable();
            _commandGateway.AddResultCoordinateSystem(system);
            return this[nameText];
        }
        [PythonAPI("Replaces a result coordinate system using command history. The three points are the " +
                   "whole definition and are always given; the name and the type may be left out.",
                   Arguments = "old_name: str, name: str = None, origin: list[float] = None, " +
                               "point_on_x: list[float] = None, point_in_xy: list[float] = None, " +
                               "coordinate_system_type: str = None, name_visible: bool = None",
                   Returns = "CoordinateSystem")]
        public ResultCoordinateSystemAPI replace(object old_name, object name = null, object origin = null,
                                                 object point_on_x = null, object point_in_xy = null,
                                                 object coordinate_system_type = null, object name_visible = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string coordinateSystemType =
                PythonValueConverter.ToText(coordinate_system_type, "coordinate_system_type", true);
            if (origin == null || point_on_x == null || point_in_xy == null)
                throw new PythonAPIException("The arguments 'origin', 'point_on_x' and 'point_in_xy' must all " +
                                             "be given.");
            CoordinateSystem system = CloneForReplace(oldName, nameText);
            ApplyType(system, coordinateSystemType);
            ApplyPointOrCoordinates(system, null, null, origin, 1, true);
            ApplyPointOrCoordinates(system, null, null, point_on_x, 2, true);
            ApplyPointOrCoordinates(system, null, null, point_in_xy, 3, true);
            ApplyNameVisible(system, name_visible);
            ValidateAxes(system);
            //
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultCoordinateSystem(ResolveCoordinateSystem(oldName).Name, system);
            return this[system.Name];
        }
        [PythonAPI("Replaces a result coordinate system the way add_from_selections builds one. A result " +
                   "replacement re-specifies the whole definition, so every point takes either " +
                   "coordinates or a mode and a selection.",
                   Arguments = "old_name: str, name: str = None, origin_created_from: str = None, " +
                               "origin_selection: Selection = None, point_on_x_created_from: str = None, " +
                               "point_on_x_selection: Selection = None, point_in_xy_created_from: str = None, " +
                               "point_in_xy_selection: Selection = None, origin: list[float] = None, " +
                               "point_on_x: list[float] = None, point_in_xy: list[float] = None, " +
                               "coordinate_system_type: str = None, name_visible: bool = None",
                   Returns = "CoordinateSystem")]
        public ResultCoordinateSystemAPI replace_from_selections(object old_name, object name = null,
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
            CoordinateSystem system = CloneForReplace(oldName, nameText);
            ApplyType(system, coordinateSystemType);
            ApplyPointOrCoordinates(system, originCreatedFrom, originSelection, origin, 1, true);
            ApplyPointOrCoordinates(system, pointOnXCreatedFrom, pointOnXSelection, point_on_x, 2, true);
            ApplyPointOrCoordinates(system, pointInXyCreatedFrom, pointInXySelection, point_in_xy, 3, true);
            ApplyNameVisible(system, name_visible);
            ValidateAxes(system);
            //
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultCoordinateSystem(ResolveCoordinateSystem(oldName).Name, system);
            return this[system.Name];
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
        public void rename(object old_name, object new_name)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string newName = PythonValueConverter.ToText(new_name, "new_name", false);
            CoordinateSystem system = ResolveCoordinateSystem(oldName);
            PythonNameComparer.ValidateFormat(newName, "new_name", system.Name);
            if (string.Equals(system.Name, newName)) return;
            if (!PythonNameComparer.AreSame(newName, system.Name) && PythonNameComparer.Contains(GetNames(), newName))
                throw new PythonAPIException("The result coordinate system '" + newName + "' already exists.");
            //
            EnsureCommandsAvailable();
            _commandGateway.RenameResultItem(system, newName);
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
        internal CoordinateSystem ResolveCoordinateSystem(string name)
        {
            if (name == null) throw new PythonAPIException("The result coordinate system name must not be None.");
            //
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
            string[] storedNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                storedNames[i] = existing.FirstOrDefault(name => PythonNameComparer.AreSame(name, names[i]));
                if (storedNames[i] == null)
                    throw new PythonAPIException("The result coordinate system '" + names[i] + "' does not exist.");
            }
            //
            return storedNames;
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
            CoordinateSystem existing = ResolveCoordinateSystem(oldName);
            string newName = name == null ? existing.Name : name;
            ValidateName(newName, existing.Name);
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
                PythonModelArgumentCheck.SetEquations(new[] { system.X1, system.Y1, system.Z1 }, equations, argumentName);
            }
            else if (point == 2)
            {
                system.PointXCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                PythonModelArgumentCheck.SetEquations(new[] { system.X2, system.Y2, system.Z2 }, equations, argumentName);
            }
            else
            {
                system.PointXYCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                PythonModelArgumentCheck.SetEquations(new[] { system.X3, system.Y3, system.Z3 }, equations, argumentName);
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
