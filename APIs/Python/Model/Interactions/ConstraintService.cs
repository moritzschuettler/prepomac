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
using System.Linq;

namespace APIs.Python.Model
{
    internal class ConstraintService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public ConstraintService(ModelResolver modelResolver, SelectionService selectionService,
                                 IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            return _modelResolver.GetModel().Constraints.Keys.ToArray();
        }
        public Constraint GetConstraint(string name)
        {
            if (name == null) return null;
            //
            Constraint constraint;
            if (!_modelResolver.GetModel().Constraints.TryGetValue(name, out constraint)) return null;
            //
            return constraint;
        }
        public Constraint ResolveConstraint(string name)
        {
            Constraint constraint = GetConstraint(name);
            if (constraint == null)
                throw new PythonAPIException("The constraint '" + name + "' does not exist.");
            //
            return constraint;
        }
        // The type cannot change on a replace, the same way the dialog only edits the type it opened
        public TConstraint CloneForReplace<TConstraint>(string name, string itemType)
            where TConstraint : Constraint
        {
            Constraint constraint = ResolveConstraint(name);
            // A cyclic symmetry derives from a tie, so only the exact type is the one being replaced
            if (constraint.GetType() != typeof(TConstraint))
                throw new PythonAPIException("The constraint '" + name + "' is made by add_" +
                                             ConstraintAPI.GetItemType(constraint) + ", so replace_" + itemType +
                                             " cannot replace it. Remove it and add the new type instead.");
            //
            TConstraint clone = ((TConstraint)constraint).DeepClone();
            // Controller.ReplaceConstraint always deletes the internal sets of the old constraint and
            // converts the new one, without the guard the boundary conditions and the loads have. Every
            // region that was selection based is therefore reset to its selection, so that the conversion
            // recreates it; a region that names an entity of the model is left alone.
            if (clone.MasterCreationData != null) clone.MasterRegionType = RegionTypeEnum.Selection;
            if (clone.SlaveCreationData != null) clone.SlaveRegionType = RegionTypeEnum.Selection;
            IMultiRegion multiRegion = clone as IMultiRegion;
            if (multiRegion != null && multiRegion.CreationData != null)
                multiRegion.RegionType = RegionTypeEnum.Selection;
            //
            return clone;
        }
        public bool GetIsTwoDimensional()
        {
            ModelSpaceEnum modelSpace = _modelResolver.GetModel().Properties.ModelSpace;
            return modelSpace != ModelSpaceEnum.ThreeD && modelSpace != ModelSpaceEnum.Undefined;
        }
        public string ResolveReferencePointName(object referencePointName)
        {
            string name = referencePointName as string;
            if (name == null)
                throw new PythonAPIException("The argument 'reference_point_name' must be a string.");
            // The controller follows a renamed reference point by comparing names exactly, so the name is
            // stored in the model's own spelling rather than the caller's
            FeReferencePoint referencePoint;
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            if (mesh == null || !mesh.ReferencePoints.TryGetValue(name, out referencePoint))
                throw new PythonAPIException("The reference point '" + name + "' does not exist.");
            //
            return referencePoint.Name;
        }
        // A constraint whose single region is the IMultiRegion one: the springs, the compression only,
        // the rigid body and the couplings
        public void ApplyRegion(IMultiRegion constraint, object region, string target)
        {
            RegionApplier.Apply(constraint, region, "region", target, _selectionService, _modelResolver);
        }
        public void ApplyRegion(IMultiRegion constraint, object region, string[] targets)
        {
            RegionApplier.Apply(constraint, region, "region", targets, _selectionService, _modelResolver);
        }
        public void ApplyRegion(IMultiRegion constraint, object region, string[] selectionTargets,
                                string[] namedRegionTargets)
        {
            RegionApplier.Apply(constraint, region, "region", selectionTargets, namedRegionTargets,
                                _selectionService, _modelResolver);
        }
        public void ApplyMaster(IMasterSlaveMultiRegion constraint, object region, string target)
        {
            MasterSlaveRegionApplier.ApplyMaster(constraint, region, "master_region", target,
                                                 _selectionService, _modelResolver);
        }
        public void ApplySlave(IMasterSlaveMultiRegion constraint, object region, string target)
        {
            MasterSlaveRegionApplier.ApplySlave(constraint, region, "slave_region", target,
                                                _selectionService, _modelResolver);
        }
        public void Add(Constraint constraint)
        {
            EnsureDefined(constraint);
            //
            EnsureCommandsAvailable();
            _modelGateway.AddConstraint(constraint);
        }
        public void Replace(string oldName, Constraint constraint)
        {
            string storedName = ResolveConstraint(oldName).Name;
            ValidateName(constraint.Name, storedName);
            EnsureDefined(constraint);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceConstraint(storedName, constraint);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.DuplicateConstraints(names);
        }
        public void SwapMasterSlave(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.SwapMasterSlaveConstraints(names);
        }
        public void MergeByMasterSlave(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.MergeByMasterSlaveConstraints(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveConstraints(names);
        }
        public void Hide(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.HideConstraints(names);
        }
        public void Show(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.ShowConstraints(names);
        }
        public void SetActive(NamedClass[] items, bool active)
        {
            EnsureCommandsAvailable();
            // Every step name is null, because a constraint does not belong to a step
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
                    throw new PythonAPIException("The constraint '" + name + "' already exists.");
            }
        }
        //
        // The dialog refuses on OK a spring without a stiffness and a coupling without a degree of freedom, in these
        // words, and judges the constraint it ends with, so an add and a replace are checked on the final one
        private static void EnsureDefined(Constraint constraint)
        {
            if (constraint is SpringConstraint spring && spring.GetSpringStiffnessValues().Length == 0)
                throw new PythonAPIException("At least one stiffness must be larger than 0. Give a nonzero 'k1', " +
                                             "'k2' or 'k3'.");
            if (constraint is KinematicCoupling kinematic && !kinematic.U1 && !kinematic.U2 && !kinematic.U3)
                throw new PythonAPIException("At least one kinematic coupling degree of freedom must be coupled. " +
                                             "Give True to 'u1', 'u2' or 'u3'.");
            if (constraint is DistributingCoupling distributing && !distributing.U1 && !distributing.U2 &&
                !distributing.U3 && !distributing.UR1 && !distributing.UR2 && !distributing.UR3)
                throw new PythonAPIException("At least one distributing coupling degree of freedom must be " +
                                             "distributed. Give True to 'u1', 'u2', 'u3', 'ur1', 'ur2' or 'ur3'.");
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
