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
using System.Linq;

namespace APIs.Python.Model
{
    internal class InitialConditionService : FieldItemService
    {
        // Properties
        protected override string ItemTypeName { get { return "initial condition"; } }


        // Constructors
        public InitialConditionService(ModelResolver modelResolver, SelectionService selectionService,
                                       IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, modelGateway)
        {
        }


        // Methods
        public string[] GetNames()
        {
            return ModelResolver.GetModel().InitialConditions.Keys.ToArray();
        }
        public InitialCondition GetInitialCondition(string name)
        {
            if (name == null) return null;
            //
            InitialCondition initialCondition;
            if (!ModelResolver.GetModel().InitialConditions.TryGetValue(name, out initialCondition)) return null;
            //
            return initialCondition;
        }
        public InitialCondition ResolveInitialCondition(string name)
        {
            InitialCondition initialCondition = GetInitialCondition(name);
            if (initialCondition == null)
                throw new PythonAPIException("The initial condition '" + name + "' does not exist.");
            //
            return initialCondition;
        }
        public void ApplyRegion(InitialCondition initialCondition, object region, string target)
        {
            RegionApplier.Apply(initialCondition, region, "region", target, SelectionService, ModelResolver);
        }
        public string ResolveScalarDistributionName(object distributionName)
        {
            string name = distributionName as string;
            if (name == null)
                throw new PythonAPIException("The argument 'distribution_name' must be a string.");
            //
            if (PythonNameComparer.AreSame(name, Distribution.DefaultDistributionName))
                return Distribution.DefaultDistributionName;
            //
            Distribution distribution;
            if (!ModelResolver.GetModel().Distributions.TryGetValue(name, out distribution))
                throw new PythonAPIException("The distribution '" + name + "' does not exist.");
            // A temperature is a scalar, and the model refuses a vector distribution for it only when
            // the deck is written, so the combination is rejected here instead
            if (distribution.DistributionType != DistributionTypeEnum.Scalar)
                throw new PythonAPIException("The distribution '" + name + "' is a " +
                                             distribution.DistributionType + " distribution, and a " +
                                             "temperature needs a Scalar one.");
            //
            return name;
        }
        public void Add(InitialCondition initialCondition)
        {
            EnsureCommandsAvailable();
            ModelGateway.AddInitialCondition(initialCondition);
        }
        // Controller.ReplaceInitialCondition deletes the internal set of the old item and converts the new one,
        // so a region that was selection based is reset to its selection for the conversion to recreate it,
        // the way FrmInitialCondition does before it replaces
        public TInitialCondition Clone<TInitialCondition>(string name, string itemType)
            where TInitialCondition : InitialCondition
        {
            InitialCondition initialCondition = ResolveInitialCondition(name);
            if (initialCondition.GetType() != typeof(TInitialCondition))
                throw new PythonAPIException("The initial condition '" + name + "' is made by add_" +
                                             InitialConditionAPI.GetItemType(initialCondition) + ", so replace_" +
                                             itemType + " cannot replace it. Remove it and add the new type instead.");
            TInitialCondition clone = (TInitialCondition)initialCondition.DeepClone();
            if (clone.CreationData != null) clone.RegionType = RegionTypeEnum.Selection;
            //
            return clone;
        }
        public void Replace(string oldName, InitialCondition initialCondition)
        {
            string storedName = ResolveInitialCondition(oldName).Name;
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceInitialCondition(storedName, initialCondition);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateInitialConditions(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveInitialConditions(names);
        }
        public void Hide(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.HideInitialConditions(names);
        }
        public void Show(string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.ShowInitialConditions(names);
        }
        public void SetActive(NamedClass[] items, bool active)
        {
            EnsureCommandsAvailable();
            // Every step name is null, because these items do not belong to a step
            ModelGateway.ActivateDeactivateItems(items, active, new string[items.Length]);
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


