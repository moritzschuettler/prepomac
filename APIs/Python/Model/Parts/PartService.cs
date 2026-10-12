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
using CaeModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;

namespace APIs.Python.Model
{
    // The behaviour shared by the geometry and the model part services. The two differ only in the
    // mesh they read and in the commands they call.
    internal abstract class PartService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonMeshGateway _meshGateway;
        private readonly IPythonModelGateway _modelGateway;


        // Properties
        protected IPythonMeshGateway MeshGateway { get { return _meshGateway; } }
        protected IPythonModelGateway ModelGateway { get { return _modelGateway; } }
        protected SelectionService SelectionService { get { return _selectionService; } }
        protected ModelResolver ModelResolver { get { return _modelResolver; } }
        protected abstract string ViewName { get; }


        // Constructors
        protected PartService(ModelResolver modelResolver, SelectionService selectionService,
                              IPythonMeshGateway meshGateway, IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _meshGateway = meshGateway;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null) return new string[0];
            //
            return mesh.Parts.Keys.ToArray();
        }
        public BasePart GetPart(string partName)
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null || partName == null) return null;
            //
            BasePart part;
            if (!mesh.Parts.TryGetValue(partName, out part)) return null;
            //
            return part;
        }
        public BasePart ResolvePart(string partName)
        {
            BasePart part = GetPart(partName);
            if (part == null) throw new PythonAPIException("The part '" + partName + "' does not exist.");
            //
            return part;
        }
        public FeMesh ResolveMesh()
        {
            FeMesh mesh = GetMeshOrNull();
            if (mesh == null) throw new PythonAPIException("There is no mesh in this view.");
            //
            return mesh;
        }
        public void SetColor(string[] partNames, object color)
        {
            Color colorValue = PythonColorConverter.ToPartColor(color, "color");
            EnsureCommandsAvailable();
            SetColorForParts(partNames, colorValue);
        }
        public void ResetColor(string[] partNames)
        {
            EnsureCommandsAvailable();
            ResetColorForParts(partNames);
        }
        public void SetOpacity(string[] partNames, object opacity)
        {
            byte alpha = PythonColorConverter.ToPartAlpha(opacity, "opacity");
            EnsureCommandsAvailable();
            SetTransparencyForParts(partNames, alpha);
        }
        public void Hide(string[] partNames)
        {
            EnsureCommandsAvailable();
            HideParts(partNames);
        }
        public void Show(string[] partNames)
        {
            EnsureCommandsAvailable();
            ShowParts(partNames);
        }
        public void Remove(string[] partNames)
        {
            EnsureCommandsAvailable();
            RemoveParts(partNames);
        }
        public void SetActive(NamedClass[] items, bool active)
        {
            EnsureCommandsAvailable();
            // Every step name is null, because these items do not belong to a step
            _modelGateway.ActivateDeactivateItems(items, active, new string[items.Length]);
        }
        public void Rename(string partName, string newName)
        {
            BasePart part = ResolvePart(partName);
            ValidateNewName(newName, partName, "new_name");
            PartProperties properties = part.GetProperties();
            properties.Name = newName;
            //
            EnsureCommandsAvailable();
            ReplacePartProperties(part.Name, properties);
        }
        public StringListAPI GetElementTypeNames()
        {
            List<string> names = new List<string>();
            foreach (FieldInfo field in typeof(PartProperties).GetFields())
            {
                if (IsElementTypeField(field)) names.Add(ToPythonName(field.Name));
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            //
            return new StringListAPI(names.ToArray());
        }
        // A part keeps an element type only for the element kinds it contains, so only those are listed
        public Dictionary<string, string> GetElementTypes(string partName)
        {
            PartProperties properties = GetPartProperties(ResolvePart(partName).Name);
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (FieldInfo field in typeof(PartProperties).GetFields())
            {
                if (IsElementTypeField(field) && IsElementKindPresent(field, properties))
                    values.Add(ToPythonName(field.Name), field.GetValue(properties).ToString());
            }
            //
            return values;
        }
        // A part property dialog changes the name, the colour and the element types together, and the
        // history records that as one command, so the API applies them in one call too
        public void ReplaceProperties(string oldPartName, string name, object color, object elementTypes)
        {
            oldPartName = ResolvePart(oldPartName).Name;
            PartProperties properties = GetPartProperties(oldPartName);
            object boxed = properties;
            //
            if (name != null)
            {
                ValidateNewName(name, oldPartName, "name");
                typeof(PartProperties).GetProperty("Name").SetValue(boxed, name, null);
            }
            if (color != null)
                typeof(PartProperties).GetField("Color").SetValue(boxed,
                                                                  PythonColorConverter.ToPartColor(color, "color"));
            if (elementTypes != null)
            {
                OrderedDictionary<string, string> values =
                    PythonDictionaryConverter.ToStringStringDictionary(elementTypes, "element_types");
                foreach (var entry in values)
                {
                    FieldInfo field = ResolveElementTypeField(entry.Key);
                    object parsed = ParseElementTypeValue(field, entry.Key, entry.Value);
                    RefuseElementTypeOfAnotherModelSpace(field, entry.Key, parsed);
                    EnsureElementKindPresent(oldPartName, properties, field, entry.Key);
                    field.SetValue(boxed, parsed);
                }
            }
            //
            EnsureCommandsAvailable();
            ReplacePartProperties(oldPartName, (PartProperties)boxed);
        }
        public void SetElementType(string[] partNames, string elementType, string value)
        {
            if (elementType == null || elementType.Trim().Length == 0)
                throw new PythonAPIException("The argument 'element_type' must not be empty.");
            if (value == null || value.Trim().Length == 0)
                throw new PythonAPIException("The argument 'value' must not be empty.");
            //
            FieldInfo field = ResolveElementTypeField(elementType);
            object parsed = ParseElementTypeValue(field, elementType, value);
            RefuseElementTypeOfAnotherModelSpace(field, elementType, parsed);
            foreach (string partName in partNames)
                EnsureElementKindPresent(partName, GetPartProperties(partName), field, elementType);
            //
            EnsureCommandsAvailable();
            foreach (string partName in partNames)
            {
                PartProperties properties = GetPartProperties(partName);
                // PartProperties is a struct, so the field is set on a boxed copy and unboxed back
                object boxed = properties;
                field.SetValue(boxed, parsed);
                ReplacePartProperties(partName, (PartProperties)boxed);
            }
        }
        public void Translate(string[] partNames, object translateVector, int numberOfCopies)
        {
            double[] vector = ToVector(translateVector, "translate_vector");
            ValidateNumberOfCopies(numberOfCopies);
            EnsureTransformable(partNames, "translate");
            EnsureCommandsAvailable();
            TranslateParts(partNames, vector, ToCommandNumberOfCopies(numberOfCopies));
        }
        public void Rotate(string[] partNames, object rotateCenter, object rotateAxis, object rotateAngle,
                           int numberOfCopies)
        {
            double[] center = ToVector(rotateCenter, "rotate_center");
            double[] axis = ToDirection(rotateAxis, "rotate_axis");
            double angleDeg = PythonValueConverter.ToFiniteDouble(rotateAngle, "rotate_angle_deg");
            ValidateNumberOfCopies(numberOfCopies);
            EnsureTransformable(partNames, "rotate");
            EnsureCommandsAvailable();
            RotateParts(partNames, center, axis, angleDeg * Math.PI / 180, ToCommandNumberOfCopies(numberOfCopies));
        }
        public void Mirror(string[] partNames, object mirrorPoint, object mirrorDirection, bool copy)
        {
            double[] point = ToVector(mirrorPoint, "mirror_point");
            double[] direction = ToDirection(mirrorDirection, "mirror_direction");
            EnsureTransformable(partNames, "mirror");
            EnsureCommandsAvailable();
            MirrorParts(partNames, point, direction, copy);
        }
        public void Scale(string[] partNames, object scaleCenter, object scaleFactors, bool copy)
        {
            double[] center = ToVector(scaleCenter, "scale_center");
            double[] factors = ToVector(scaleFactors, "scale_factors");
            EnsureTransformable(partNames, "scale");
            EnsureCommandsAvailable();
            ScaleParts(partNames, center, factors, copy);
        }
        //
        public GeometrySelection BuildGeometrySelection(SelectionAPI selection, string argumentName)
        {
            return BuildGeometrySelection(selection, argumentName, "geometry");
        }
        public GeometrySelection BuildGeometrySelection(SelectionAPI selection, string argumentName,
                                                        params string[] targets)
        {
            if (selection == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            SelectionCompilation compilation = _selectionService.CompileForView(selection, ViewName, targets);
            return new GeometrySelection("Selection", compilation.Ids, compilation.Selection);
        }
        public double[] ToVector(object value, string argumentName)
        {
            double[] vector = PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (vector == null || vector.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            for (int i = 0; i < vector.Length; i++)
            {
                if (double.IsNaN(vector[i]) || double.IsInfinity(vector[i]))
                    throw new PythonAPIException("The argument '" + argumentName + "[" + i + "]' must be finite.");
            }
            //
            return vector;
        }
        public string[] ResolveNames(object partNames)
        {
            return ResolveNames(partNames, "part_names");
        }
        public string[] ResolveNames(object partNames, string argumentName)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(partNames, argumentName);
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be empty.");
            //
            PythonNameComparer.RefuseRepeated(names, argumentName);
            // The view hides and colours a part by the name of its actor, compared exactly, so the names are
            // passed on in the model's spelling
            string[] storedNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++) storedNames[i] = ResolvePart(names[i]).Name;
            //
            return storedNames;
        }
        public void EnsureCommandsAvailable()
        {
            if (_meshGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
        //
        protected abstract FeMesh GetMeshOrNull();
        protected abstract PartProperties GetPartProperties(string partName);
        protected abstract void ReplacePartProperties(string oldPartName, PartProperties properties);
        protected abstract void SetColorForParts(string[] partNames, Color color);
        protected abstract void ResetColorForParts(string[] partNames);
        protected abstract void SetTransparencyForParts(string[] partNames, byte alpha);
        protected abstract void HideParts(string[] partNames);
        protected abstract void ShowParts(string[] partNames);
        protected abstract void RemoveParts(string[] partNames);
        protected abstract void TranslateParts(string[] partNames, double[] translateVector, int numberOfCopies);
        protected abstract void RotateParts(string[] partNames, double[] rotateCenter, double[] rotateAxis,
                                            double rotateAngle, int numberOfCopies);
        protected abstract void MirrorParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection,
                                            bool copy);
        protected abstract void ScaleParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy);
        // A transform the application refuses only after it was recorded is refused here instead
        protected virtual void EnsureTransformable(string[] partNames, string transformName)
        {
        }
        //
        private static void ValidateNumberOfCopies(int numberOfCopies)
        {
            if (numberOfCopies < 0)
                throw new PythonAPIException("The argument 'number_of_copies' must not be negative.");
        }
        // The dialogs record a move as -1 copies, and the Python API writes it as 0
        private static int ToCommandNumberOfCopies(int numberOfCopies)
        {
            return numberOfCopies == 0 ? -1 : numberOfCopies;
        }
        // The find edges dialog limits the angle between neighbouring faces to 0 - 90 degrees
        protected static double ToEdgeAngle(object edgeAngle)
        {
            double angle = PythonValueConverter.ToFiniteDouble(edgeAngle, "edge_angle_deg");
            if (angle < 0 || angle > 90)
                throw new PythonAPIException("The argument 'edge_angle_deg' must be between 0 and 90 degrees.");
            //
            return angle;
        }
        private double[] ToDirection(object value, string argumentName)
        {
            double[] direction = ToVector(value, argumentName);
            if (direction[0] == 0 && direction[1] == 0 && direction[2] == 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be a zero vector.");
            return direction;
        }
        private static FieldInfo ResolveElementTypeField(string elementType)
        {
            foreach (FieldInfo field in typeof(PartProperties).GetFields())
            {
                if (!IsElementTypeField(field)) continue;
                if (string.Equals(ToPythonName(field.Name), elementType.Trim(), StringComparison.OrdinalIgnoreCase))
                    return field;
            }
            //
            throw new PythonAPIException("The element type '" + elementType + "' does not exist. Use " +
                                         "get_element_type_names() to list the supported names.");
        }
        // The type of the part describes the part rather than choosing an element, so it is not a slot
        private static bool IsElementTypeField(FieldInfo field)
        {
            return field.FieldType.IsEnum && field.Name.EndsWith("Type") && field.Name != "PartType";
        }
        // None marks an element kind the part does not contain, so it is neither listed nor settable
        private static bool IsElementKindPresent(FieldInfo field, PartProperties properties)
        {
            return Convert.ToInt32(field.GetValue(properties)) != 0;
        }
        private static void EnsureElementKindPresent(string partName, PartProperties properties, FieldInfo field,
                                                     string elementType)
        {
            if (IsElementKindPresent(field, properties)) return;
            //
            List<string> presentKinds = new List<string>();
            foreach (FieldInfo otherField in typeof(PartProperties).GetFields())
            {
                if (IsElementTypeField(otherField) && IsElementKindPresent(otherField, properties))
                    presentKinds.Add(ToPythonName(otherField.Name));
            }
            string message = "The part '" + partName + "' contains no " + elementType.Trim() +
                             " elements, so their element type cannot be set. ";
            if (presentKinds.Count == 0) message += "The part contains no elements that take an element type.";
            else message += "The part contains " + string.Join(", ", presentKinds) + " elements.";
            throw new PythonAPIException(message);
        }
        // Enum.Parse accepts a number and combines a comma separated list into another member, so only a member
        // name is taken. None is not a value: it marks an absent element kind, and the part ignores it
        // The part dialog offers only the element types of the model space; another one fails in the solver
        private void RefuseElementTypeOfAnotherModelSpace(FieldInfo field, string elementType, object parsed)
        {
            ModelSpaceEnum modelSpace = _modelResolver.GetModel().Properties.ModelSpace;
            HashSet<string> unavailableNames;
            if (modelSpace.GetUnavailableElementTypeNames().TryGetValue(field.FieldType, out unavailableNames) &&
                unavailableNames.Contains(parsed.ToString()))
                throw new PythonAPIException("The value '" + parsed + "' of the element type '" + elementType +
                                             "' cannot be used in a " + modelSpace + " model, as the dialog does " +
                                             "not offer it there.");
        }
        private static object ParseElementTypeValue(FieldInfo field, string elementType, string value)
        {
            object parsed = null;
            foreach (string memberName in Enum.GetNames(field.FieldType))
            {
                if (string.Equals(memberName, value.Trim(), StringComparison.OrdinalIgnoreCase))
                    parsed = Enum.Parse(field.FieldType, memberName);
            }
            if (parsed == null || Convert.ToInt32(parsed) == 0)
            {
                List<string> possibleValues = new List<string>();
                foreach (object member in Enum.GetValues(field.FieldType))
                {
                    if (Convert.ToInt32(member) != 0) possibleValues.Add(member.ToString());
                }
                throw new PythonAPIException("The value '" + value + "' is not supported for the element type '" +
                                             elementType + "'. Possible values are " +
                                             string.Join(", ", possibleValues) + ".");
            }
            //
            return parsed;
        }
        private static string ToPythonName(string fieldName)
        {
            // LinearTetraType becomes linear_tetra
            string name = fieldName.EndsWith("Type") ? fieldName.Substring(0, fieldName.Length - 4) : fieldName;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) builder.Append('_');
                builder.Append(char.ToLowerInvariant(name[i]));
            }
            //
            return builder.ToString();
        }
        private void ValidateNewName(string name, string allowedExistingName, string argumentName)
        {
            PythonNameComparer.ValidateFormat(name, argumentName, allowedExistingName);
            if (PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in _modelResolver.GetAllMeshEntityNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The name '" + name + "' is already used by another model item.");
            }
        }
    }
}
