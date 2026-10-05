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
using System.Linq;

namespace APIs.Python.Model
{
    internal class CoordinateSystemService : RegionService
    {
        // Constructors
        public CoordinateSystemService(ModelResolver modelResolver, SelectionService selectionService,
                                       IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, modelGateway)
        {
        }


        // Methods
        // A coordinate system has a name space of its own, as in FrmCoordinateSystem, rather than the one the
        // parts, the sets and the reference points share
        private void ValidateCoordinateSystemName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            if (PythonNameComparer.Contains(GetNames(), name))
                throw new PythonAPIException("The coordinate system '" + name + "' already exists.");
        }
        public string[] GetNames()
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null) return new string[0];
            //
            return mesh.CoordinateSystems.Keys.ToArray();
        }
        public CoordinateSystem GetCoordinateSystem(string name)
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null || name == null) return null;
            //
            CoordinateSystem coordinateSystem;
            if (!mesh.CoordinateSystems.TryGetValue(name, out coordinateSystem)) return null;
            //
            return coordinateSystem;
        }
        public CoordinateSystem ResolveCoordinateSystem(string name)
        {
            CoordinateSystem coordinateSystem = GetCoordinateSystem(name);
            if (coordinateSystem == null)
                throw new PythonAPIException("The coordinate system '" + name + "' does not exist.");
            //
            return coordinateSystem;
        }
        public void Add(string name, string type, object origin, object pointOnX, object pointInXY,
                        object nameVisible)
        {
            ValidateCoordinateSystemName(name, null);
            CoordinateSystem coordinateSystem = Build(name, type, origin, pointOnX, pointInXY);
            ApplyNameVisible(coordinateSystem, nameVisible);
            ValidateDefinition(coordinateSystem);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelCoordinateSystem(coordinateSystem);
        }
        public void Replace(string oldName, string name, string type, object origin, object pointOnX,
                            object pointInXY, object nameVisible)
        {
            CoordinateSystem existing = ResolveCoordinateSystem(oldName);
            string newName = name == null ? oldName : name;
            ValidateCoordinateSystemName(newName, oldName);
            CoordinateSystem coordinateSystem = existing.DeepClone();
            coordinateSystem.Name = newName;
            if (type != null)
                coordinateSystem.Type = PythonValueConverter.ToEnum<CoordinateSystemTypeEnum>(type,
                                                                                              "coordinate_system_type");
            if (origin != null) ApplyCoordinates(coordinateSystem, origin, 1);
            if (pointOnX != null) ApplyCoordinates(coordinateSystem, pointOnX, 2);
            if (pointInXY != null) ApplyCoordinates(coordinateSystem, pointInXY, 3);
            ApplyNameVisible(coordinateSystem, nameVisible);
            // The completed definition is checked, so an edit is rejected for the same reason a creation is
            ValidateDefinition(coordinateSystem);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceModelCoordinateSystem(oldName, coordinateSystem);
        }
        public void AddFromSelections(string name, string type, string originCreatedFrom,
                                       SelectionAPI originSelection, string pointOnXCreatedFrom,
                                       SelectionAPI pointOnXSelection, string pointInXYCreatedFrom,
                                       SelectionAPI pointInXYSelection, object origin, object pointOnX,
                                       object pointInXY, object nameVisible)
        {
            ValidateCoordinateSystemName(name, null);
            CoordinateSystem item = new CoordinateSystem(name, GetIsTwoDimensional());
            if (type != null)
                item.Type = PythonValueConverter.ToEnum<CoordinateSystemTypeEnum>(type, "coordinate_system_type");
            ApplyPointOrCoordinates(item, originCreatedFrom, originSelection, origin, 1);
            ApplyPointOrCoordinates(item, pointOnXCreatedFrom, pointOnXSelection, pointOnX, 2);
            ApplyPointOrCoordinates(item, pointInXYCreatedFrom, pointInXYSelection, pointInXY, 3);
            ApplyNameVisible(item, nameVisible);
            ValidateDefinition(item);
            EnsureCommandsAvailable();
            ModelGateway.AddModelCoordinateSystem(item);
        }
        public void ReplaceFromSelections(string oldName, string name, string type, string originCreatedFrom,
                                           SelectionAPI originSelection, string pointOnXCreatedFrom,
                                           SelectionAPI pointOnXSelection, string pointInXYCreatedFrom,
                                           SelectionAPI pointInXYSelection, object origin, object pointOnX,
                                           object pointInXY, object nameVisible)
        {
            CoordinateSystem existing = ResolveCoordinateSystem(oldName);
            string newName = name == null ? oldName : name;
            ValidateCoordinateSystemName(newName, oldName);
            CoordinateSystem item = existing.DeepClone();
            item.Name = newName;
            if (type != null)
                item.Type = PythonValueConverter.ToEnum<CoordinateSystemTypeEnum>(type, "coordinate_system_type");
            ApplyPointOrCoordinates(item, originCreatedFrom, originSelection, origin, 1);
            ApplyPointOrCoordinates(item, pointOnXCreatedFrom, pointOnXSelection, pointOnX, 2);
            ApplyPointOrCoordinates(item, pointInXYCreatedFrom, pointInXYSelection, pointInXY, 3);
            ApplyNameVisible(item, nameVisible);
            ValidateDefinition(item);
            EnsureCommandsAvailable();
            ModelGateway.ReplaceModelCoordinateSystem(oldName, item);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateModelCoordinateSystems(names);
        }
        // The name visibility is part of the stored definition, so a creation carries the same value its
        // replacement can
        private static void ApplyNameVisible(CoordinateSystem coordinateSystem, object nameVisible)
        {
            if (nameVisible == null) return;
            //
            coordinateSystem.NameVisible = PythonValueConverter.ToBool(nameVisible, "name_visible");
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveModelCoordinateSystems(names);
        }
        public void Hide(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.HideModelCoordinateSystems(names);
        }
        public void Show(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.ShowModelCoordinateSystems(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateCoordinateSystemName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        //
        private CoordinateSystem Build(string name, string type, object origin, object pointOnX, object pointInXY)
        {
            EquationString[] originPoint = PythonValueConverter.ToEquationArray(origin, "origin", 3);
            EquationString[] xPoint = PythonValueConverter.ToEquationArray(pointOnX, "point_on_x", 3);
            EquationString[] xyPoint = PythonValueConverter.ToEquationArray(pointInXY, "point_in_xy", 3);
            //
            CoordinateSystem coordinateSystem = new CoordinateSystem(name, GetIsTwoDimensional());
            if (type != null)
                coordinateSystem.Type = PythonValueConverter.ToEnum<CoordinateSystemTypeEnum>(type,
                                                                                              "coordinate_system_type");
            coordinateSystem.X1.Equation = originPoint[0];
            coordinateSystem.Y1.Equation = originPoint[1];
            coordinateSystem.Z1.Equation = originPoint[2];
            coordinateSystem.X2.Equation = xPoint[0];
            coordinateSystem.Y2.Equation = xPoint[1];
            coordinateSystem.Z2.Equation = xPoint[2];
            coordinateSystem.X3.Equation = xyPoint[0];
            coordinateSystem.Y3.Equation = xyPoint[1];
            coordinateSystem.Z3.Equation = xyPoint[2];
            //
            return coordinateSystem;
        }
        private void ApplyPoint(string createdFrom, SelectionAPI selection, string modeArgument,
                                string selectionArgument, Action<CsPointCreatedFromEnum, SelectionCompilation> apply)
        {
            if (createdFrom == null && selection == null) return;
            if (createdFrom == null || selection == null)
                throw new PythonAPIException("The arguments '" + modeArgument + "' and '" + selectionArgument +
                                             "' must be given together.");
            //
            CsPointCreatedFromEnum mode =
                PythonValueConverter.ToEnum<CsPointCreatedFromEnum>(createdFrom, modeArgument);
            if (mode == CsPointCreatedFromEnum.Coordinates)
                throw new PythonAPIException("The argument '" + modeArgument + "' must be OnPoint, " +
                                             "BetweenTwoPoints or CircleCenter when a selection is given.");
            //
            SelectionCompilation compilation = CompileModelSelection(selection, "nodes");
            int expected = mode == CsPointCreatedFromEnum.OnPoint
                           ? 1 : (mode == CsPointCreatedFromEnum.BetweenTwoPoints ? 2 : 3);
            if (compilation.Ids.Length != expected)
                throw new PythonAPIException("The mode " + mode + " needs exactly " + expected +
                                             " selected points, but " + compilation.Ids.Length + " were given.");
            //
            apply(mode, compilation);
        }
        private void ApplyPointOrCoordinates(CoordinateSystem item, string createdFrom, SelectionAPI selection,
                                              object coordinates, int point)
        {
            if (coordinates != null)
            {
                if (createdFrom != null || selection != null)
                    throw new PythonAPIException("A point must use either coordinates or a selection, not both.");
                ApplyCoordinates(item, coordinates, point);
                return;
            }
            if (createdFrom == null && selection == null) return;
            // The three points are three pairs of arguments, so a refusal names the pair the caller
            // wrote rather than a generic point that is not an argument of anything
            string pointName = point == 1 ? "origin" : (point == 2 ? "point_on_x" : "point_in_xy");
            ApplyPoint(createdFrom, selection, pointName + "_created_from", pointName + "_selection",
                       delegate (CsPointCreatedFromEnum mode, SelectionCompilation compilation)
                       {
                           if (point == 1)
                           {
                               item.CenterCreatedFrom = mode;
                               item.CenterCreationData = compilation.Selection;
                               item.CenterCreationIds = compilation.Ids;
                           }
                           else if (point == 2)
                           {
                               item.PointXCreatedFrom = mode;
                               item.PointXCreationData = compilation.Selection;
                               item.PointXCreationIds = compilation.Ids;
                           }
                           else
                           {
                               item.PointXYCreatedFrom = mode;
                               item.PointXYCreationData = compilation.Selection;
                               item.PointXYCreationIds = compilation.Ids;
                           }
                       });
        }
        // A point that comes from a selection is resolved on the model mesh before the axes are
        // checked, so a definition the application would reject never reaches the command history
        private void ValidateDefinition(CoordinateSystem item)
        {
            CoordinateSystemValidator.ValidateAxes(item, GetMeshOrNull());
        }
        private static void ApplyCoordinates(CoordinateSystem coordinateSystem, object value, int point)
        {
            string argumentName = point == 1 ? "origin" : (point == 2 ? "point_on_x" : "point_in_xy");
            EquationString[] equations = PythonValueConverter.ToEquationArray(value, argumentName, 3);
            if (point == 1)
            {
                coordinateSystem.CenterCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                coordinateSystem.X1.Equation = equations[0];
                coordinateSystem.Y1.Equation = equations[1];
                coordinateSystem.Z1.Equation = equations[2];
            }
            else if (point == 2)
            {
                coordinateSystem.PointXCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                coordinateSystem.X2.Equation = equations[0];
                coordinateSystem.Y2.Equation = equations[1];
                coordinateSystem.Z2.Equation = equations[2];
            }
            else
            {
                coordinateSystem.PointXYCreatedFrom = CsPointCreatedFromEnum.Coordinates;
                coordinateSystem.X3.Equation = equations[0];
                coordinateSystem.Y3.Equation = equations[1];
                coordinateSystem.Z3.Equation = equations[2];
            }
        }
    }
}
