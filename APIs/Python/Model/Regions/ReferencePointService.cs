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
using System.Linq;

namespace APIs.Python.Model
{
    internal class ReferencePointService : RegionService
    {
        // Constructors
        public ReferencePointService(ModelResolver modelResolver, SelectionService selectionService,
                                     IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, modelGateway)
        {
        }


        // Methods
        public string[] GetNames()
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null) return new string[0];
            //
            return mesh.ReferencePoints.Keys.ToArray();
        }
        public FeReferencePoint GetReferencePoint(string name)
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null || name == null) return null;
            //
            FeReferencePoint referencePoint;
            if (!mesh.ReferencePoints.TryGetValue(name, out referencePoint)) return null;
            //
            return referencePoint;
        }
        public FeReferencePoint ResolveReferencePoint(string name)
        {
            FeReferencePoint referencePoint = GetReferencePoint(name);
            if (referencePoint == null)
                throw new PythonAPIException("The reference point '" + name + "' does not exist.");
            //
            return referencePoint;
        }
        public void Add(string name, object coordinates, object nameVisible, object color)
        {
            ValidateNewName(name, null);
            FeReferencePoint referencePoint = Build(name, coordinates);
            ApplyDisplayValues(referencePoint, nameVisible, color);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelReferencePoint(referencePoint);
        }
        public void AddFromSelection(string name, string createdFrom, SelectionAPI selection,
                                     object nameVisible, object color)
        {
            ValidateNewName(name, null);
            FeReferencePoint referencePoint = new FeReferencePoint(name, 0, 0, 0);
            ApplySelection(referencePoint, createdFrom, selection);
            ApplyDisplayValues(referencePoint, nameVisible, color);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelReferencePoint(referencePoint);
        }
        public void AddFromRegion(string name, string createdFrom, string regionType, string regionName,
                                  object nameVisible, object color)
        {
            ValidateNewName(name, null);
            FeReferencePoint referencePoint = new FeReferencePoint(name, 0, 0, 0);
            ApplyRegion(referencePoint, createdFrom, regionType, regionName);
            ApplyDisplayValues(referencePoint, nameVisible, color);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelReferencePoint(referencePoint);
        }
        public void Replace(string oldName, string name, object coordinates, object nameVisible, object color)
        {
            FeReferencePoint referencePoint = CloneForReplace(oldName, name);
            if (coordinates != null) ApplyCoordinates(referencePoint, coordinates);
            ApplyDisplayValues(referencePoint, nameVisible, color);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceModelReferencePoint(oldName, referencePoint);
        }
        // A model side replacement keeps what it is not told, so a value that is not given is taken from
        // the stored definition rather than being required again
        public void ReplaceFromSelection(string oldName, string name, string createdFrom,
                                         SelectionAPI selection, object nameVisible, object color)
        {
            FeReferencePoint referencePoint = CloneForReplace(oldName, name);
            string mode = createdFrom != null ? createdFrom : GetKeptCreatedFrom(referencePoint, "created_from");
            if (selection == null) ApplyKeptSelection(referencePoint, mode);
            else ApplySelection(referencePoint, mode, selection);
            ApplyDisplayValues(referencePoint, nameVisible, color);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceModelReferencePoint(oldName, referencePoint);
        }
        public void ReplaceFromRegion(string oldName, string name, string createdFrom, string regionType,
                                      string regionName, object nameVisible, object color)
        {
            FeReferencePoint referencePoint = CloneForReplace(oldName, name);
            string mode = createdFrom != null ? createdFrom : GetKeptCreatedFrom(referencePoint, "created_from");
            string region = regionType != null ? regionType : GetKeptRegionType(referencePoint);
            string keptRegionName = regionName != null ? regionName : GetKeptRegionName(referencePoint);
            ApplyRegion(referencePoint, mode, region, keptRegionName);
            ApplyDisplayValues(referencePoint, nameVisible, color);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceModelReferencePoint(oldName, referencePoint);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateModelReferencePoints(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveModelReferencePoints(names);
        }
        public void Hide(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.HideModelReferencePoints(names);
        }
        public void Show(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.ShowModelReferencePoints(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        //
        private FeReferencePoint Build(string name, object coordinates)
        {
            FeReferencePoint referencePoint = new FeReferencePoint(name, 0, 0, 0);
            ApplyCoordinates(referencePoint, coordinates);
            //
            return referencePoint;
        }
        private FeReferencePoint CloneForReplace(string oldName, string name)
        {
            FeReferencePoint existing = ResolveReferencePoint(oldName);
            string newName = name == null ? oldName : name;
            ValidateNewName(newName, oldName);
            FeReferencePoint referencePoint = existing.DeepClone();
            referencePoint.Name = newName;
            //
            return referencePoint;
        }
        // The name visibility and the symbol colour are part of the stored definition, so a creation
        // carries the same values its replacement can
        private static void ApplyDisplayValues(FeReferencePoint referencePoint, object nameVisible, object color)
        {
            if (nameVisible != null)
                referencePoint.NameVisible = PythonValueConverter.ToBool(nameVisible, "name_visible");
            if (color != null)
                referencePoint.Color = PythonColorConverter.ToColor(color, "color");
        }
        private static void ApplyCoordinates(FeReferencePoint referencePoint, object coordinates)
        {
            EquationString[] point = PythonValueConverter.ToEquationArray(coordinates, "coordinates", 3);
            referencePoint.CreatedFrom = FeReferencePointCreatedFrom.Coordinates;
            referencePoint.X.Equation = point[0];
            referencePoint.Y.Equation = point[1];
            referencePoint.Z.Equation = point[2];
            ClearOtherKinds(referencePoint);
        }
        private void ApplySelection(FeReferencePoint referencePoint, string createdFrom, SelectionAPI selection)
        {
            FeReferencePointCreatedFrom mode =
                PythonValueConverter.ToEnum<FeReferencePointCreatedFrom>(createdFrom, "created_from");
            if (mode != FeReferencePointCreatedFrom.OnPoint && mode != FeReferencePointCreatedFrom.BetweenTwoPoints &&
                mode != FeReferencePointCreatedFrom.CircleCenter)
                throw new PythonAPIException("The argument 'created_from' must be OnPoint, BetweenTwoPoints or " +
                                             "CircleCenter when a selection is given.");
            // The mesh computes the coordinates from the creation IDs on the update that follows the command
            SelectionCompilation compilation = CompileModelSelection(selection, "nodes");
            int expected = mode == FeReferencePointCreatedFrom.OnPoint
                           ? 1 : (mode == FeReferencePointCreatedFrom.BetweenTwoPoints ? 2 : 3);
            if (compilation.Ids.Length != expected)
                throw new PythonAPIException("The mode " + mode + " needs exactly " + expected +
                                             " selected points, but " + compilation.Ids.Length + " were given.");
            //
            referencePoint.CreatedFrom = mode;
            referencePoint.CreationData = compilation.Selection;
            referencePoint.CreationIds = compilation.Ids;
            ClearOtherKinds(referencePoint);
        }
        private void ApplyRegion(FeReferencePoint referencePoint, string createdFrom, string regionType,
                                 string regionName)
        {
            FeReferencePointCreatedFrom mode =
                PythonValueConverter.ToEnum<FeReferencePointCreatedFrom>(createdFrom, "created_from");
            if (mode != FeReferencePointCreatedFrom.CenterOfGravity &&
                mode != FeReferencePointCreatedFrom.BoundingBoxCenter)
                throw new PythonAPIException("The argument 'created_from' must be CenterOfGravity or " +
                                             "BoundingBoxCenter when a region is given.");
            // The reference point dialog offers a node set or a surface, so a part is not a region here
            RegionTypeEnum region = PythonValueConverter.ToEnum<RegionTypeEnum>(regionType, "region_type");
            if (region != RegionTypeEnum.NodeSetName && region != RegionTypeEnum.SurfaceName)
                throw new PythonAPIException("The argument 'region_type' must be NodeSetName or SurfaceName.");
            if (regionName == null || regionName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'region_name' must not be empty.");
            ValidateRegionExists(region, regionName);
            //
            referencePoint.CreatedFrom = mode;
            referencePoint.RegionType = region;
            referencePoint.RegionName = regionName;
            ClearOtherKinds(referencePoint);
        }
        private static string GetKeptCreatedFrom(FeReferencePoint referencePoint, string argumentName)
        {
            if (referencePoint.CreatedFrom == FeReferencePointCreatedFrom.Coordinates)
                throw new PythonAPIException("The argument '" + argumentName + "' must be given, because the " +
                                             "reference point is defined by coordinates.");
            //
            return referencePoint.CreatedFrom.ToString();
        }
        // A value is kept only while the construction kind stays the same, so a region left over from an
        // earlier kind is never brought back
        private static string GetKeptRegionType(FeReferencePoint referencePoint)
        {
            if (!IsRegionKind(referencePoint.CreatedFrom) || referencePoint.RegionName == null)
                throw new PythonAPIException("The argument 'region_type' must be given, because the reference " +
                                             "point is not defined by a region.");
            //
            return referencePoint.RegionType.ToString();
        }
        private static string GetKeptRegionName(FeReferencePoint referencePoint)
        {
            if (!IsRegionKind(referencePoint.CreatedFrom) || referencePoint.RegionName == null)
                throw new PythonAPIException("The argument 'region_name' must be given, because the reference " +
                                             "point is not defined by a region.");
            //
            return referencePoint.RegionName;
        }
        // Only the mode is being changed, so the stored selection is reused for the new mode
        private static void ApplyKeptSelection(FeReferencePoint referencePoint, string createdFrom)
        {
            FeReferencePointCreatedFrom mode =
                PythonValueConverter.ToEnum<FeReferencePointCreatedFrom>(createdFrom, "created_from");
            if (mode != FeReferencePointCreatedFrom.OnPoint && mode != FeReferencePointCreatedFrom.BetweenTwoPoints &&
                mode != FeReferencePointCreatedFrom.CircleCenter)
                throw new PythonAPIException("The argument 'created_from' must be OnPoint, BetweenTwoPoints or " +
                                             "CircleCenter when a selection is kept.");
            Selection keptSelection = referencePoint.CreationData;
            int[] keptIds = referencePoint.CreationIds;
            if (!IsSelectionKind(referencePoint.CreatedFrom) || keptSelection == null || keptIds == null ||
                keptIds.Length == 0)
                throw new PythonAPIException("The argument 'selection' must be given, because the reference " +
                                             "point is not defined by a selection.");
            int expected = mode == FeReferencePointCreatedFrom.OnPoint
                           ? 1 : (mode == FeReferencePointCreatedFrom.BetweenTwoPoints ? 2 : 3);
            if (keptIds.Length != expected)
                throw new PythonAPIException("The mode " + mode + " needs exactly " + expected +
                                             " selected points, but the kept selection has " + keptIds.Length + ".");
            //
            referencePoint.CreatedFrom = mode;
            referencePoint.CreationData = keptSelection;
            referencePoint.CreationIds = keptIds;
            ClearOtherKinds(referencePoint);
        }
        // A replace that switches the construction kind drops what belonged to the old kind, whether or not
        // the mode itself changed, so a later replace cannot bring it back
        private static void ClearOtherKinds(FeReferencePoint referencePoint)
        {
            if (!IsSelectionKind(referencePoint.CreatedFrom))
            {
                referencePoint.CreationData = null;
                referencePoint.CreationIds = null;
            }
            if (!IsRegionKind(referencePoint.CreatedFrom))
            {
                referencePoint.RegionName = null;
                referencePoint.RegionType = RegionTypeEnum.NodeSetName;
            }
        }
        private static bool IsSelectionKind(FeReferencePointCreatedFrom mode)
        {
            return mode == FeReferencePointCreatedFrom.OnPoint || mode == FeReferencePointCreatedFrom.BetweenTwoPoints ||
                   mode == FeReferencePointCreatedFrom.CircleCenter;
        }
        private static bool IsRegionKind(FeReferencePointCreatedFrom mode)
        {
            return mode == FeReferencePointCreatedFrom.CenterOfGravity ||
                   mode == FeReferencePointCreatedFrom.BoundingBoxCenter;
        }
        private void ValidateRegionExists(RegionTypeEnum region, string regionName)
        {
            FeMesh mesh = GetMeshOrNull();
            bool exists = false;
            if (mesh != null)
            {
                if (region == RegionTypeEnum.NodeSetName) exists = mesh.NodeSets.ContainsKey(regionName);
                else if (region == RegionTypeEnum.SurfaceName) exists = mesh.Surfaces.ContainsKey(regionName);
            }
            if (!exists)
                throw new PythonAPIException("The " + region + " '" + regionName + "' does not exist.");
        }
    }
}
