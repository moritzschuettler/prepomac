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
    // The reference points of a result mesh. They belong to the result rather than to the model, so
    // they are reached through the result mesh and are only available while that result is current.
    public class ResultReferencePointsAPI : BaseAPI, IReadOnlyList<ResultReferencePointAPI>
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
        internal ResultReferencePointsAPI(MeshDataService meshService, string resultName,
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
        public ResultReferencePointAPI __getitem__(string name)
        {
            return this[name];
        }
        public ResultReferencePointAPI this[string name]
        {
            get { return new ResultReferencePointAPI(ResolveReferencePoint(name).Name, this); }
        }
        // Explicit, so that the Python side sees a single indexer and resolves collection[name] without
        // an ambiguous overload
        ResultReferencePointAPI IReadOnlyList<ResultReferencePointAPI>.this[int index]
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
        public IEnumerator<ResultReferencePointAPI> GetEnumerator()
        {
            foreach (string name in GetNames()) yield return this[name];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool __contains__(string name)
        {
            return contains(name);
        }
        [PythonAPI("Returns the names of the result reference points.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(GetNames());
        }
        [PythonAPI("Returns true if the result mesh contains the result reference point. Names are compared " +
                   "without regard to case.", Arguments = "name: str", Returns = "bool")]
        public bool contains(string name)
        {
            return name != null && PythonNameComparer.Contains(GetNames(), name);
        }
        [PythonAPI("Returns the result reference point with the specified name.", Arguments = "name: str",
                   Returns = "ReferencePoint")]
        public ResultReferencePointAPI get(string name)
        {
            return this[name];
        }
        [PythonAPI("Returns the first result reference point.", Returns = "ReferencePoint")]
        public ResultReferencePointAPI first()
        {
            string[] names = GetNames();
            if (names.Length == 0) throw new PythonAPIException("The reference points collection is empty.");
            return this[names[0]];
        }
        [PythonAPI("Returns the last result reference point.", Returns = "ReferencePoint")]
        public ResultReferencePointAPI last()
        {
            string[] names = GetNames();
            if (names.Length == 0) throw new PythonAPIException("The reference points collection is empty.");
            return this[names[names.Length - 1]];
        }
        [PythonAPI("Adds a result reference point using command history.",
                   Arguments = "name: str, coordinates: list[float], name_visible: bool = None, " +
                               "color: list[int] or str = None", Returns = "ReferencePoint",
                   Example = "pmx.results.current.mesh.reference_points.add('RP-1', [0, 0, 0])")]
        public ResultReferencePointAPI add(string name, object coordinates, object name_visible = null,
                                           object color = null)
        {
            ValidateName(name, null);
            FeReferencePoint point = new FeReferencePoint(name, 0, 0, 0);
            ApplyCoordinates(point, coordinates);
            ApplyDisplayValues(point, name_visible, color);
            //
            EnsureCommandsAvailable();
            _commandGateway.AddResultReferencePoint(point);
            return this[name];
        }
        [PythonAPI("Adds a result reference point from selected result mesh points using command history. " +
                   "The mode is OnPoint, BetweenTwoPoints or CircleCenter, and the coordinates are " +
                   "computed by the result mesh.",
                   Arguments = "name: str, created_from: str, selection: Selection, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint",
                   Example = "pmx.results.current.mesh.reference_points.add_from_selection('RP-1', " +
                             "'CircleCenter', selection)")]
        public ResultReferencePointAPI add_from_selection(string name, string created_from, SelectionAPI selection,
                                                          object name_visible = null, object color = null)
        {
            ValidateName(name, null);
            FeReferencePoint point = new FeReferencePoint(name, 0, 0, 0);
            ApplySelection(point, created_from, selection);
            ApplyDisplayValues(point, name_visible, color);
            //
            EnsureCommandsAvailable();
            _commandGateway.AddResultReferencePoint(point);
            return this[name];
        }
        [PythonAPI("Adds a result reference point from a named result region using command history. The " +
                   "mode is CenterOfGravity or BoundingBoxCenter, and the region type is NodeSetName or " +
                   "SurfaceName.",
                   Arguments = "name: str, created_from: str, region_type: str, region_name: str, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ResultReferencePointAPI add_from_region(string name, string created_from, string region_type,
                                                       string region_name, object name_visible = null,
                                                       object color = null)
        {
            ValidateName(name, null);
            FeReferencePoint point = new FeReferencePoint(name, 0, 0, 0);
            ApplyRegion(point, created_from, region_type, region_name);
            ApplyDisplayValues(point, name_visible, color);
            //
            EnsureCommandsAvailable();
            _commandGateway.AddResultReferencePoint(point);
            return this[name];
        }
        [PythonAPI("Replaces a result reference point using command history. The coordinates are the whole " +
                   "definition and are always given; the name and the display values may be left out and " +
                   "are kept.",
                   Arguments = "old_name: str, name: str = None, coordinates: list[float], " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ResultReferencePointAPI replace(string old_name, string name = null, object coordinates = null,
                                               object name_visible = null, object color = null)
        {
            if (coordinates == null)
                throw new PythonAPIException("The argument 'coordinates' must be given.");
            FeReferencePoint point = CloneForReplace(old_name, name);
            ApplyCoordinates(point, coordinates);
            ApplyDisplayValues(point, name_visible, color);
            //
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultReferencePoint(ResolveReferencePoint(old_name).Name, point);
            return this[point.Name];
        }
        [PythonAPI("Replaces a result reference point by result mesh points selected the way " +
                   "add_from_selection does. The mode and the selection are the whole definition and are " +
                   "always given.",
                   Arguments = "old_name: str, name: str = None, created_from: str = None, " +
                               "selection: Selection = None, name_visible: bool = None, " +
                               "color: list[int] or str = None", Returns = "ReferencePoint")]
        public ResultReferencePointAPI replace_from_selection(string old_name, string name = null,
                                                              string created_from = null,
                                                              SelectionAPI selection = null,
                                                              object name_visible = null, object color = null)
        {
            if (created_from == null || selection == null)
                throw new PythonAPIException("The arguments 'created_from' and 'selection' must both be given.");
            FeReferencePoint point = CloneForReplace(old_name, name);
            ApplySelection(point, created_from, selection);
            ApplyDisplayValues(point, name_visible, color);
            //
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultReferencePoint(ResolveReferencePoint(old_name).Name, point);
            return this[point.Name];
        }
        [PythonAPI("Replaces a result reference point by a named result region the way add_from_region " +
                   "does. The mode and the region are the whole definition and are always given.",
                   Arguments = "old_name: str, name: str = None, created_from: str = None, " +
                               "region_type: str = None, region_name: str = None, " +
                               "name_visible: bool = None, color: list[int] or str = None",
                   Returns = "ReferencePoint")]
        public ResultReferencePointAPI replace_from_region(string old_name, string name = null,
                                                           string created_from = null, string region_type = null,
                                                           string region_name = null, object name_visible = null,
                                                           object color = null)
        {
            if (created_from == null || region_type == null || region_name == null)
                throw new PythonAPIException("The arguments 'created_from', 'region_type' and 'region_name' " +
                                             "must all be given.");
            FeReferencePoint point = CloneForReplace(old_name, name);
            ApplyRegion(point, created_from, region_type, region_name);
            ApplyDisplayValues(point, name_visible, color);
            //
            EnsureCommandsAvailable();
            _commandGateway.ReplaceResultReferencePoint(ResolveReferencePoint(old_name).Name, point);
            return this[point.Name];
        }
        [PythonAPI("Duplicates result reference points using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void duplicate(object names)
        {
            string[] resolved = ResolveNames(names);
            EnsureCommandsAvailable();
            _commandGateway.DuplicateResultReferencePoints(resolved);
        }
        [PythonAPI("Renames a result reference point using command history.",
                   Arguments = "old_name: str, new_name: str", Returns = "None")]
        public void rename(string old_name, string new_name)
        {
            FeReferencePoint point = ResolveReferencePoint(old_name);
            PythonNameComparer.ValidateFormat(new_name, "new_name", point.Name);
            if (string.Equals(point.Name, new_name)) return;
            if (!PythonNameComparer.AreSame(new_name, point.Name) && PythonNameComparer.Contains(GetNames(), new_name))
                throw new PythonAPIException("The result reference point '" + new_name + "' already exists.");
            //
            EnsureCommandsAvailable();
            _commandGateway.RenameResultItem(point, new_name);
        }
        [PythonAPI("Removes result reference points using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void remove(object names)
        {
            string[] resolved = ResolveNames(names);
            EnsureCommandsAvailable();
            _commandGateway.RemoveResultReferencePoints(resolved);
        }
        [PythonAPI("Hides result reference points using command history.", Arguments = "names: list[str] or str",
                   Returns = "None")]
        public void hide(object names)
        {
            string[] resolved = ResolveNames(names);
            EnsureCommandsAvailable();
            _commandGateway.HideResultReferencePoints(resolved);
        }
        [PythonAPI("Shows result reference points using command history.", Arguments = "names: list[str] or str",
                   Returns = "None")]
        public void show(object names)
        {
            string[] resolved = ResolveNames(names);
            EnsureCommandsAvailable();
            _commandGateway.ShowResultReferencePoints(resolved);
        }
        public override string ToString()
        {
            string[] names = GetNames();
            return "ReferencePoints(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private FeResults ResolveResult()
        {
            if (!_resultResolver.IsCurrentResult(_resultName))
                throw new PythonAPIException("The result '" + _resultName + "' is no longer the current one.");
            //
            return _meshService.ResolveResultWithMesh(_resultName);
        }
        internal FeReferencePoint ResolveReferencePoint(string name)
        {
            if (name == null) throw new PythonAPIException("The result reference point name must not be None.");
            //
            FeResults result = ResolveResult();
            FeReferencePoint point;
            if (result.Mesh == null || !result.Mesh.ReferencePoints.TryGetValue(name, out point))
                throw new PythonAPIException("The result reference point '" + name + "' does not exist.");
            //
            return point;
        }
        private string[] GetNames()
        {
            FeResults result = ResolveResult();
            //
            return result.Mesh == null ? new string[0] : result.Mesh.ReferencePoints.Keys.ToArray();
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
                    throw new PythonAPIException("The result reference point '" + names[i] + "' does not exist.");
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
                throw new PythonAPIException("The result reference point '" + name + "' already exists.");
        }
        private void EnsureCommandsAvailable()
        {
            if (_commandGateway == null)
                throw new PythonAPIException("Result modifications are not available in this Python API context.");
            //
            ResolveResult();
        }
        private FeReferencePoint CloneForReplace(string oldName, string name)
        {
            FeReferencePoint existing = ResolveReferencePoint(oldName);
            string newName = name == null ? existing.Name : name;
            ValidateName(newName, existing.Name);
            FeReferencePoint point = existing.DeepClone();
            point.Name = newName;
            //
            return point;
        }
        // A replacement re-specifies the whole definition, so the construction mode and the creation
        // data of the cloned point must follow the values that are applied
        // The name visibility and the symbol colour are part of the stored definition, so a creation
        // carries the same values its replacement can
        private static void ApplyDisplayValues(FeReferencePoint point, object nameVisible, object color)
        {
            if (nameVisible != null)
                point.NameVisible = PythonValueConverter.ToBool(nameVisible, "name_visible");
            if (color != null)
                point.Color = PythonColorConverter.ToColor(color, "color");
        }
        private static void ApplyCoordinates(FeReferencePoint point, object coordinates)
        {
            EquationString[] values = PythonValueConverter.ToEquationArray(coordinates, "coordinates", 3);
            point.CreatedFrom = FeReferencePointCreatedFrom.Coordinates;
            PythonModelArgumentCheck.SetEquations(new[] { point.X, point.Y, point.Z }, values, "coordinates");
        }
        private void ApplySelection(FeReferencePoint point, string createdFrom, SelectionAPI selection)
        {
            FeReferencePointCreatedFrom mode =
                PythonValueConverter.ToEnum<FeReferencePointCreatedFrom>(createdFrom, "created_from");
            if (mode != FeReferencePointCreatedFrom.OnPoint && mode != FeReferencePointCreatedFrom.BetweenTwoPoints &&
                mode != FeReferencePointCreatedFrom.CircleCenter)
                throw new PythonAPIException("The argument 'created_from' must be OnPoint, BetweenTwoPoints or " +
                                             "CircleCenter when a selection is given.");
            // The result mesh computes the coordinates from the creation IDs on the update that follows
            SelectionCompilation compilation = CompileResultSelection(selection);
            int expected = mode == FeReferencePointCreatedFrom.OnPoint
                           ? 1 : (mode == FeReferencePointCreatedFrom.BetweenTwoPoints ? 2 : 3);
            if (compilation.Ids.Length != expected)
                throw new PythonAPIException("The mode " + mode + " needs exactly " + expected +
                                             " selected points, but " + compilation.Ids.Length + " were given.");
            //
            point.CreatedFrom = mode;
            point.CreationData = compilation.Selection;
            point.CreationIds = compilation.Ids;
        }
        private void ApplyRegion(FeReferencePoint point, string createdFrom, string regionType, string regionName)
        {
            FeReferencePointCreatedFrom mode =
                PythonValueConverter.ToEnum<FeReferencePointCreatedFrom>(createdFrom, "created_from");
            if (mode != FeReferencePointCreatedFrom.CenterOfGravity &&
                mode != FeReferencePointCreatedFrom.BoundingBoxCenter)
                throw new PythonAPIException("The argument 'created_from' must be CenterOfGravity or " +
                                             "BoundingBoxCenter when a region is given.");
            RegionTypeEnum region = PythonValueConverter.ToEnum<RegionTypeEnum>(regionType, "region_type");
            if (region != RegionTypeEnum.NodeSetName && region != RegionTypeEnum.SurfaceName)
                throw new PythonAPIException("The argument 'region_type' must be NodeSetName or SurfaceName.");
            if (regionName == null || regionName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'region_name' must not be empty.");
            string storedRegionName = ResolveRegionName(region, regionName);
            //
            point.CreatedFrom = mode;
            point.RegionType = region;
            point.RegionName = storedRegionName;
        }
        private SelectionCompilation CompileResultSelection(SelectionAPI selection)
        {
            if (selection == null) throw new PythonAPIException("The argument 'selection' must not be None.");
            if (_selectionService == null)
                throw new PythonAPIException("Selections are not available in this Python API context.");
            //
            return _selectionService.CompileForResult(selection, _resultName, "nodes");
        }
        private string ResolveRegionName(RegionTypeEnum region, string regionName)
        {
            FeMesh mesh = ResolveResult().Mesh;
            string storedName = null;
            if (mesh != null)
            {
                FeNodeSet nodeSet;
                FeSurface surface;
                if (region == RegionTypeEnum.NodeSetName && mesh.NodeSets.TryGetValue(regionName, out nodeSet))
                    storedName = nodeSet.Name;
                else if (region == RegionTypeEnum.SurfaceName && mesh.Surfaces.TryGetValue(regionName, out surface))
                    storedName = surface.Name;
            }
            if (storedName == null)
                throw new PythonAPIException("The " + region + " '" + regionName + "' does not exist in the result.");
            return storedName;
        }
    }
}
