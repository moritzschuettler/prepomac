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
        public void Add(InitialCondition initialCondition)
        {
            EnsureCommandsAvailable();
            ModelGateway.AddInitialCondition(initialCondition);
        }
        public void Replace(string oldName, InitialCondition initialCondition)
        {
            ResolveInitialCondition(oldName);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceInitialCondition(oldName, initialCondition);
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
