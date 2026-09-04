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
        public void Add(string name, string type, object origin, object pointOnX, object pointInXY)
        {
            ValidateNewName(name, null);
            CoordinateSystem coordinateSystem = Build(name, type, origin, pointOnX, pointInXY);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelCoordinateSystem(coordinateSystem);
        }
        public void Replace(string oldName, string name, string type, object origin, object pointOnX,
                            object pointInXY)
        {
            ResolveCoordinateSystem(oldName);
            ValidateNewName(name, oldName);
            CoordinateSystem coordinateSystem = Build(name, type, origin, pointOnX, pointInXY);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceModelCoordinateSystem(oldName, coordinateSystem);
        }
        public void AddFromSelections(string name, string type, string originCreatedFrom,
                                      SelectionAPI originSelection, string pointOnXCreatedFrom,
                                      SelectionAPI pointOnXSelection, string pointInXYCreatedFrom,
                                      SelectionAPI pointInXYSelection)
        {
            ValidateNewName(name, null);
            CoordinateSystem coordinateSystem = new CoordinateSystem(name, GetIsTwoDimensional());
            if (type != null)
                coordinateSystem.Type = PythonValueConverter.ToEnum<CoordinateSystemTypeEnum>(type, "type");
            // The mesh computes the three points from the creation IDs on the update that follows the add
            ApplyPoint(originCreatedFrom, originSelection, "origin_created_from", "origin_selection",
                       delegate (CsPointCreatedFromEnum mode, SelectionCompilation compilation)
                       {
                           coordinateSystem.CenterCreatedFrom = mode;
                           coordinateSystem.CenterCreationData = compilation.Selection;
                           coordinateSystem.CenterCreationIds = compilation.Ids;
                       });
            ApplyPoint(pointOnXCreatedFrom, pointOnXSelection, "point_on_x_created_from", "point_on_x_selection",
                       delegate (CsPointCreatedFromEnum mode, SelectionCompilation compilation)
                       {
                           coordinateSystem.PointXCreatedFrom = mode;
                           coordinateSystem.PointXCreationData = compilation.Selection;
                           coordinateSystem.PointXCreationIds = compilation.Ids;
                       });
            ApplyPoint(pointInXYCreatedFrom, pointInXYSelection, "point_in_xy_created_from",
                       "point_in_xy_selection",
                       delegate (CsPointCreatedFromEnum mode, SelectionCompilation compilation)
                       {
                           coordinateSystem.PointXYCreatedFrom = mode;
                           coordinateSystem.PointXYCreationData = compilation.Selection;
                           coordinateSystem.PointXYCreationIds = compilation.Ids;
                       });
            //
            EnsureCommandsAvailable();
            ModelGateway.AddModelCoordinateSystem(coordinateSystem);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateModelCoordinateSystems(names);
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
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        //
        private CoordinateSystem Build(string name, string type, object origin, object pointOnX, object pointInXY)
        {
            double[] originPoint = ToPoint(origin, "origin");
            double[] xPoint = ToPoint(pointOnX, "point_on_x");
            double[] xyPoint = ToPoint(pointInXY, "point_in_xy");
            //
            CoordinateSystem coordinateSystem = new CoordinateSystem(name, GetIsTwoDimensional());
            if (type != null)
                coordinateSystem.Type = PythonValueConverter.ToEnum<CoordinateSystemTypeEnum>(type, "type");
            coordinateSystem.X1.Equation = new EquationString(ToText(originPoint[0]));
            coordinateSystem.Y1.Equation = new EquationString(ToText(originPoint[1]));
            coordinateSystem.Z1.Equation = new EquationString(ToText(originPoint[2]));
            coordinateSystem.X2.Equation = new EquationString(ToText(xPoint[0]));
            coordinateSystem.Y2.Equation = new EquationString(ToText(xPoint[1]));
            coordinateSystem.Z2.Equation = new EquationString(ToText(xPoint[2]));
            coordinateSystem.X3.Equation = new EquationString(ToText(xyPoint[0]));
            coordinateSystem.Y3.Equation = new EquationString(ToText(xyPoint[1]));
            coordinateSystem.Z3.Equation = new EquationString(ToText(xyPoint[2]));
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
        private static double[] ToPoint(object value, string argumentName)
        {
            double[] point = PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (point == null || point.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            //
            return point;
        }
        private static string ToText(double value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
