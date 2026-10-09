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
            // The controller follows a renamed surface interaction by comparing names exactly, so the name is
            // stored in the model's own spelling rather than the caller's
            return _surfaceInteractionService.ResolveSurfaceInteraction(name).Name;
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
            ContactPairMethod methodValue = contactPair.Method;
            if (method != null)
                methodValue = PythonValueConverter.ToEnum<ContactPairMethod>(AsString(method, "method"), "method");
            bool? smallSlidingValue = null;
            if (smallSliding != null)
                smallSlidingValue = PythonValueConverter.ToBool(smallSliding, "small_sliding");
            // Only the node to surface method writes small sliding; the model clears it for surface to surface
            // and keeps it unwritten for the others, so it is refused rather than silently dropped
            bool usesSmallSliding = contactPair.Method == ContactPairMethod.NodeToSurface && contactPair.SmallSliding;
            if (methodValue != ContactPairMethod.NodeToSurface)
            {
                if (smallSlidingValue == true)
                    throw new PythonAPIException("The " + methodValue + " contact method does not support small " +
                                                 "sliding. Give method='NodeToSurface' together with " +
                                                 "'small_sliding'.");
                if (method != null && smallSlidingValue == null && usesSmallSliding)
                    throw new PythonAPIException("The contact pair '" + contactPair.Name + "' uses small " +
                                                 "sliding, which the " + methodValue + " method does not " +
                                                 "support. Give small_sliding=False together with the method.");
                smallSlidingValue = false;
            }
            contactPair.Method = methodValue;
            if (smallSlidingValue.HasValue) contactPair.SmallSliding = smallSlidingValue.Value;
            //
            bool? adjustValue = null;
            if (adjust != null) adjustValue = PythonValueConverter.ToBool(adjust, "adjust");
            if (adjustmentSize != null)
            {
                double size = PythonValueConverter.ToDouble(adjustmentSize, "adjustment_size");
                if (adjustValue == false)
                    throw new PythonAPIException("The argument 'adjustment_size' applies only when the slave " +
                                                 "nodes are adjusted. Leave it out together with adjust=False.");
                // The dialog offers the size as Default, stored as NaN, and None keeps the value on a replace,
                // so float('nan') is how a script asks for Default
                if (!double.IsNaN(size) && !(size >= 0))
                    throw new PythonAPIException("The argument 'adjustment_size' must be zero or larger, or " +
                                                 "float('nan') for Default.");
                // An adjustment size implies the adjustment, the way the dialog presents the two
                contactPair.AdjustmentSize = size;
                contactPair.Adjust = true;
            }
            if (adjustValue.HasValue) contactPair.Adjust = adjustValue.Value;
        }
        public static void ApplyColors(ContactPair contactPair, object masterColor, object slaveColor)
        {
            if (masterColor != null) contactPair.MasterColor = PythonColorConverter.ToColor(masterColor, "master_color");
            if (slaveColor != null) contactPair.SlaveColor = PythonColorConverter.ToColor(slaveColor, "slave_color");
        }
        public void Add(ContactPair contactPair)
        {
            EnsureCommandsAvailable();
            _modelGateway.AddContactPair(contactPair);
        }
        public void Replace(string oldName, ContactPair contactPair)
        {
            string storedName = ResolveContactPair(oldName).Name;
            ValidateName(contactPair.Name, storedName);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceContactPair(storedName, contactPair);
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
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
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
