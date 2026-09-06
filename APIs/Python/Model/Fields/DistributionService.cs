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
            DistributionFromFile distribution = new DistributionFromFile(name);
            distribution.FileName = ResolveDistributionFileName(fileName);
            ApplyMappedValues(distribution, interpolatorType, interpolatorRadius, scale, translate);
            //
            return distribution;
        }
        public Distribution BuildReplacementFromFile(string oldName, string name, string fileName,
                                                     string interpolatorType, object interpolatorRadius,
                                                     object scale, object translate)
        {
            DistributionFromFile existing = ResolveDistribution(oldName) as DistributionFromFile;
            if (existing == null)
                throw new PythonAPIException("The distribution '" + oldName + "' is not read from a file.");
            //
            string newName = name == null ? oldName : name;
            ValidateNewName(newName, oldName);
            DistributionFromFile distribution = (DistributionFromFile)existing.DeepClone();
            distribution.Name = newName;
            if (fileName != null) distribution.FileName = ResolveDistributionFileName(fileName);
            ApplyMappedValues(distribution, interpolatorType, interpolatorRadius, scale, translate);
            //
            return distribution;
        }
        public void Add(Distribution distribution)
        {
            EnsureCommandsAvailable();
            ModelGateway.AddDistribution(distribution);
        }
        // DistributionFromFile is a MappedDistribution, so the cloud interpolation and the point cloud
        // placement belong to it
        private static void ApplyMappedValues(DistributionFromFile distribution, string interpolatorType,
                                              object interpolatorRadius, object scale, object translate)
        {
            if (interpolatorType != null)
                distribution.InterpolatorType =
                    PythonValueConverter.ToEnum<CloudInterpolatorEnum>(interpolatorType, "interpolator_type");
            if (interpolatorRadius != null)
                distribution.InterpolatorRadius.Equation =
                    PythonValueConverter.ToEquationString(interpolatorRadius, "interpolator_radius");
            if (scale != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(scale, "scale", 3);
                distribution.ScaleX.Equation = values[0];
                distribution.ScaleY.Equation = values[1];
                distribution.ScaleZ.Equation = values[2];
            }
            if (translate != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(translate, "translate", 3);
                distribution.TranslateX.Equation = values[0];
                distribution.TranslateY.Equation = values[1];
                distribution.TranslateZ.Equation = values[2];
            }
        }
        private static string ResolveDistributionFileName(string fileName)
        {
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
            return fullFileName;
        }
        public Distribution Clone(string name)
        {
            return ResolveDistribution(name).DeepClone();
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
    }
}
