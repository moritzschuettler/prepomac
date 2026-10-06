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
using System.Collections.Generic;
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
            return CreateFromEquation(name, equation);
        }
        // An equation that is given may change a scalar distribution into a vector one or replace one read from
        // a file, so it builds the distribution anew; the coordinate system, which the API cannot set, is
        // carried over either way
        public Distribution BuildReplacementFromEquation(string oldName, string name, object equation)
        {
            Distribution existing = ResolveDistribution(oldName);
            string newName = name == null ? oldName : name;
            ValidateNewName(newName, oldName);
            //
            Distribution distribution;
            if (equation != null) distribution = CreateFromEquation(newName, equation);
            else if (existing is DistributionFromEquation) distribution = existing.DeepClone();
            else throw new PythonAPIException("The distribution '" + oldName + "' is read from a file, so the " +
                                              "argument 'equation' must be given to replace it.");
            distribution.Name = newName;
            distribution.CoordinateSystemName = existing.CoordinateSystemName;
            if (distribution.DistributionType != existing.DistributionType)
                EnsureUnused(existing, "change it from a " + existing.DistributionType + " to a " +
                                       distribution.DistributionType + " distribution");
            //
            return distribution;
        }
        // An item that takes a distribution was checked against the type it had when the item was defined, and the
        // model reports a mismatch only when the deck is written, so the type of a used distribution is kept
        private void EnsureUnused(Distribution distribution, string change)
        {
            List<string> users = new List<string>();
            FeModel model = ModelResolver.GetModel();
            foreach (InitialCondition condition in model.InitialConditions.Values)
                AddUser(users, condition as IDistribution, distribution, "initial condition " + condition.Name);
            foreach (Step step in model.StepCollection.StepsList)
            {
                string inStep = " in step " + step.Name;
                foreach (BoundaryCondition condition in step.BoundaryConditions.Values)
                    AddUser(users, condition as IDistribution, distribution,
                            "boundary condition " + condition.Name + inStep);
                foreach (Load load in step.Loads.Values)
                    AddUser(users, load as IDistribution, distribution, "load " + load.Name + inStep);
                foreach (DefinedField field in step.DefinedFields.Values)
                    AddUser(users, field as IDistribution, distribution, "defined field " + field.Name + inStep);
            }
            if (users.Count > 0)
                throw new PythonAPIException("The distribution '" + distribution.Name + "' is used by the " +
                                             string.Join(", ", users) + ", so the replacement cannot " + change +
                                             ".");
        }
        private static void AddUser(List<string> users, IDistribution user, Distribution distribution, string label)
        {
            if (user != null && PythonNameComparer.AreSame(user.DistributionName, distribution.Name))
                users.Add(label);
        }
        private static Distribution CreateFromEquation(string name, object equation)
        {
            string single = equation as string;
            if (single != null) return new DistributionFromEquation(name, ToEquationText(single));
            //
            string[] components = PythonStringListConverter.ToStringArray(equation, "equation");
            if (components == null || components.Length != 3)
                throw new PythonAPIException("The argument 'equation' must be one equation string, or three " +
                                             "equation strings, one per direction.");
            //
            return new DistributionFromEquation(name, ToEquationText(components[0]),
                                                ToEquationText(components[1]),
                                                ToEquationText(components[2]));
        }
        // A distribution keeps its equation as the plain text it was given rather than in an
        // EquationContainer, so the leading equals sign the application expects is supplied here
        private static string ToEquationText(string equation)
        {
            return PythonValueConverter.ToEquationString(equation, "equation").Equation;
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
