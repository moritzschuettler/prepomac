// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    internal class MaterialService
    {
        // Variables
        // The characters Material adds to the ones every name may contain
        private static readonly HashSet<char> _additionalNameCharacters = new HashSet<char>() { '@' };
        private readonly ModelResolver _modelResolver;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public MaterialService(ModelResolver modelResolver, IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            return _modelResolver.GetModel().Materials.Keys.ToArray();
        }
        public Material GetMaterial(string name)
        {
            if (name == null) return null;
            //
            Material material;
            if (!_modelResolver.GetModel().Materials.TryGetValue(name, out material)) return null;
            //
            return material;
        }
        public Material ResolveMaterial(string name)
        {
            Material material = GetMaterial(name);
            if (material == null) throw new PythonAPIException("The material '" + name + "' does not exist.");
            //
            return material;
        }
        public void Add(string name, object properties, string description, object temperatureDependent)
        {
            ValidateName(name, null);
            Material material = Build(name, properties, description, temperatureDependent);
            //
            EnsureCommandsAvailable();
            _modelGateway.AddMaterial(material);
        }
        public Material Replace(string oldName, string name, object properties, string description,
                                object temperatureDependent)
        {
            Material material = ResolveMaterial(oldName).DeepClone();
            if (name != null)
            {
                ValidateName(name, oldName);
                material.Name = name;
            }
            if (properties != null) ApplyProperties(material, properties);
            if (description != null) material.Description = description;
            if (temperatureDependent != null)
                material.TemperatureDependent =
                    PythonValueConverter.ToBool(temperatureDependent, "temperature_dependent");
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceMaterial(oldName, material);
            //
            return material;
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.DuplicateMaterials(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveMaterials(names);
        }
        public void SetActive(NamedClass[] items, bool active)
        {
            EnsureCommandsAvailable();
            // Every step name is null, because these items do not belong to a step
            _modelGateway.ActivateDeactivateItems(items, active, new string[items.Length]);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            _modelGateway.RenameModelItem(item, newName, null);
        }
        //
        private Material Build(string name, object properties, string description, object temperatureDependent)
        {
            Material material = new Material(name);
            ApplyProperties(material, properties);
            if (description != null) material.Description = description;
            if (temperatureDependent != null)
                material.TemperatureDependent =
                    PythonValueConverter.ToBool(temperatureDependent, "temperature_dependent");
            //
            return material;
        }
        private void ApplyProperties(Material material, object properties)
        {
            List<MaterialProperty> materialProperties =
                PythonObjectListConverter.ToList<MaterialProperty>(properties, "properties",
                                                                   "a material property created by pmx.create, " +
                                                                   "or a list of them");
            if (materialProperties.Count == 0)
                throw new PythonAPIException("The argument 'properties' must contain at least one material property.");
            //
            // AddProperty rejects a second elastic property, so the model does the domain check
            material.Properties.Clear();
            foreach (MaterialProperty property in materialProperties) material.AddProperty(property);
        }
        private void ValidateName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName, _additionalNameCharacters);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The material '" + name + "' already exists.");
            }
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
