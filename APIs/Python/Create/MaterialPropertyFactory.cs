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
using Python.Runtime;
using System.Collections;

namespace APIs.Python
{
    // Builds the material property parts that pmx.create exposes. A tabular property takes either the
    // values of a single row or a list of rows, where the last column is always the temperature.
    internal static class MaterialPropertyFactory
    {
        // Methods
        // The model builds a property without checking its values, so every maker checks them here, where the
        // refusal can name the argument, rather than when the command that carries the property runs
        public static Density CreateDensity(object density)
        {
            return Checked(new Density(ToTable(density, 2, "density", new string[] { "density" })), "density");
        }
        public static Elastic CreateElastic(object youngsModulus, object poissonsRatio)
        {
            if (poissonsRatio == null && !IsTable(youngsModulus))
                throw new PythonAPIException("The argument 'poissons_ratio' is required when 'youngs_modulus' is a " +
                                             "single value.");
            //
            Elastic elastic = new Elastic(ToTable(youngsModulus, poissonsRatio, 3, "elastic",
                                                  new string[] { "youngs_modulus", "poissons_ratio" }));
            return Checked(elastic, "youngs_modulus");
        }
        public static ElasticWithDensity CreateElasticWithDensity(object youngsModulus, object poissonsRatio,
                                                                  object density)
        {
            ElasticWithDensity property =
                new ElasticWithDensity(PythonValueConverter.ToDouble(youngsModulus, "youngs_modulus"),
                                       PythonValueConverter.ToDouble(poissonsRatio, "poissons_ratio"),
                                       PythonValueConverter.ToDouble(density, "density"));
            PythonModelArgumentCheck.Apply(property.YoungsModulus.CheckEquation, "youngs_modulus");
            PythonModelArgumentCheck.Apply(property.PoissonsRatio.CheckEquation, "poissons_ratio");
            PythonModelArgumentCheck.Apply(property.Density.CheckEquation, "density");
            //
            return property;
        }
        public static ElasticEngineeringConstants CreateElasticEngineeringConstants(object constants)
        {
            ElasticEngineeringConstants property =
                new ElasticEngineeringConstants(ToTable(constants, ElasticEngineeringConstants.NumberOfValues,
                                                        "elastic_engineering_constants", null));
            return Checked(property, "constants");
        }
        public static ElasticLamina CreateElasticLamina(object constants)
        {
            ElasticLamina property =
                new ElasticLamina(ToTable(constants, ElasticLamina.NumberOfValues, "elastic_lamina", null));
            return Checked(property, "constants");
        }
        public static Plastic CreatePlastic(object stressStrain, string hardening)
        {
            Plastic plastic = Checked(new Plastic(ToTable(stressStrain, 3, "plastic", null)), "stress_strain");
            if (hardening != null)
                plastic.Hardening = PythonValueConverter.ToEnum<PlasticHardening>(hardening, "hardening");
            //
            return plastic;
        }
        public static SlipWear CreateSlipWear(object hardness, object wearCoefficient)
        {
            SlipWear property = new SlipWear(PythonValueConverter.ToDouble(hardness, "hardness"),
                                             PythonValueConverter.ToDouble(wearCoefficient, "wear_coefficient"));
            PythonModelArgumentCheck.Apply(property.Hardness.CheckEquation, "hardness");
            PythonModelArgumentCheck.Apply(property.WearCoefficient.CheckEquation, "wear_coefficient");
            //
            return property;
        }
        public static SpecificHeat CreateSpecificHeat(object specificHeat)
        {
            SpecificHeat property =
                new SpecificHeat(ToTable(specificHeat, 2, "specific_heat", new string[] { "specific_heat" }));
            return Checked(property, "specific_heat");
        }
        public static ThermalConductivity CreateThermalConductivity(object thermalConductivity)
        {
            ThermalConductivity property =
                new ThermalConductivity(ToTable(thermalConductivity, 2, "thermal_conductivity",
                                                new string[] { "thermal_conductivity" }));
            return Checked(property, "thermal_conductivity");
        }
        public static ThermalExpansion CreateThermalExpansion(object thermalExpansion, object zeroTemperature)
        {
            ThermalExpansion property =
                new ThermalExpansion(ToTable(thermalExpansion, 2, "thermal_expansion",
                                             new string[] { "thermal_expansion" }));
            if (zeroTemperature != null)
            {
                EquationString equation = PythonValueConverter.ToEquationString(zeroTemperature, "zero_temperature");
                PythonModelArgumentCheck.Apply(() => property.ZeroTemperature.Equation = equation, "zero_temperature");
            }
            return Checked(property, "thermal_expansion");
        }
        //
        private static T Checked<T>(T property, string argumentName) where T : MaterialProperty
        {
            PythonModelArgumentCheck.Apply(property.CheckEquations, argumentName);
            //
            return property;
        }
        private static double[][] ToTable(object value, int columnCount, string propertyName, string[] valueNames)
        {
            return ToTable(value, null, columnCount, propertyName, valueNames);
        }
        private static double[][] ToTable(object firstValue, object secondValue, int columnCount,
                                          string propertyName, string[] valueNames)
        {
            // A single scalar, or a pair of scalars, is the one row form
            if (secondValue != null || !IsTable(firstValue))
            {
                if (valueNames == null)
                    throw new PythonAPIException("The property '" + propertyName +
                                                 "' must be given as a list of rows, where the last value of " +
                                                 "every row is the temperature.");
                //
                double[] row = new double[columnCount];
                row[0] = PythonValueConverter.ToDouble(firstValue, valueNames[0]);
                if (secondValue != null) row[1] = PythonValueConverter.ToDouble(secondValue, valueNames[1]);
                //
                return new double[][] { row };
            }
            //
            double[][] rows = PythonDoubleListConverter.ToDoubleArray2D(firstValue, propertyName);
            if (rows == null || rows.Length == 0)
                throw new PythonAPIException("The property '" + propertyName + "' must contain at least one row.");
            //
            foreach (double[] row in rows)
            {
                if (row == null || row.Length != columnCount)
                    throw new PythonAPIException("Every row of the property '" + propertyName + "' must contain " +
                                                 columnCount + " values, the last one being the temperature.");
            }
            //
            return rows;
        }
        private static bool IsTable(object value)
        {
            if (value == null) return false;
            if (value is string) return false;
            //
            PyObject pyObject = value as PyObject;
            if (pyObject != null)
            {
                using (Py.GIL())
                {
                    return !PyFloat.IsFloatType(pyObject) && !PyInt.IsIntType(pyObject);
                }
            }
            //
            return value is IEnumerable;
        }
    }
}
