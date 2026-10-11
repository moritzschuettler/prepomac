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
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    // What every step scoped item needs: the step lookup, the region, the names it refers to, and the
    // name uniqueness rule, which is per step. The commands themselves differ per item type and stay
    // in the derived service.
    internal abstract class StepItemService<TItem> where TItem : NamedClass
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly StepService _stepService;
        private readonly IPythonModelGateway _modelGateway;
        private readonly string _itemTypeName;
        // Only the item types that read a file - loads and defined fields from a file - are given one
        private readonly PythonFileNameResolver _fileNameResolver;


        // Properties
        protected ModelResolver ModelResolver { get { return _modelResolver; } }
        protected IPythonModelGateway ModelGateway { get { return _modelGateway; } }
        protected StepService StepService { get { return _stepService; } }


        // Constructors
        protected StepItemService(ModelResolver modelResolver, SelectionService selectionService,
                                  StepService stepService, IPythonModelGateway modelGateway, string itemTypeName,
                                  PythonFileNameResolver fileNameResolver = null)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _stepService = stepService;
            _modelGateway = modelGateway;
            _itemTypeName = itemTypeName;
            _fileNameResolver = fileNameResolver;
        }


        // Methods
        public string[] GetNames(string stepName)
        {
            return GetItems(_stepService.ResolveStep(stepName)).Keys.ToArray();
        }
        public TItem GetItem(string stepName, string name)
        {
            if (name == null) return null;
            //
            TItem item;
            if (!GetItems(_stepService.ResolveStep(stepName)).TryGetValue(name, out item)) return null;
            //
            return item;
        }
        public TItem ResolveItem(string stepName, string name)
        {
            TItem item = GetItem(stepName, name);
            if (item == null)
                throw new PythonAPIException("The " + _itemTypeName + " '" + name + "' does not exist in the " +
                                             "step '" + stepName + "'.");
            //
            return item;
        }
        // The type cannot change on a replace, the same way the dialog only edits the type it opened
        public TTyped CloneForReplace<TTyped>(string stepName, string name, string itemType) where TTyped : TItem
        {
            TItem item = ResolveItem(stepName, name);
            if (item.GetType() != typeof(TTyped))
                throw new PythonAPIException("The " + _itemTypeName + " '" + name + "' is made by add_" +
                                             GetItemType(item) + ", so replace_" + itemType + " cannot " +
                                             "replace it. Remove it and add the new type instead.");
            //
            return ((TTyped)item).DeepClone();
        }
        public abstract string GetItemType(TItem item);
        public bool GetIsTwoDimensional()
        {
            ModelSpaceEnum modelSpace = _modelResolver.GetModel().Properties.ModelSpace;
            return modelSpace != ModelSpaceEnum.ThreeD && modelSpace != ModelSpaceEnum.Undefined;
        }
        public bool GetIsAxisymmetric()
        {
            return _modelResolver.GetModel().Properties.ModelSpace == ModelSpaceEnum.Axisymmetric;
        }
        // The dialog derives the complex flag from the step type, so the API derives it too
        public bool GetIsComplex(string stepName)
        {
            return _stepService.ResolveStep(stepName) is SteadyStateDynamicsStep;
        }
        public void ApplyRegion(IMultiRegion item, object region, string target)
        {
            RegionApplier.Apply(item, region, "region", target, _selectionService, _modelResolver);
        }
        public void ApplyRegion(IMultiRegion item, object region, string[] targets)
        {
            RegionApplier.Apply(item, region, "region", targets, _selectionService, _modelResolver);
        }
        public void ApplyRegion(IMultiRegion item, object region, string[] selectionTargets,
                                string[] namedRegionTargets)
        {
            RegionApplier.Apply(item, region, "region", selectionTargets, namedRegionTargets, _selectionService,
                                _modelResolver);
        }
        // A name is compared without regard to case, and the name the model keys the item by is stored, because
        // the default names are matched exactly wherever the model reads them
        public string ResolveAmplitudeName(object amplitudeName, string argumentName)
        {
            string name = AsString(amplitudeName, argumentName);
            if (PythonNameComparer.AreSame(name, Amplitude.DefaultAmplitudeName)) return Amplitude.DefaultAmplitudeName;
            //
            string modelName = FindKey(_modelResolver.GetModel().Amplitudes.Keys, name);
            if (modelName == null) throw new PythonAPIException("The amplitude '" + name + "' does not exist.");
            //
            return modelName;
        }
        public string ResolveCoordinateSystemName(object coordinateSystemName)
        {
            string name = AsString(coordinateSystemName, "coordinate_system_name");
            if (PythonNameComparer.AreSame(name, CoordinateSystem.DefaultCoordinateSystemName))
                return CoordinateSystem.DefaultCoordinateSystemName;
            //
            CaeMesh.FeMesh mesh = _modelResolver.GetModel().Mesh;
            string modelName = mesh == null ? null : FindKey(mesh.CoordinateSystems.Keys, name);
            if (modelName == null)
                throw new PythonAPIException("The coordinate system '" + name + "' does not exist.");
            //
            return modelName;
        }
        public string ResolveDistributionName(object distributionName)
        {
            string name = AsString(distributionName, "distribution_name");
            if (PythonNameComparer.AreSame(name, Distribution.DefaultDistributionName))
                return Distribution.DefaultDistributionName;
            //
            string modelName = FindKey(_modelResolver.GetModel().Distributions.Keys, name);
            if (modelName == null) throw new PythonAPIException("The distribution '" + name + "' does not exist.");
            //
            return modelName;
        }
        // A temperature and a pressure are scalars, and the model refuses a vector distribution for them only
        // when the deck is written, so the combination is rejected here instead
        public string ResolveScalarDistributionName(object distributionName, string quantityName)
        {
            string name = ResolveDistributionName(distributionName);
            if (PythonNameComparer.AreSame(name, Distribution.DefaultDistributionName)) return name;
            //
            Distribution distribution = _modelResolver.GetModel().Distributions[name];
            if (distribution.DistributionType != DistributionTypeEnum.Scalar)
                throw new PythonAPIException("The distribution '" + name + "' is a " +
                                             distribution.DistributionType + " distribution, and " +
                                             quantityName + " needs a Scalar one.");
            //
            return name;
        }
        public string ResolveExistingFileName(object fileName, string argumentName)
        {
            string name = AsString(fileName, argumentName);
            if (_fileNameResolver == null)
                throw new System.InvalidOperationException("The " + _itemTypeName + " service reads no files.");
            //
            return _fileNameResolver.GetExistingFileName(name, argumentName);
        }
        public void SetActive(string stepName, NamedClass[] items, bool active)
        {
            string[] stepNames = new string[items.Length];
            for (int i = 0; i < stepNames.Length; i++) stepNames[i] = stepName;
            //
            EnsureCommandsAvailable();
            _modelGateway.ActivateDeactivateItems(items, active, stepNames);
        }
        public void Rename(string stepName, NamedClass item, string newName)
        {
            ValidateName(stepName, newName, item.Name);
            //
            EnsureCommandsAvailable();
            _modelGateway.RenameModelItem(item, newName, stepName);
        }
        public void ValidateName(string stepName, string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames(stepName))
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The " + _itemTypeName + " '" + name + "' already exists in " +
                                                 "the step '" + stepName + "'.");
            }
        }
        protected void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
        private static string FindKey(IEnumerable<string> keys, string name)
        {
            foreach (string key in keys)
            {
                if (PythonNameComparer.AreSame(key, name)) return key;
            }
            return null;
        }
        // The item wraps a phase into 0 to 360 degrees and then stores the wrapped number in place of an expression,
        // so a phase outside that range is refused, read from a copy that does not wrap it
        protected static EquationString ToPhaseDeg(EquationContainer phaseDeg, object value)
        {
            EquationString equation = PythonValueConverter.ToEquationString(value, "phase_deg");
            double phase = GivenEquationValue.Evaluate(phaseDeg, equation, "phase_deg");
            if (!(phase >= 0 && phase < 360))
                throw new PythonAPIException("The argument 'phase_deg' is " + phase + ", but a phase is at least 0 " +
                                             "and below 360 degrees. Give " + Tools.GetPhase360(phase) + " instead.");
            //
            return equation;
        }
        protected static string AsString(object value, string argumentName)
        {
            string text = value as string;
            if (text == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a string.");
            //
            return text;
        }
        //
        protected abstract OrderedDictionary<string, TItem> GetItems(Step step);
    }
}
