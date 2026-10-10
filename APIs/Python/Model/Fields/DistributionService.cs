// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
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
        public Distribution BuildFromEquation(string name, object equation, object coordinateSystemName)
        {
            ValidateNewName(name, null);
            Distribution distribution = CreateFromEquation(name, equation);
            ApplyCoordinateSystemName(distribution, coordinateSystemName);
            //
            return distribution;
        }
        // An equation that is given may change a scalar distribution into a vector one, so it builds the
        // distribution anew; the coordinate system that is not given is carried over either way
        public Distribution BuildReplacementFromEquation(string oldName, string name, object equation,
                                                         object coordinateSystemName)
        {
            Distribution existing = ResolveDistribution(oldName);
            EnsureReplaceable(oldName, existing, typeof(DistributionFromEquation), "from_equation");
            string newName = name == null ? existing.Name : name;
            ValidateNewName(newName, existing.Name);
            //
            Distribution distribution = equation == null ? existing.DeepClone() : CreateFromEquation(newName, equation);
            distribution.Name = newName;
            distribution.CoordinateSystemName = existing.CoordinateSystemName;
            ApplyCoordinateSystemName(distribution, coordinateSystemName);
            if (distribution.DistributionType != existing.DistributionType)
                EnsureUnused(existing, "change it from a " + existing.DistributionType + " to a " +
                                       distribution.DistributionType + " distribution");
            //
            return distribution;
        }
        // A replace edits only the form it is named after, so it never turns an equation into a file or back
        private static void EnsureReplaceable(string name, Distribution distribution, Type replacedType,
                                              string itemType)
        {
            if (distribution.GetType() != replacedType)
                throw new PythonAPIException("The distribution '" + name + "' is made by add_" +
                                             DistributionAPI.GetItemType(distribution) + ", so replace_" + itemType +
                                             " cannot replace it. Remove it and add the new type instead.");
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
                                          object interpolatorRadius, object scale, object translate,
                                          object coordinateSystemName)
        {
            ValidateNewName(name, null);
            DistributionFromFile distribution = new DistributionFromFile(name);
            ImportFile(distribution, fileName);
            ApplyMappedValues(distribution, interpolatorType, interpolatorRadius, scale, translate);
            ApplyCoordinateSystemName(distribution, coordinateSystemName);
            //
            return distribution;
        }
        public Distribution BuildReplacementFromFile(string oldName, string name, string fileName,
                                                     string interpolatorType, object interpolatorRadius,
                                                     object scale, object translate, object coordinateSystemName)
        {
            Distribution resolved = ResolveDistribution(oldName);
            EnsureReplaceable(oldName, resolved, typeof(DistributionFromFile), "from_file");
            DistributionFromFile existing = (DistributionFromFile)resolved;
            string newName = name == null ? existing.Name : name;
            ValidateNewName(newName, existing.Name);
            DistributionFromFile distribution = (DistributionFromFile)existing.DeepClone();
            distribution.Name = newName;
            if (fileName != null)
            {
                ImportFile(distribution, fileName);
                if (distribution.DistributionType != existing.DistributionType)
                    EnsureUnused(existing, "change it from a " + existing.DistributionType + " to a " +
                                           distribution.DistributionType + " distribution");
            }
            ApplyMappedValues(distribution, interpolatorType, interpolatorRadius, scale, translate);
            ApplyCoordinateSystemName(distribution, coordinateSystemName);
            //
            return distribution;
        }
        public void Add(Distribution distribution)
        {
            EnsureCommandsAvailable();
            ModelGateway.AddDistribution(distribution);
        }
        // A name is compared without regard to case, and the name the model keys the coordinate system by is
        // stored, because the model looks the coordinate system up by it when the distribution is evaluated
        private void ApplyCoordinateSystemName(Distribution distribution, object coordinateSystemName)
        {
            if (coordinateSystemName == null) return;
            //
            string name = coordinateSystemName as string;
            if (name == null)
                throw new PythonAPIException("The argument 'coordinate_system_name' must be a string.");
            if (PythonNameComparer.AreSame(name, CoordinateSystem.DefaultCoordinateSystemName))
            {
                distribution.CoordinateSystemName = CoordinateSystem.DefaultCoordinateSystemName;
                return;
            }
            //
            // A coordinate system other than the global one belongs to the mesh
            CaeMesh.FeMesh mesh = ModelResolver.GetModel().Mesh;
            if (mesh == null)
                throw new PythonAPIException("The coordinate system '" + name + "' given as " +
                                             "'coordinate_system_name' does not exist, because the model has no " +
                                             "mesh; only '" + CoordinateSystem.DefaultCoordinateSystemName +
                                             "' can be given.");
            foreach (string modelName in mesh.CoordinateSystems.Keys)
            {
                if (!PythonNameComparer.AreSame(modelName, name)) continue;
                //
                distribution.CoordinateSystemName = modelName;
                return;
            }
            throw new PythonAPIException("The coordinate system '" + name + "' given as 'coordinate_system_name' " +
                                         "does not exist.");
        }
        // DistributionFromFile is a MappedDistribution, so the cloud interpolation and the point cloud
        // placement belong to it
        private static void ApplyMappedValues(DistributionFromFile distribution, string interpolatorType,
                                              object interpolatorRadius, object scale, object translate)
        {
            if (interpolatorType != null)
                distribution.InterpolatorType =
                    PythonValueConverter.ToEnum<CloudInterpolatorEnum>(interpolatorType, "interpolator_type");
            // The closest point interpolation has no radius, which the dialog then hides
            if (interpolatorRadius != null && distribution.InterpolatorType == CloudInterpolatorEnum.ClosestPoint)
                throw new PythonAPIException("The argument 'interpolator_radius' is not used with " +
                                             "interpolator_type='ClosestPoint'" +
                                             (interpolatorType == null ? ", which the distribution keeps," : "") +
                                             " as it has no radius. Give interpolator_type='Gauss' or 'Shepard' " +
                                             "to use it.");
            if (interpolatorRadius != null)
                PythonModelArgumentCheck.SetEquation(distribution.InterpolatorRadius, interpolatorRadius,
                                                     "interpolator_radius");
            if (scale != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(scale, "scale", 3);
                PythonModelArgumentCheck.SetEquations(new[] { distribution.ScaleX, distribution.ScaleY,
                                                              distribution.ScaleZ }, values, "scale");
            }
            if (translate != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(translate, "translate", 3);
                PythonModelArgumentCheck.SetEquations(new[] { distribution.TranslateX, distribution.TranslateY,
                                                              distribution.TranslateZ }, values, "translate");
            }
        }
        // Setting the file name imports the file, which decides whether the distribution is a scalar or a vector
        private static void ImportFile(DistributionFromFile distribution, string fileName)
        {
            string fullFileName = ResolveDistributionFileName(fileName);
            PythonModelArgumentCheck.Apply(() => distribution.FileName = fullFileName, "file_name");
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
            string storedName = ResolveDistribution(oldName).Name;
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceDistribution(storedName, distribution);
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
