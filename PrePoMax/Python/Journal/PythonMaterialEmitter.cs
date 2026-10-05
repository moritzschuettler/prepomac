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
            foreach (MaterialProperty property in material.Properties) makers.Add(GetMaker(property));
            //
            return "[" + string.Join(", ", makers) + "]";
        }
        public static string GetMaker(MaterialProperty property)
        {
            if (property is Density density)
                return PythonJournalWriter.Join("pmx.create.density", Column(density.DensityTemp, 0, "density"));
            if (property is ElasticWithDensity elasticWithDensity)
                return PythonJournalWriter.Join("pmx.create.elastic_with_density",
                                                Scalar(elasticWithDensity.YoungsModulus, "youngs_modulus"),
                                                Scalar(elasticWithDensity.PoissonsRatio, "poissons_ratio"),
                                                Scalar(elasticWithDensity.Density, "density"));
            if (property is Elastic elastic)
                return PythonJournalWriter.Join("pmx.create.elastic", Column(elastic.YoungsPoissonsTemp, 0,
                                                                             "youngs_modulus"),
                                                Column(elastic.YoungsPoissonsTemp, 1, "poissons_ratio"));
            if (property is ElasticEngineeringConstants engineeringConstants)
                return PythonJournalWriter.Join("pmx.create.elastic_engineering_constants",
                                                Table(engineeringConstants.EngineeringConstantsTemp,
                                                      "elastic_engineering_constants"));
            if (property is ElasticLamina lamina)
                return PythonJournalWriter.Join("pmx.create.elastic_lamina",
                                                Table(lamina.LaminaConstantsTemp, "elastic_lamina"));
            if (property is Plastic plastic)
                return PythonJournalWriter.Join("pmx.create.plastic", Table(plastic.StressStrainTemp, "plastic"),
                                                "hardening=" + PythonValueFormatter.Text(plastic.Hardening.ToString()));
            if (property is SlipWear slipWear)
                return PythonJournalWriter.Join("pmx.create.slip_wear", Scalar(slipWear.Hardness, "hardness"),
                                                Scalar(slipWear.WearCoefficient, "wear_coefficient"));
            if (property is SpecificHeat specificHeat)
                return PythonJournalWriter.Join("pmx.create.specific_heat",
                                                Column(specificHeat.SpecificHeatTemp, 0, "specific_heat"));
            if (property is ThermalConductivity thermalConductivity)
                return PythonJournalWriter.Join("pmx.create.thermal_conductivity",
                                                Column(thermalConductivity.ThermalConductivityTemp, 0,
                                                       "thermal_conductivity"));
            if (property is ThermalExpansion thermalExpansion)
                return PythonJournalWriter.Join("pmx.create.thermal_expansion",
                                                Column(thermalExpansion.ThermalExpansionTemp, 0, "thermal_expansion"),
                                                GetZeroTemperature(thermalExpansion));
            //
            throw new NotSupportedException("the material property " + property.GetType().Name +
                                            " has no pmx.create maker");
        }
        private static string GetZeroTemperature(ThermalExpansion thermalExpansion)
        {
            if (thermalExpansion.ZeroTemperature == null || thermalExpansion.ZeroTemperature.Value == 0) return null;
            //
            return "zero_temperature=" + Scalar(thermalExpansion.ZeroTemperature, "zero_temperature");
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
        private static bool IsConstant(EquationContainer[][] table)
        {
            return table.Length == 1 && table[0].Length > 0 && table[0][table[0].Length - 1].Value == 0;
        }
        // The makers convert their arguments to plain numbers, so an equation would be silently written
        // out as the value it happened to evaluate to
        private static string Scalar(EquationContainer value, string name)
        {
            if (value == null) throw new NotSupportedException("the material property '" + name + "' is missing");
            if (value.IsEquation())
                throw new NotSupportedException("the material property '" + name +
                                                "' carries the equation '" + value.String +
                                                "', which pmx.create cannot take");
            //
            return PythonValueFormatter.Number(value.Value);
        }
    }
}
