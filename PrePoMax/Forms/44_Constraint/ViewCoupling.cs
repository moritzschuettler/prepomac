// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using CaeGlobals;
using DynamicTypeDescriptor;


namespace PrePoMax
{
    [Serializable]
    public enum CouplingTypeEnum
    {
        [StandardValue("Kinematic", "Kinematic", Description =
                       "The selected degrees of freedom of the region nodes follow the reference point rigidly.")]
        Kinematic,
        [StandardValue("Distributing", "Distributing", Description =
                       "The forces and the moments at the reference point are distributed over the region nodes.")]
        Distributing
    }

    [Serializable]
    public class ViewCoupling : ViewConstraint
    {
        // Variables                                                                                                                
        private CaeModel.KinematicCoupling _kinematicCoupling;
        private CaeModel.DistributingCoupling _distributingCoupling;
        private CaeModel.Coupling _coupling;


        // Properties                                                                                                               
        public override string Name { get { return _coupling.Name; } set { _coupling.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Coupling type")]
        [DescriptionAttribute("Select the coupling type.")]
        [Id(2, 1)]
        public CouplingTypeEnum CouplingType
        {
            get
            {
                if (_coupling == _kinematicCoupling) return CouplingTypeEnum.Kinematic;
                else return CouplingTypeEnum.Distributing;
            }
            set { SetCouplingType(value); }
        }
        // MASTER
        [CategoryAttribute("Control point")]
        [OrderedDisplayName(0, 10, "Reference point")]
        [DescriptionAttribute("Select the reference point for the creation of the coupling definition.")]
        [Id(1, 2)]
        public string ReferencePointName
        {
            get { return _coupling.ReferencePointName; }
            set { _coupling.ReferencePointName = value; }
        }
        // SLAVE
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the coupling definition.")]
        [Id(1, 3)]
        public override string SlaveRegionType { get { return base.SlaveRegionType; } set { base.SlaveRegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the coupling definition.")]
        [Id(2, 3)]
        public string SurfaceName
        {
            get { return _coupling.RegionName; }
            set { _coupling.RegionName = value; }
        }
        // DOF
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(0, 10, "U1")]
        [DescriptionAttribute("Couple the displacement in the direction of the first axis.")]
        [Id(1, 4)]
        public bool U1
        {
            get
            {
                if (_coupling == _kinematicCoupling) return _kinematicCoupling.U1;
                else return _distributingCoupling.U1;
            }
            set
            {
                if (_coupling == _kinematicCoupling) _kinematicCoupling.U1 = value;
                else _distributingCoupling.U1 = value;
            }
        }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(1, 10, "U2")]
        [DescriptionAttribute("Couple the displacement in the direction of the second axis.")]
        [Id(2, 4)]
        public bool U2
        {
            get
            {
                if (_coupling == _kinematicCoupling) return _kinematicCoupling.U2;
                else return _distributingCoupling.U2;
            }
            set
            {
                if (_coupling == _kinematicCoupling) _kinematicCoupling.U2 = value;
                else _distributingCoupling.U2 = value;
            }
        }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(2, 10, "U3")]
        [DescriptionAttribute("Couple the displacement in the direction of the third axis.")]
        [Id(3, 4)]
        public bool U3
        {
            get
            {
                if (_coupling == _kinematicCoupling) return _kinematicCoupling.U3;
                else return _distributingCoupling.U3;
            }
            set
            {
                if (_coupling == _kinematicCoupling) _kinematicCoupling.U3 = value;
                else _distributingCoupling.U3 = value;
            }
        }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(3, 10, "UR1")]
        [DescriptionAttribute("Distribute the moment in the direction of the first axis.")]
        [Id(4, 4)]
        public bool UR1 { get { return _distributingCoupling.UR1; } set { _distributingCoupling.UR1 = value; } }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(4, 10, "UR2")]
        [DescriptionAttribute("Distribute the moment in the direction of the second axis.")]
        [Id(5, 4)]
        public bool UR2 { get { return _distributingCoupling.UR2; } set { _distributingCoupling.UR2 = value; } }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(5, 10, "UR3")]
        [DescriptionAttribute("Distribute the moment in the direction of the third axis.")]
        [Id(6, 4)]
        public bool UR3 { get { return _distributingCoupling.UR3; } set { _distributingCoupling.UR3 = value; } }
        //
        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Select constraint color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 10)]
        public Color Color
        {
            get { return _coupling.MasterColor; }
            set { _coupling.MasterColor = value; }
        }


        // Constructors                                                                                                             
        public ViewCoupling(CaeModel.Coupling coupling)
        {
            // The order is important
            if (coupling is CaeModel.KinematicCoupling kinematicCoupling)
            {
                _kinematicCoupling = kinematicCoupling;
                _distributingCoupling = new CaeModel.DistributingCoupling(kinematicCoupling.Name,
                                                                          kinematicCoupling.ReferencePointName,
                                                                          kinematicCoupling.RegionName,
                                                                          kinematicCoupling.RegionType,
                                                                          kinematicCoupling.TwoD);
                CopyCouplingData(kinematicCoupling, _distributingCoupling);
            }
            else if (coupling is CaeModel.DistributingCoupling distributingCoupling)
            {
                _distributingCoupling = distributingCoupling;
                _kinematicCoupling = new CaeModel.KinematicCoupling(distributingCoupling.Name,
                                                                    distributingCoupling.ReferencePointName,
                                                                    distributingCoupling.RegionName,
                                                                    distributingCoupling.RegionType,
                                                                    distributingCoupling.TwoD);
                CopyCouplingData(distributingCoupling, _kinematicCoupling);
            }
            else throw new NotSupportedException();
            //
            _coupling = coupling;
            //
            SetCouplingBase();
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // Now lets display Distributed/Unconstrained instead of True/False
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(UR1), "Distributed", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(UR2), "Distributed", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(UR3), "Distributed", "Unconstrained");
            //
            UpdateCouplingVisibility();
        }


        // Methods                                                                                                                  
        public override CaeModel.Constraint GetBase()
        {
            return _coupling;
        }
        public void PopulateDropDownLists(string[] referencePointNames, string[] surfaceNames)
        {
            base.DynamicCustomTypeDescriptor.PopulateProperty(nameof(ReferencePointName), referencePointNames);
            // Master
            Dictionary<RegionTypeEnum, string[]> masterRegionTypeListItemsPairs =
                new Dictionary<RegionTypeEnum, string[]>();
            masterRegionTypeListItemsPairs.Add(RegionTypeEnum.ReferencePointName, referencePointNames);
            // Slave
            Dictionary<RegionTypeEnum, string[]> slaveRegionTypeListItemsPairs =
                new Dictionary<RegionTypeEnum, string[]>();
            slaveRegionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            slaveRegionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            //
            base.PopulateDropDownLists(masterRegionTypeListItemsPairs, slaveRegionTypeListItemsPairs);
        }
        private void SetCouplingBase()
        {
            Dictionary<RegionTypeEnum, string> masterRegionTypePropertyNamePairs =
                new Dictionary<RegionTypeEnum, string>();
            masterRegionTypePropertyNamePairs.Add(RegionTypeEnum.ReferencePointName, nameof(ReferencePointName));
            //
            Dictionary<RegionTypeEnum, string> slaveRegionTypePropertyNamePairs =
                new Dictionary<RegionTypeEnum, string>();
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SlaveSelectionHidden));
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            base.SetBase(_coupling, masterRegionTypePropertyNamePairs, slaveRegionTypePropertyNamePairs);
        }
        private void SetCouplingType(CouplingTypeEnum couplingType)
        {
            if (couplingType == CouplingType) return;
            //
            CaeModel.Coupling previousCoupling = _coupling;
            if (couplingType == CouplingTypeEnum.Kinematic) _coupling = _kinematicCoupling;
            else _coupling = _distributingCoupling;
            //
            CopyCouplingData(previousCoupling, _coupling);
            //
            SetCouplingBase();
            //
            UpdateCouplingVisibility();
        }
        private void CopyCouplingData(CaeModel.Coupling source, CaeModel.Coupling target)
        {
            target.CopyFrom(source);
            //
            target.ReferencePointName = source.ReferencePointName;
            target.RegionName = source.RegionName;
            target.RegionType = source.RegionType;
            target.CreationIds = source.CreationIds;
            target.CreationData = source.CreationData;
            target.MasterColor = source.MasterColor;
            target.SlaveColor = source.SlaveColor;
            //
            if (source is CaeModel.KinematicCoupling sourceKinematicCoupling &&
                target is CaeModel.DistributingCoupling targetDistributingCoupling)
            {
                targetDistributingCoupling.U1 = sourceKinematicCoupling.U1;
                targetDistributingCoupling.U2 = sourceKinematicCoupling.U2;
                targetDistributingCoupling.U3 = sourceKinematicCoupling.U3;
            }
            else if (source is CaeModel.DistributingCoupling sourceDistributingCoupling &&
                     target is CaeModel.KinematicCoupling targetKinematicCoupling)
            {
                targetKinematicCoupling.U1 = sourceDistributingCoupling.U1;
                targetKinematicCoupling.U2 = sourceDistributingCoupling.U2;
                targetKinematicCoupling.U3 = sourceDistributingCoupling.U3;
            }
        }
        private void UpdateCouplingVisibility()
        {
            if (DynamicCustomTypeDescriptor == null) return;
            //
            bool kinematic = _coupling == _kinematicCoupling;
            bool twoD = _coupling.TwoD;
            // Now lets display Coupled/Distributed instead of True/False
            string trueName = kinematic ? "Coupled" : "Distributed";
            DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U1), trueName, "Unconstrained");
            DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U2), trueName, "Unconstrained");
            DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U3), trueName, "Unconstrained");
            //
            string action = kinematic ? "Couple the displacement" : "Distribute the force";
            DynamicCustomTypeDescriptor.GetProperty(nameof(U1)).
                SetDescription(action + " in the direction of the first axis.");
            DynamicCustomTypeDescriptor.GetProperty(nameof(U2)).
                SetDescription(action + " in the direction of the second axis.");
            DynamicCustomTypeDescriptor.GetProperty(nameof(U3)).
                SetDescription(action + " in the direction of the third axis.");
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(UR1)).SetIsBrowsable(!kinematic && !twoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(UR2)).SetIsBrowsable(!kinematic && !twoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(UR3)).SetIsBrowsable(!kinematic);
            // 2D
            DynamicCustomTypeDescriptor.GetProperty(nameof(U3)).SetIsBrowsable(!twoD);
        }
    }
}
