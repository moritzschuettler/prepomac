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
    public class MaterialPropertyAPI : DetachedPartAPI
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
            { typeof(CompositeStrength), "composite_strength" },
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


        // Methods
        // A property given as plain numbers returns them one by one, and a table returns its rows, the last value of
        // every row being the temperature, under the first argument
        protected override Dictionary<string, object> GetValues()
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
            else if (_property is CompositeStrength strength)
            {
                values["tensile_strength_1"] = ToValue(strength.TensileStrength1);
                values["compressive_strength_1"] = ToValue(strength.CompressiveStrength1);
                values["tensile_strength_2"] = ToValue(strength.TensileStrength2);
                values["compressive_strength_2"] = ToValue(strength.CompressiveStrength2);
                values["shear_strength_12"] = ToValue(strength.ShearStrength12);
                values["shear_strength_23"] = ToValue(strength.ShearStrength23);
                values["tsai_wu_coefficient"] = ToValue(strength.TsaiWuCoefficient);
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
        protected override string GetTypeName()
        {
            return "MaterialProperty";
        }
        protected override string GetMakerName()
        {
            return GetPropertyType(_property);
        }
        protected override string GetPartDescription()
        {
            return GetPropertyType(_property) + " material property";
        }
        internal MaterialProperty GetPropertyCopy()
        {
            return _property.DeepClone();
        }
        public override string ToString()
        {
            return "MaterialProperty(type=" + GetPropertyType(_property) +
                   string.Concat(GetValues().Select(entry => ", " + entry.Key + "=" + FormatValue(entry.Value))) +
                   ")";
        }
    }
}
