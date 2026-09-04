// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
        public static Density CreateDensity(object density)
        {
            return new Density(ToTable(density, 2, "density", new string[] { "density" }));
        }
        public static Elastic CreateElastic(object youngsModulus, object poissonsRatio)
        {
            return new Elastic(ToTable(youngsModulus, poissonsRatio, 3, "elastic",
                                       new string[] { "youngs_modulus", "poissons_ratio" }));
        }
        public static ElasticWithDensity CreateElasticWithDensity(object youngsModulus, object poissonsRatio,
                                                                  object density)
        {
            return new ElasticWithDensity(PythonValueConverter.ToDouble(youngsModulus, "youngs_modulus"),
                                          PythonValueConverter.ToDouble(poissonsRatio, "poissons_ratio"),
                                          PythonValueConverter.ToDouble(density, "density"));
        }
        public static ElasticEngineeringConstants CreateElasticEngineeringConstants(object constants)
        {
            return new ElasticEngineeringConstants(ToTable(constants, ElasticEngineeringConstants.NumberOfValues,
                                                           "elastic_engineering_constants", null));
        }
        public static ElasticLamina CreateElasticLamina(object constants)
        {
            return new ElasticLamina(ToTable(constants, ElasticLamina.NumberOfValues, "elastic_lamina", null));
        }
        public static Plastic CreatePlastic(object stressStrain)
        {
            return new Plastic(ToTable(stressStrain, 3, "plastic", null));
        }
        public static SlipWear CreateSlipWear(object hardness, object wearCoefficient)
        {
            return new SlipWear(PythonValueConverter.ToDouble(hardness, "hardness"),
                                PythonValueConverter.ToDouble(wearCoefficient, "wear_coefficient"));
        }
        public static SpecificHeat CreateSpecificHeat(object specificHeat)
        {
            return new SpecificHeat(ToTable(specificHeat, 2, "specific_heat", new string[] { "specific_heat" }));
        }
        public static ThermalConductivity CreateThermalConductivity(object thermalConductivity)
        {
            return new ThermalConductivity(ToTable(thermalConductivity, 2, "thermal_conductivity",
                                                   new string[] { "thermal_conductivity" }));
        }
        public static ThermalExpansion CreateThermalExpansion(object thermalExpansion)
        {
            return new ThermalExpansion(ToTable(thermalExpansion, 2, "thermal_expansion",
                                                new string[] { "thermal_expansion" }));
        }
        //
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
