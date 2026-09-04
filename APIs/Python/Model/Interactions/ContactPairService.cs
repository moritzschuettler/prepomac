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
    internal class ContactPairService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly SurfaceInteractionService _surfaceInteractionService;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public ContactPairService(ModelResolver modelResolver, SelectionService selectionService,
                                  SurfaceInteractionService surfaceInteractionService,
                                  IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _surfaceInteractionService = surfaceInteractionService;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            return _modelResolver.GetModel().ContactPairs.Keys.ToArray();
        }
        public ContactPair GetContactPair(string name)
        {
            if (name == null) return null;
            //
            ContactPair contactPair;
            if (!_modelResolver.GetModel().ContactPairs.TryGetValue(name, out contactPair)) return null;
            //
            return contactPair;
        }
        public ContactPair ResolveContactPair(string name)
        {
            ContactPair contactPair = GetContactPair(name);
            if (contactPair == null)
                throw new PythonAPIException("The contact pair '" + name + "' does not exist.");
            //
            return contactPair;
        }
        public ContactPair CloneForReplace(string name)
        {
            ContactPair contactPair = ResolveContactPair(name).DeepClone();
            // Controller.ReplaceContactPair always deletes the internal surfaces of the old contact pair
            // and converts the new one, without the guard the boundary conditions and the loads have. A
            // side that was selection based is therefore reset to its selection, so that the conversion
            // recreates it; a side that names a region of the model is left alone.
            if (contactPair.MasterCreationData != null) contactPair.MasterRegionType = RegionTypeEnum.Selection;
            if (contactPair.SlaveCreationData != null) contactPair.SlaveRegionType = RegionTypeEnum.Selection;
            //
            return contactPair;
        }
        public bool GetIsTwoDimensional()
        {
            ModelSpaceEnum modelSpace = _modelResolver.GetModel().Properties.ModelSpace;
            return modelSpace != ModelSpaceEnum.ThreeD && modelSpace != ModelSpaceEnum.Undefined;
        }
        public string ResolveSurfaceInteractionName(object surfaceInteractionName)
        {
            string name = surfaceInteractionName as string;
            if (name == null)
                throw new PythonAPIException("The argument 'surface_interaction_name' must be a string.");
            //
            _surfaceInteractionService.ResolveSurfaceInteraction(name);
            //
            return name;
        }
        public void ApplyMaster(ContactPair contactPair, object region)
        {
            MasterSlaveRegionApplier.ApplyMaster(contactPair, region, "master_region", "surfaces",
                                                 _selectionService, _modelResolver);
        }
        public void ApplySlave(ContactPair contactPair, object region)
        {
            MasterSlaveRegionApplier.ApplySlave(contactPair, region, "slave_region", "surfaces",
                                                _selectionService, _modelResolver);
        }
        public void ApplyValues(ContactPair contactPair, object method, object smallSliding, object adjust,
                                object adjustmentSize)
        {
            if (method != null)
                contactPair.Method =
                    PythonValueConverter.ToEnum<ContactPairMethod>(AsString(method, "method"), "method");
            if (smallSliding != null)
            {
                // The model clears small sliding for a surface to surface pair instead of refusing it,
                // so the incompatible combination is rejected here rather than silently ignored
                bool value = PythonValueConverter.ToBool(smallSliding, "small_sliding");
                if (value && contactPair.Method == ContactPairMethod.SurfaceToSurface)
                    throw new PythonAPIException("The SurfaceToSurface contact method does not support " +
                                                 "small sliding. Give a method of NodeToSurface or Mortar " +
                                                 "together with 'small_sliding'.");
                contactPair.SmallSliding = value;
            }
            if (adjustmentSize != null)
            {
                // An adjustment size implies the adjustment, the way the dialog presents the two
                contactPair.AdjustmentSize = PythonValueConverter.ToDouble(adjustmentSize, "adjustment_size");
                contactPair.Adjust = true;
            }
            if (adjust != null) contactPair.Adjust = PythonValueConverter.ToBool(adjust, "adjust");
        }
        public void Add(ContactPair contactPair)
        {
            EnsureCommandsAvailable();
            _modelGateway.AddContactPair(contactPair);
        }
        public void Replace(string oldName, ContactPair contactPair)
        {
            ResolveContactPair(oldName);
            ValidateName(contactPair.Name, oldName);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceContactPair(oldName, contactPair);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.DuplicateContactPairs(names);
        }
        public void SwapMasterSlave(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.SwapMasterSlaveContactPairs(names);
        }
        public void MergeByMasterSlave(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.MergeByMasterSlaveContactPairs(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveContactPairs(names);
        }
        public void Hide(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.HideContactPairs(names);
        }
        public void Show(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.ShowContactPairs(names);
        }
        public void SetActive(NamedClass[] items, bool active)
        {
            EnsureCommandsAvailable();
            // Every step name is null, because a contact pair does not belong to a step
            _modelGateway.ActivateDeactivateItems(items, active, new string[items.Length]);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            _modelGateway.RenameModelItem(item, newName, null);
        }
        public void ValidateName(string name, string allowedExistingName)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (allowedExistingName != null && string.Equals(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (string.Equals(name, existingName))
                    throw new PythonAPIException("The contact pair '" + name + "' already exists.");
            }
        }
        //
        private static string AsString(object value, string argumentName)
        {
            string text = value as string;
            if (text == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a string.");
            //
            return text;
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
