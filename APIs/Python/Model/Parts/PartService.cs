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
            Color colorValue = PythonColorConverter.ToColor(color, "color");
            EnsureCommandsAvailable();
            SetColorForParts(partNames, colorValue);
        }
        public void ResetColor(string[] partNames)
        {
            EnsureCommandsAvailable();
            ResetColorForParts(partNames);
        }
        public void SetTransparency(string[] partNames, object transparency)
        {
            byte alpha = PythonColorConverter.ToAlpha(transparency, "transparency");
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
            ValidateNewName(newName, partName);
            PartProperties properties = part.GetProperties();
            properties.Name = newName;
            //
            EnsureCommandsAvailable();
            ReplacePartProperties(partName, properties);
        }
        public StringListAPI GetElementTypeNames()
        {
            List<string> names = new List<string>();
            foreach (FieldInfo field in typeof(PartProperties).GetFields())
            {
                if (field.FieldType.IsEnum && field.Name.EndsWith("Type")) names.Add(ToPythonName(field.Name));
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            //
            return new StringListAPI(names.ToArray());
        }
        public StringDoubleMapAPI GetElementTypes(string partName)
        {
            ResolvePart(partName);
            PartProperties properties = GetPartProperties(partName);
            OrderedDictionary<string, double> values =
                new OrderedDictionary<string, double>("Element types", StringComparer.OrdinalIgnoreCase);
            foreach (FieldInfo field in typeof(PartProperties).GetFields())
            {
                if (field.FieldType.IsEnum && field.Name.EndsWith("Type"))
                    values.Add(ToPythonName(field.Name), Convert.ToDouble(field.GetValue(properties)));
            }
            //
            return new StringDoubleMapAPI(values);
        }
        // A part property dialog changes the name, the colour and the element types together, and the
        // history records that as one command, so the API applies them in one call too
        public void ReplaceProperties(string oldPartName, string name, object color, object elementTypes)
        {
            ResolvePart(oldPartName);
            PartProperties properties = GetPartProperties(oldPartName);
            object boxed = properties;
            //
            if (name != null)
            {
                if (name.Trim().Length == 0)
                    throw new PythonAPIException("The argument 'name' must not be empty.");
                typeof(PartProperties).GetProperty("Name").SetValue(boxed, name.Trim(), null);
            }
            if (color != null)
                typeof(PartProperties).GetField("Color").SetValue(boxed, PythonColorConverter.ToColor(color, "color"));
            if (elementTypes != null)
            {
                OrderedDictionary<string, string> values =
                    PythonDictionaryConverter.ToStringStringDictionary(elementTypes, "element_types");
                foreach (var entry in values)
                {
                    FieldInfo field = ResolveElementTypeField(entry.Key);
                    object parsed;
                    try
                    {
                        parsed = Enum.Parse(field.FieldType, entry.Value.Trim(), true);
                    }
                    catch (Exception)
                    {
                        throw new PythonAPIException("The value '" + entry.Value + "' is not supported for the " +
                                                     "element type '" + entry.Key + "'. Possible values are " +
                                                     string.Join(", ", Enum.GetNames(field.FieldType)) + ".");
                    }
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
            EnsureCommandsAvailable();
            foreach (string partName in partNames)
            {
                PartProperties properties = GetPartProperties(partName);
                object parsed;
                try
                {
                    parsed = Enum.Parse(field.FieldType, value.Trim(), true);
                }
                catch (Exception)
                {
                    throw new PythonAPIException("The value '" + value + "' is not supported for the element type '" +
                                                 elementType + "'. Possible values are " +
                                                 string.Join(", ", Enum.GetNames(field.FieldType)) + ".");
                }
                // PartProperties is a struct, so the field is set on a boxed copy and unboxed back
                object boxed = properties;
                field.SetValue(boxed, parsed);
                ReplacePartProperties(partName, (PartProperties)boxed);
            }
        }
        public void Scale(string[] partNames, object scaleCenter, object scaleFactors, bool copy)
        {
            double[] center = ToVector(scaleCenter, "scale_center");
            double[] factors = ToVector(scaleFactors, "scale_factors");
            EnsureCommandsAvailable();
            ScaleParts(partNames, center, factors, copy);
        }
        //
        public GeometrySelection BuildGeometrySelection(SelectionAPI selection, string argumentName)
        {
            if (selection == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be None.");
            //
            SelectionCompilation compilation = _selectionService.CompileForView(selection, ViewName, "geometry");
            return new GeometrySelection("Selection", compilation.Ids, compilation.Selection);
        }
        public double[] ToVector(object value, string argumentName)
        {
            double[] vector = PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (vector == null || vector.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            //
            return vector;
        }
        public string[] ResolveNames(object partNames)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(partNames, "part_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'part_names' must not be empty.");
            //
            foreach (string name in names) ResolvePart(name);
            //
            return names;
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
        protected abstract void ScaleParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy);
        //
        private static FieldInfo ResolveElementTypeField(string elementType)
        {
            foreach (FieldInfo field in typeof(PartProperties).GetFields())
            {
                if (!field.FieldType.IsEnum || !field.Name.EndsWith("Type")) continue;
                if (string.Equals(ToPythonName(field.Name), elementType.Trim(), StringComparison.OrdinalIgnoreCase))
                    return field;
            }
            //
            throw new PythonAPIException("The element type '" + elementType + "' does not exist. Use " +
                                         "get_element_type_names() to list the supported names.");
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
        private void ValidateNewName(string name, string allowedExistingName)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'new_name' must not be empty.");
            if (string.Equals(name, allowedExistingName)) return;
            //
            foreach (string existingName in _modelResolver.GetAllMeshEntityNames())
            {
                if (string.Equals(name, existingName))
                    throw new PythonAPIException("The name '" + name + "' is already used by another model item.");
            }
        }
    }
}
