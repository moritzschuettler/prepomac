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
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    // A material property made by pmx.create or read back from a material. Its values are reported under the
    // names of the pmx.create arguments, in the form those arguments take them, so that
    // pmx.create.<property_type>(**property.get_values()) makes the same property again.
    public class MaterialPropertyAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> PropertyTypes = new Dictionary<Type, string>
        {
            { typeof(Elastic), "elastic" },
            { typeof(Density), "density" },
            { typeof(ElasticWithDensity), "elastic_with_density" },
            { typeof(ElasticEngineeringConstants), "elastic_engineering_constants" },
            { typeof(ElasticLamina), "elastic_lamina" },
            { typeof(Plastic), "plastic" },
            { typeof(SlipWear), "slip_wear" },
            { typeof(SpecificHeat), "specific_heat" },
            { typeof(ThermalConductivity), "thermal_conductivity" },
            { typeof(ThermalExpansion), "thermal_expansion" }
        };
        private readonly MaterialProperty _property;


        // Properties
        [PythonAPI("The property type, named after the pmx.create method that makes it, for example elastic.",
                   Returns = "str")]
        public string property_type { get { return GetPropertyType(_property); } }


        // Constructors
        internal MaterialPropertyAPI(MaterialProperty property)
        {
            _property = property;
        }


        // Static methods
        internal static string GetPropertyType(MaterialProperty property)
        {
            string propertyType;
            if (PropertyTypes.TryGetValue(property.GetType(), out propertyType)) return propertyType;
            //
            throw new PythonAPIException("The material property " + property.GetType().Name + " is not made by " +
                                         "any pmx.create method.");
        }
        // A table holds a single row whose temperature is zero when the property was given as plain numbers
        private static void AddColumns(Dictionary<string, object> values, EquationContainer[][] table,
                                       params string[] argumentNames)
        {
            EquationContainer temperature = table.Length == 1 ? table[0][table[0].Length - 1] : null;
            if (temperature != null && !temperature.IsEquation() && temperature.Value == 0)
            {
                for (int i = 0; i < argumentNames.Length; i++) values[argumentNames[i]] = ToValue(table[0][i]);
            }
            else values[argumentNames[0]] = ToTable(table);
        }
        // A value entered as an expression is read back as that expression, with its leading equals sign, so that
        // the maker it goes back into keeps it an expression
        private static object ToValue(EquationContainer value)
        {
            if (value.IsEquation()) return value.String;
            //
            return value.Value;
        }
        // A table with an expression in it is a list of rows, its plain values numbers, because a NumericArray
        // holds numbers only
        private static object ToTable(EquationContainer[][] table)
        {
            bool hasEquation = false;
            foreach (EquationContainer[] row in table)
            {
                foreach (EquationContainer value in row) hasEquation |= value.IsEquation();
            }
            if (!hasEquation) return ToNumericTable(table);
            //
            object[] rows = new object[table.Length];
            for (int i = 0; i < table.Length; i++)
            {
                object[] row = new object[table[i].Length];
                for (int j = 0; j < row.Length; j++) row[j] = ToValue(table[i][j]);
                rows[i] = PythonListConverter.ToPythonList(row);
            }
            //
            return PythonListConverter.ToPythonList(rows);
        }
        private static NumericArrayAPI ToNumericTable(EquationContainer[][] table)
        {
            int columnCount = table.Length == 0 ? 0 : table[0].Length;
            double[,] values = new double[table.Length, columnCount];
            for (int row = 0; row < table.Length; row++)
            {
                for (int column = 0; column < columnCount; column++) values[row, column] = table[row][column].Value;
            }
            //
            return new NumericArrayAPI(values);
        }
        private static string FormatValue(object value)
        {
            if (value is string) return StringListAPI.ToDisplayString((string)value);
            if (value is double) return PythonValueConverter.ToRoundTripString((double)value);
            //
            return value.ToString();
        }


        // Methods
        [PythonAPI("Returns the values of the property under the names of the pmx.create arguments that set " +
                   "them, in the form those arguments take them: a property given as plain numbers returns " +
                   "them one by one, and a table returns its rows, the last value of every row being the " +
                   "temperature, under the first argument. A value entered as an expression is returned as that " +
                   "expression, with its leading equals sign, and a table holding one is returned as a list of " +
                   "rows rather than a NumericArray. Passing the result back as keyword arguments to the " +
                   "pmx.create method named by property_type makes the same property.",
                   Returns = "dict[str, float, str, list or NumericArray]",
                   Example = "pmx.create.elastic(**material.get_properties()[0].get_values())")]
        public Dictionary<string, object> get_values()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            if (_property is Density density) AddColumns(values, density.DensityTemp, "density");
            else if (_property is ElasticWithDensity elasticWithDensity)
            {
                values["youngs_modulus"] = ToValue(elasticWithDensity.YoungsModulus);
                values["poissons_ratio"] = ToValue(elasticWithDensity.PoissonsRatio);
                values["density"] = ToValue(elasticWithDensity.Density);
            }
            else if (_property is Elastic elastic)
                AddColumns(values, elastic.YoungsPoissonsTemp, "youngs_modulus", "poissons_ratio");
            else if (_property is ElasticEngineeringConstants engineeringConstants)
                values["constants"] = ToTable(engineeringConstants.EngineeringConstantsTemp);
            else if (_property is ElasticLamina lamina) values["constants"] = ToTable(lamina.LaminaConstantsTemp);
            else if (_property is Plastic plastic)
            {
                values["stress_strain"] = ToTable(plastic.StressStrainTemp);
                values["hardening"] = plastic.Hardening.ToString();
            }
            else if (_property is SlipWear slipWear)
            {
                values["hardness"] = ToValue(slipWear.Hardness);
                values["wear_coefficient"] = ToValue(slipWear.WearCoefficient);
            }
            else if (_property is SpecificHeat specificHeat)
                AddColumns(values, specificHeat.SpecificHeatTemp, "specific_heat");
            else if (_property is ThermalConductivity thermalConductivity)
                AddColumns(values, thermalConductivity.ThermalConductivityTemp, "thermal_conductivity");
            else if (_property is ThermalExpansion thermalExpansion)
            {
                AddColumns(values, thermalExpansion.ThermalExpansionTemp, "thermal_expansion");
                values["zero_temperature"] = ToValue(thermalExpansion.ZeroTemperature);
            }
            else GetPropertyType(_property);
            //
            return values;
        }

        [PythonAPI("Returns the names get_values reports for this property.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(get_values().Keys);
        }

        [PythonAPI("Returns one value of the property by the name of the pmx.create argument that sets it. Use " +
                   "get_value_names to list them. A value entered as an expression is returned as that " +
                   "expression, as get_values returns it.", Arguments = "name: str",
                   Returns = "float, str, list or NumericArray")]
        public object get_value(string name)
        {
            if (name == null) throw new PythonAPIException("The argument 'name' must not be empty.");
            //
            Dictionary<string, object> values = get_values();
            object value;
            if (!values.TryGetValue(name.Trim(), out value))
                throw new PythonAPIException("The value '" + name + "' is not defined for a " +
                                             GetPropertyType(_property) + " material property. Possible values " +
                                             "are " + string.Join(", ", values.Keys) + ".");
            //
            return value;
        }
        internal MaterialProperty GetPropertyCopy()
        {
            return _property.DeepClone();
        }
        public override string ToString()
        {
            return "MaterialProperty(type=" + GetPropertyType(_property) +
                   string.Concat(get_values().Select(entry => ", " + entry.Key + "=" + FormatValue(entry.Value))) +
                   ")";
        }
    }
}
