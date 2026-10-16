// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using CaeGlobals;
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // A material is a name and a list of properties, and every property type has its own pmx.create
    // maker. A property table whose single row carries no temperature is emitted in the short scalar
    // form the maker also accepts, because that is what a user writes.
    public static class PythonMaterialEmitter
    {
        // Methods                                                                                                                  
        public static void Add(PythonJournalWriter writer, Material material)
        {
            writer.Statement(writer.Call("pmx.model.materials", "add", PythonValueFormatter.Text(material.Name),
                                         GetProperties(material), GetDescription(material, false),
                                         GetTemperatureDependent(material, false)));
            PythonModelItemEmitter.EmitState(writer, "pmx.model.materials", material);
            writer.CountEmittedCommand();
        }
        // A replace keeps a value it is not given, so a cleared description and a material that is no longer
        // temperature dependent are written out
        public static void Replace(PythonJournalWriter writer, string oldName, Material material)
        {
            writer.Statement(writer.Call("pmx.model.materials", "replace", PythonValueFormatter.Text(oldName),
                                         PythonValueFormatter.Text(material.Name), GetProperties(material),
                                         GetDescription(material, true), GetTemperatureDependent(material, true)));
            PythonModelItemEmitter.EmitState(writer, "pmx.model.materials", material);
            writer.CountEmittedCommand();
        }
        private static string GetDescription(Material material, bool replace)
        {
            if (!replace && string.IsNullOrEmpty(material.Description)) return null;
            //
            return "description=" + PythonValueFormatter.Text(material.Description ?? "");
        }
        private static string GetTemperatureDependent(Material material, bool replace)
        {
            if (material.TemperatureDependent) return "temperature_dependent=True";
            //
            return replace ? "temperature_dependent=False" : null;
        }
        private static string GetProperties(Material material)
        {
            List<string> makers = new List<string>();
            foreach (MaterialProperty property in material.Properties)
                makers.Add(GetMaker(property, material.TemperatureDependent));
            //
            return "[" + string.Join(", ", makers) + "]";
        }
        public static string GetMaker(MaterialProperty property, bool temperatureDependent)
        {
            if (property is Density density)
                return PythonJournalWriter.Join("pmx.create.density",
                                                Column(Used(density.DensityTemp, temperatureDependent, true), 0,
                                                       "density"));
            if (property is ElasticWithDensity elasticWithDensity)
                return PythonJournalWriter.Join("pmx.create.elastic_with_density",
                                                Scalar(elasticWithDensity.YoungsModulus, "youngs_modulus"),
                                                Scalar(elasticWithDensity.PoissonsRatio, "poissons_ratio"),
                                                Scalar(elasticWithDensity.Density, "density"));
            // A table of rows is given whole as the first argument, so the second one is left out
            if (property is Elastic elastic)
            {
                EquationContainer[][] youngsPoissonsTemp = Used(elastic.YoungsPoissonsTemp, temperatureDependent, true);
                if (!IsConstant(youngsPoissonsTemp))
                    return PythonJournalWriter.Join("pmx.create.elastic", Table(youngsPoissonsTemp, "elastic"));
                return PythonJournalWriter.Join("pmx.create.elastic", Column(youngsPoissonsTemp, 0, "youngs_modulus"),
                                                Column(youngsPoissonsTemp, 1, "poissons_ratio"));
            }
            if (property is ElasticEngineeringConstants engineeringConstants)
                return PythonJournalWriter.Join("pmx.create.elastic_engineering_constants",
                                                Table(Used(engineeringConstants.EngineeringConstantsTemp,
                                                           temperatureDependent, true),
                                                      "elastic_engineering_constants"));
            if (property is ElasticLamina lamina)
                return PythonJournalWriter.Join("pmx.create.elastic_lamina",
                                                Table(Used(lamina.LaminaConstantsTemp, temperatureDependent, true),
                                                      "elastic_lamina"));
            if (property is Plastic plastic)
                return PythonJournalWriter.Join("pmx.create.plastic",
                                                Table(Used(plastic.StressStrainTemp, temperatureDependent, false),
                                                      "plastic"),
                                                "hardening=" + PythonValueFormatter.Text(plastic.Hardening.ToString()));
            if (property is SlipWear slipWear)
                return PythonJournalWriter.Join("pmx.create.slip_wear", Scalar(slipWear.Hardness, "hardness"),
                                                Scalar(slipWear.WearCoefficient, "wear_coefficient"));
            if (property is CompositeStrength strength)
                return PythonJournalWriter.Join("pmx.create.composite_strength",
                                                Scalar(strength.TensileStrength1, "tensile_strength_1"),
                                                Scalar(strength.CompressiveStrength1, "compressive_strength_1"),
                                                Scalar(strength.TensileStrength2, "tensile_strength_2"),
                                                Scalar(strength.CompressiveStrength2, "compressive_strength_2"),
                                                Scalar(strength.ShearStrength12, "shear_strength_12"),
                                                Scalar(strength.ShearStrength23, "shear_strength_23"),
                                                Scalar(strength.TsaiWuCoefficient, "tsai_wu_coefficient"));
            if (property is SpecificHeat specificHeat)
                return PythonJournalWriter.Join("pmx.create.specific_heat",
                                                Column(Used(specificHeat.SpecificHeatTemp, temperatureDependent, true),
                                                       0, "specific_heat"));
            if (property is ThermalConductivity thermalConductivity)
                return PythonJournalWriter.Join("pmx.create.thermal_conductivity",
                                                Column(Used(thermalConductivity.ThermalConductivityTemp,
                                                            temperatureDependent, true),
                                                       0, "thermal_conductivity"));
            if (property is ThermalExpansion thermalExpansion)
                return PythonJournalWriter.Join("pmx.create.thermal_expansion",
                                                Column(Used(thermalExpansion.ThermalExpansionTemp, temperatureDependent,
                                                            true),
                                                       0, "thermal_expansion"),
                                                GetZeroTemperature(thermalExpansion));
            //
            throw new NotSupportedException("the material property " + property.GetType().Name +
                                            " has no pmx.create maker");
        }
        // The maker defaults the zero temperature to 20, so it is always written, an expression included whatever it
        // evaluates to
        private static string GetZeroTemperature(ThermalExpansion thermalExpansion)
        {
            if (thermalExpansion.ZeroTemperature == null) return null;
            //
            return "zero_temperature=" + Scalar(thermalExpansion.ZeroTemperature, "zero_temperature");
        }
        // A material that is not temperature dependent writes no temperature, and only the first row of a
        // property other than plastic, so the rows are emitted the way the analysis uses them; pmx.create
        // refuses the others for such a material
        private static EquationContainer[][] Used(EquationContainer[][] table, bool temperatureDependent,
                                                  bool firstRowOnly)
        {
            if (temperatureDependent || table == null || table.Length == 0) return table;
            //
            int rowCount = firstRowOnly ? 1 : table.Length;
            EquationContainer[][] used = new EquationContainer[rowCount][];
            for (int i = 0; i < rowCount; i++)
            {
                used[i] = (EquationContainer[])table[i].Clone();
                if (used[i].Length > 0) used[i][used[i].Length - 1] = new EquationContainer(typeof(StringDoubleConverter), 0);
            }
            //
            return used;
        }
        // A single row whose temperature is zero is what a constant property looks like, and the maker
        // takes that as a plain number
        private static string Column(EquationContainer[][] table, int column, string name)
        {
            if (table == null || table.Length == 0)
                throw new NotSupportedException("the material property '" + name + "' carries no values");
            //
            if (IsConstant(table)) return Scalar(table[0][column], name);
            //
            return Table(table, name);
        }
        private static string Table(EquationContainer[][] table, string name)
        {
            if (table == null || table.Length == 0)
                throw new NotSupportedException("the material property '" + name + "' carries no values");
            //
            List<string> rows = new List<string>();
            foreach (EquationContainer[] row in table)
            {
                List<string> values = new List<string>();
                foreach (EquationContainer value in row) values.Add(Scalar(value, name));
                rows.Add("[" + string.Join(", ", values) + "]");
            }
            //
            return "[" + string.Join(", ", rows) + "]";
        }
        // A temperature entered as an expression is kept in the table form, which is the only one that carries it
        private static bool IsConstant(EquationContainer[][] table)
        {
            if (table.Length != 1 || table[0].Length == 0) return false;
            //
            EquationContainer temperature = table[0][table[0].Length - 1];
            return !temperature.IsEquation() && temperature.Value == 0;
        }
        // The makers take an equation string as the dialog takes an expression, so it is written as one
        private static string Scalar(EquationContainer value, string name)
        {
            if (value == null) throw new NotSupportedException("the material property '" + name + "' is missing");
            //
            return PythonValueFormatter.Equation(value);
        }
    }
}
