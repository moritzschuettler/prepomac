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
using CaeResults;
using System;
using System.IO;
using System.Linq;

namespace APIs.Python.Model
{
    internal class DistributionService : FieldItemService
    {
        // Properties
        protected override string ItemTypeName { get { return "distribution"; } }


        // Constructors
        public DistributionService(ModelResolver modelResolver, SelectionService selectionService,
                                   IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, modelGateway)
        {
        }


        // Methods
        public string[] GetNames()
        {
            return ModelResolver.GetModel().Distributions.Keys.ToArray();
        }
        public Distribution GetDistribution(string name)
        {
            if (name == null) return null;
            //
            Distribution distribution;
            if (!ModelResolver.GetModel().Distributions.TryGetValue(name, out distribution)) return null;
            //
            return distribution;
        }
        public Distribution ResolveDistribution(string name)
        {
            Distribution distribution = GetDistribution(name);
            if (distribution == null)
                throw new PythonAPIException("The distribution '" + name + "' does not exist.");
            //
            return distribution;
        }
        public Distribution BuildFromEquation(string name, object equation)
        {
            ValidateNewName(name, null);
            string single = equation as string;
            if (single != null) return new DistributionFromEquation(name, single);
            //
            string[] components = PythonStringListConverter.ToStringArray(equation, "equation");
            if (components == null || components.Length != 3)
                throw new PythonAPIException("The argument 'equation' must be one equation string, or three " +
                                             "equation strings, one per direction.");
            //
            return new DistributionFromEquation(name, components[0], components[1], components[2]);
        }
        public Distribution BuildFromFile(string name, string fileName, string interpolatorType,
                                          object interpolatorRadius, object scale, object translate)
        {
            ValidateNewName(name, null);
            if (fileName == null || fileName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'file_name' must not be empty.");
            //
            string fullFileName;
            try
            {
                fullFileName = Path.GetFullPath(fileName);
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument 'file_name' is invalid: " + ex.Message);
            }
            if (!File.Exists(fullFileName))
                throw new PythonAPIException("The distribution file '" + fullFileName + "' does not exist.");
            //
            DistributionFromFile distribution = new DistributionFromFile(name);
            distribution.FileName = fullFileName;
            // DistributionFromFile is a MappedDistribution, so the cloud interpolation and the point cloud
            // placement belong to it
            if (interpolatorType != null)
                distribution.InterpolatorType =
                    PythonValueConverter.ToEnum<CloudInterpolatorEnum>(interpolatorType, "interpolator_type");
            if (interpolatorRadius != null)
                distribution.InterpolatorRadius.Equation =
                    PythonValueConverter.ToEquationString(interpolatorRadius, "interpolator_radius");
            if (scale != null)
            {
                double[] values = ToVector(scale, "scale");
                distribution.ScaleX.Equation = ToEquation(values[0]);
                distribution.ScaleY.Equation = ToEquation(values[1]);
                distribution.ScaleZ.Equation = ToEquation(values[2]);
            }
            if (translate != null)
            {
                double[] values = ToVector(translate, "translate");
                distribution.TranslateX.Equation = ToEquation(values[0]);
                distribution.TranslateY.Equation = ToEquation(values[1]);
                distribution.TranslateZ.Equation = ToEquation(values[2]);
            }
            //
            return distribution;
        }
        public void Add(Distribution distribution)
        {
            EnsureCommandsAvailable();
            ModelGateway.AddDistribution(distribution);
        }
        public void Replace(string oldName, Distribution distribution)
        {
            ResolveDistribution(oldName);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceDistribution(oldName, distribution);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateDistributions(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveDistributions(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            ModelGateway.RenameModelItem(item, newName, null);
        }
        //
        protected override string[] GetItemNames()
        {
            return GetNames();
        }
        private static double[] ToVector(object value, string argumentName)
        {
            double[] vector = PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (vector == null || vector.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            //
            return vector;
        }
        private static EquationString ToEquation(double value)
        {
            return new EquationString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
