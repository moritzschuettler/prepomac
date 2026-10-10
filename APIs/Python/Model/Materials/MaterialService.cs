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
using System.Globalization;
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
            string storedName = material.Name;
            if (name != null)
            {
                ValidateName(name, storedName);
                material.Name = name;
            }
            if (properties != null) ApplyProperties(material, properties);
            if (description != null) material.Description = description;
            if (temperatureDependent != null)
                material.TemperatureDependent =
                    PythonValueConverter.ToBool(temperatureDependent, "temperature_dependent");
            if (properties != null || temperatureDependent != null) CheckRowsAreUsed(material);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceMaterial(storedName, material);
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
            CheckRowsAreUsed(material);
            //
            return material;
        }
        private void ApplyProperties(Material material, object properties)
        {
            List<MaterialPropertyAPI> materialProperties =
                PythonObjectListConverter.ToList<MaterialPropertyAPI>(properties, "properties",
                                                                      "a material property created by pmx.create " +
                                                                      "or read from a material, or a list of them");
            if (materialProperties.Count == 0)
                throw new PythonAPIException("The argument 'properties' must contain at least one material property.");
            //
            // AddProperty rejects a second elastic property with a message that names no argument, and the dialog
            // adds a property type only once
            material.Properties.Clear();
            HashSet<System.Type> propertyTypes = new HashSet<System.Type>();
            MaterialProperty densityProperty = null;
            foreach (MaterialPropertyAPI property in materialProperties)
            {
                MaterialProperty propertyCopy = property.GetPropertyCopy();
                if (!propertyTypes.Add(propertyCopy.GetType()))
                    throw new PythonAPIException("The argument 'properties' contains the " +
                                                 MaterialPropertyAPI.GetPropertyType(propertyCopy) + " property more " +
                                                 "than once; a material holds one property of each type.");
                // The elastic property with density is also the density, which the solver input would get twice
                if (propertyCopy is Density || propertyCopy is ElasticWithDensity)
                {
                    if (densityProperty != null)
                        throw new PythonAPIException("The argument 'properties' contains the " +
                                                     MaterialPropertyAPI.GetPropertyType(densityProperty) +
                                                     " and the " + MaterialPropertyAPI.GetPropertyType(propertyCopy) +
                                                     " property, which both define the density; a material holds " +
                                                     "one density.");
                    densityProperty = propertyCopy;
                }
                PythonModelArgumentCheck.Apply(() => material.AddProperty(propertyCopy), "properties");
            }
        }
        // A material that is not temperature dependent writes no temperature column, and of every property but
        // plastic only the first row, so a table the analysis would cut short is refused
        private static void CheckRowsAreUsed(Material material)
        {
            if (material.TemperatureDependent) return;
            //
            foreach (MaterialProperty property in material.Properties)
            {
                EquationContainer[][] table = GetSingleValuedTable(property);
                if (table == null) continue;
                //
                string propertyType = MaterialPropertyAPI.GetPropertyType(property);
                if (table.Length > 1)
                    throw new PythonAPIException("The " + propertyType + " property has " + table.Length + " rows, " +
                                                 "but a material that is not temperature dependent uses only the " +
                                                 "first one. Give temperature_dependent=True, or a single row.");
                EquationContainer[] row = table[0];
                double temperature = row.Length == 0 ? 0 : row[row.Length - 1].Value;
                if (temperature != 0)
                    throw new PythonAPIException("The " + propertyType + " property has the temperature " +
                                                 temperature.ToString(CultureInfo.InvariantCulture) + ", but a " +
                                                 "material that is not temperature dependent writes no " +
                                                 "temperature. Give temperature_dependent=True, or a temperature " +
                                                 "of 0.");
            }
        }
        private static EquationContainer[][] GetSingleValuedTable(MaterialProperty property)
        {
            if (property is Density density) return density.DensityTemp;
            if (property is Elastic elastic) return elastic.YoungsPoissonsTemp;
            if (property is ElasticEngineeringConstants engineeringConstants)
                return engineeringConstants.EngineeringConstantsTemp;
            if (property is ElasticLamina lamina) return lamina.LaminaConstantsTemp;
            if (property is SpecificHeat specificHeat) return specificHeat.SpecificHeatTemp;
            if (property is ThermalConductivity thermalConductivity) return thermalConductivity.ThermalConductivityTemp;
            if (property is ThermalExpansion thermalExpansion) return thermalExpansion.ThermalExpansionTemp;
            //
            return null;
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
