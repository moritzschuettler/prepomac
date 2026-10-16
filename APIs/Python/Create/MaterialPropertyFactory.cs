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
using System;
using System.Collections;
using System.Collections.Generic;

namespace APIs.Python
{
    // Builds the material property parts that pmx.create exposes. A tabular property takes either the
    // values of a single row or a list of rows, where the last column is always the temperature. Every value
    // is a number in model units or an equation string, which the property keeps as the dialog does.
    internal static class MaterialPropertyFactory
    {
        // Methods
        // The model builds a property without checking its values, so every maker checks them here, where the
        // refusal can name the argument, rather than when the command that carries the property runs
        public static Density CreateDensity(object density)
        {
            string[] argumentNames;
            EquationString[][] rows = ToTable(density, 2, "density", new string[] { "density" }, "density",
                                              out argumentNames);
            Density property = new Density(ToPlaceholders(rows));
            SetTable(property.DensityTemp, rows, argumentNames);
            //
            return Checked(property, "density");
        }
        public static Elastic CreateElastic(object youngsModulus, object poissonsRatio)
        {
            if (poissonsRatio == null && !IsTable(youngsModulus))
                throw new PythonAPIException("The argument 'poissons_ratio' is required when 'youngs_modulus' is a " +
                                             "single value.");
            //
            string[] argumentNames;
            EquationString[][] rows = ToTable(youngsModulus, poissonsRatio, 3, "elastic",
                                              new string[] { "youngs_modulus", "poissons_ratio" }, "youngs_modulus",
                                              out argumentNames);
            Elastic elastic = new Elastic(ToPlaceholders(rows));
            SetTable(elastic.YoungsPoissonsTemp, rows, argumentNames);
            //
            return Checked(elastic, "youngs_modulus");
        }
        public static ElasticWithDensity CreateElasticWithDensity(object youngsModulus, object poissonsRatio,
                                                                  object density)
        {
            ElasticWithDensity property = new ElasticWithDensity(0, 0, 0);
            SetValue(property.YoungsModulus, youngsModulus, "youngs_modulus");
            SetValue(property.PoissonsRatio, poissonsRatio, "poissons_ratio");
            SetValue(property.Density, density, "density");
            //
            return property;
        }
        public static ElasticEngineeringConstants CreateElasticEngineeringConstants(object constants)
        {
            string[] argumentNames;
            EquationString[][] rows = ToTable(constants, ElasticEngineeringConstants.NumberOfValues,
                                              "elastic_engineering_constants", null, "constants", out argumentNames);
            ElasticEngineeringConstants property = new ElasticEngineeringConstants(ToPlaceholders(rows));
            SetTable(property.EngineeringConstantsTemp, rows, argumentNames);
            //
            return Checked(property, "constants");
        }
        public static ElasticLamina CreateElasticLamina(object constants)
        {
            string[] argumentNames;
            EquationString[][] rows = ToTable(constants, ElasticLamina.NumberOfValues, "elastic_lamina", null,
                                              "constants", out argumentNames);
            ElasticLamina property = new ElasticLamina(ToPlaceholders(rows));
            SetTable(property.LaminaConstantsTemp, rows, argumentNames);
            //
            return Checked(property, "constants");
        }
        public static Plastic CreatePlastic(object stressStrain, string hardening)
        {
            string[] argumentNames;
            EquationString[][] rows = ToTable(stressStrain, 3, "plastic", null, "stress_strain", out argumentNames);
            Plastic plastic = new Plastic(ToPlaceholders(rows));
            SetTable(plastic.StressStrainTemp, rows, argumentNames);
            Checked(plastic, "stress_strain");
            if (hardening != null)
                plastic.Hardening = PythonValueConverter.ToEnum<PlasticHardening>(hardening, "hardening");
            //
            return plastic;
        }
        public static SlipWear CreateSlipWear(object hardness, object wearCoefficient)
        {
            SlipWear property = new SlipWear(0, 0);
            SetValue(property.Hardness, hardness, "hardness");
            SetValue(property.WearCoefficient, wearCoefficient, "wear_coefficient");
            //
            return property;
        }
        public static CompositeStrength CreateCompositeStrength(object tensileStrength1, object compressiveStrength1,
                                                                object tensileStrength2, object compressiveStrength2,
                                                                object shearStrength12, object shearStrength23,
                                                                object tsaiWuCoefficient)
        {
            CompositeStrength property = new CompositeStrength(0, 0, 0, 0, 0, 0, CompositeStrength.DefaultTsaiWuCoefficient);
            SetValue(property.TensileStrength1, tensileStrength1, "tensile_strength_1");
            SetValue(property.CompressiveStrength1, compressiveStrength1, "compressive_strength_1");
            SetValue(property.TensileStrength2, tensileStrength2, "tensile_strength_2");
            SetValue(property.CompressiveStrength2, compressiveStrength2, "compressive_strength_2");
            SetValue(property.ShearStrength12, shearStrength12, "shear_strength_12");
            SetValue(property.ShearStrength23, shearStrength23, "shear_strength_23");
            if (tsaiWuCoefficient != null) SetValue(property.TsaiWuCoefficient, tsaiWuCoefficient, "tsai_wu_coefficient");
            //
            return property;
        }
        public static SpecificHeat CreateSpecificHeat(object specificHeat)
        {
            string[] argumentNames;
            EquationString[][] rows = ToTable(specificHeat, 2, "specific_heat", new string[] { "specific_heat" },
                                              "specific_heat", out argumentNames);
            SpecificHeat property = new SpecificHeat(ToPlaceholders(rows));
            SetTable(property.SpecificHeatTemp, rows, argumentNames);
            //
            return Checked(property, "specific_heat");
        }
        public static ThermalConductivity CreateThermalConductivity(object thermalConductivity)
        {
            string[] argumentNames;
            EquationString[][] rows = ToTable(thermalConductivity, 2, "thermal_conductivity",
                                              new string[] { "thermal_conductivity" }, "thermal_conductivity",
                                              out argumentNames);
            ThermalConductivity property = new ThermalConductivity(ToPlaceholders(rows));
            SetTable(property.ThermalConductivityTemp, rows, argumentNames);
            //
            return Checked(property, "thermal_conductivity");
        }
        public static ThermalExpansion CreateThermalExpansion(object thermalExpansion, object zeroTemperature)
        {
            string[] argumentNames;
            EquationString[][] rows = ToTable(thermalExpansion, 2, "thermal_expansion",
                                              new string[] { "thermal_expansion" }, "thermal_expansion",
                                              out argumentNames);
            ThermalExpansion property = new ThermalExpansion(ToPlaceholders(rows));
            SetTable(property.ThermalExpansionTemp, rows, argumentNames);
            if (zeroTemperature != null) SetValue(property.ZeroTemperature, zeroTemperature, "zero_temperature");
            //
            return Checked(property, "thermal_expansion");
        }
        //
        private static T Checked<T>(T property, string argumentName) where T : MaterialProperty
        {
            PythonModelArgumentCheck.Apply(property.CheckEquations, argumentName);
            //
            return property;
        }
        // A container checks a value as it is set, so a value the model refuses is refused naming the argument
        private static void SetValue(EquationContainer container, object value, string argumentName)
        {
            EquationString equation = PythonValueConverter.ToEquationString(value, argumentName);
            PythonModelArgumentCheck.SetEquation(container, equation, argumentName);
        }
        // The property is built from zeros, which it takes without a check, so that every value can then be
        // set through the container of its column, with that column's unit and check
        private static double[][] ToPlaceholders(EquationString[][] rows)
        {
            double[][] placeholders = new double[rows.Length][];
            for (int i = 0; i < rows.Length; i++) placeholders[i] = new double[rows[i].Length];
            //
            return placeholders;
        }
        private static void SetTable(EquationContainer[][] table, EquationString[][] rows, string[] argumentNames)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                for (int j = 0; j < rows[i].Length; j++)
                {
                    // A value the single row form does not take stays the zero temperature it was built with
                    if (rows[i][j] == null) continue;
                    //
                    EquationContainer container = table[i][j];
                    EquationString equation = rows[i][j];
                    PythonModelArgumentCheck.SetEquation(container, equation, argumentNames[j]);
                }
            }
        }
        // The argument names say, per column, which argument a refused value was given in
        private static EquationString[][] ToTable(object value, int columnCount, string propertyName,
                                                  string[] valueNames, string tableArgumentName,
                                                  out string[] argumentNames)
        {
            return ToTable(value, null, columnCount, propertyName, valueNames, tableArgumentName, out argumentNames);
        }
        private static EquationString[][] ToTable(object firstValue, object secondValue, int columnCount,
                                                  string propertyName, string[] valueNames, string tableArgumentName,
                                                  out string[] argumentNames)
        {
            argumentNames = new string[columnCount];
            // A single scalar, or a pair of scalars, is the one row form
            if (secondValue != null || !IsTable(firstValue))
            {
                if (valueNames == null)
                    throw new PythonAPIException("The property '" + propertyName +
                                                 "' must be given as a list of rows, where the last value of " +
                                                 "every row is the temperature.");
                //
                EquationString[] row = new EquationString[columnCount];
                row[0] = PythonValueConverter.ToEquationString(firstValue, valueNames[0]);
                argumentNames[0] = valueNames[0];
                if (secondValue != null)
                {
                    row[1] = PythonValueConverter.ToEquationString(secondValue, valueNames[1]);
                    argumentNames[1] = valueNames[1];
                }
                //
                return new EquationString[][] { row };
            }
            //
            for (int i = 0; i < columnCount; i++) argumentNames[i] = tableArgumentName;
            List<object> rows = ToItems(firstValue, propertyName, columnCount);
            try
            {
                if (rows.Count == 0)
                    throw new PythonAPIException("The property '" + propertyName + "' must contain at least one row.");
                //
                EquationString[][] table = new EquationString[rows.Count][];
                for (int i = 0; i < rows.Count; i++)
                {
                    List<object> items = ToItems(rows[i], propertyName, columnCount);
                    DisposeItems(items);
                    if (items.Count != columnCount) throw InvalidRow(propertyName, columnCount);
                    //
                    table[i] = PythonValueConverter.ToEquationArray(rows[i], tableArgumentName + "[" + i + "]",
                                                                    columnCount);
                }
                //
                return table;
            }
            finally
            {
                DisposeItems(rows);
            }
        }
        // The rows of a table and the values of a row come as a Python sequence, a NumericArray read back from a
        // property, or a CLR array
        private static List<object> ToItems(object value, string propertyName, int columnCount)
        {
            if (value == null || value is string) throw InvalidRow(propertyName, columnCount);
            //
            List<object> items = new List<object>();
            PyObject python = value as PyObject;
            if (python != null)
            {
                try
                {
                    using (Py.GIL())
                    {
                        if (python.IsNone() || PyString.IsStringType(python)) throw InvalidRow(propertyName, columnCount);
                        using (PyObject builtins = Py.Import("builtins"))
                        using (PyObject listFunction = builtins.GetAttr("list"))
                        using (PyObject list = listFunction.Invoke(python))
                        {
                            long count = list.Length();
                            for (int i = 0; i < count; i++) items.Add(list.GetItem(i));
                        }
                    }
                }
                catch (PythonAPIException)
                {
                    throw;
                }
                catch (Exception)
                {
                    DisposeItems(items);
                    throw InvalidRow(propertyName, columnCount);
                }
                return items;
            }
            //
            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null) throw InvalidRow(propertyName, columnCount);
            foreach (object item in enumerable) items.Add(item);
            //
            return items;
        }
        private static void DisposeItems(List<object> items)
        {
            if (!items.Exists(item => item is PyObject)) return;
            //
            using (Py.GIL())
            {
                foreach (object item in items)
                {
                    PyObject python = item as PyObject;
                    if (python != null) python.Dispose();
                }
            }
        }
        private static PythonAPIException InvalidRow(string propertyName, int columnCount)
        {
            return new PythonAPIException("Every row of the property '" + propertyName + "' must contain " +
                                          columnCount + " values, the last one being the temperature.");
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
