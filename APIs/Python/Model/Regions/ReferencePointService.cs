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
        public void Add(string name, object coordinates)
        {
            ValidateNewName(name, null);
            FeReferencePoint referencePoint = Build(name, coordinates);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelReferencePoint(referencePoint);
        }
        public void AddFromSelection(string name, string createdFrom, SelectionAPI selection)
        {
            ValidateNewName(name, null);
            FeReferencePointCreatedFrom mode =
                PythonValueConverter.ToEnum<FeReferencePointCreatedFrom>(createdFrom, "created_from");
            if (mode != FeReferencePointCreatedFrom.OnPoint && mode != FeReferencePointCreatedFrom.BetweenTwoPoints &&
                mode != FeReferencePointCreatedFrom.CircleCenter)
                throw new PythonAPIException("The argument 'created_from' must be OnPoint, BetweenTwoPoints or " +
                                             "CircleCenter when a selection is given.");
            // The mesh computes the coordinates from the creation IDs on the update that follows the add
            SelectionCompilation compilation = CompileModelSelection(selection, "nodes");
            int expected = mode == FeReferencePointCreatedFrom.OnPoint
                           ? 1 : (mode == FeReferencePointCreatedFrom.BetweenTwoPoints ? 2 : 3);
            if (compilation.Ids.Length != expected)
                throw new PythonAPIException("The mode " + mode + " needs exactly " + expected +
                                             " selected points, but " + compilation.Ids.Length + " were given.");
            //
            FeReferencePoint referencePoint = new FeReferencePoint(name, 0, 0, 0);
            referencePoint.CreatedFrom = mode;
            referencePoint.CreationData = compilation.Selection;
            referencePoint.CreationIds = compilation.Ids;
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelReferencePoint(referencePoint);
        }
        public void AddFromRegion(string name, string createdFrom, string regionType, string regionName)
        {
            ValidateNewName(name, null);
            FeReferencePointCreatedFrom mode =
                PythonValueConverter.ToEnum<FeReferencePointCreatedFrom>(createdFrom, "created_from");
            if (mode != FeReferencePointCreatedFrom.CenterOfGravity &&
                mode != FeReferencePointCreatedFrom.BoundingBoxCenter)
                throw new PythonAPIException("The argument 'created_from' must be CenterOfGravity or " +
                                             "BoundingBoxCenter when a region is given.");
            //
            RegionTypeEnum region = PythonValueConverter.ToEnum<RegionTypeEnum>(regionType, "region_type");
            if (region != RegionTypeEnum.NodeSetName && region != RegionTypeEnum.SurfaceName &&
                region != RegionTypeEnum.PartName)
                throw new PythonAPIException("The argument 'region_type' must be NodeSetName, SurfaceName or " +
                                             "PartName.");
            if (regionName == null || regionName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'region_name' must not be empty.");
            //
            FeReferencePoint referencePoint = new FeReferencePoint(name, 0, 0, 0);
            referencePoint.CreatedFrom = mode;
            referencePoint.RegionType = region;
            referencePoint.RegionName = regionName;
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelReferencePoint(referencePoint);
        }
        public void Replace(string oldName, string name, object coordinates)
        {
            ResolveReferencePoint(oldName);
            ValidateNewName(name, oldName);
            FeReferencePoint referencePoint = Build(name, coordinates);
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
            double[] point = PythonDoubleListConverter.ToDoubleArray(coordinates, "coordinates");
            if (point == null || point.Length != 3)
                throw new PythonAPIException("The argument 'coordinates' must contain 3 values.");
            //
            if (GetIsTwoDimensional()) return new FeReferencePoint(name, point[0], point[1]);
            else return new FeReferencePoint(name, point[0], point[1], point[2]);
        }
    }
}
