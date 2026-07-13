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
    public class ViewDistributingCoupling : ViewConstraint
    {
        // Variables                                                                                                                
        private CaeModel.DistributingCoupling _distributingCoupling;


        // Properties                                                                                                               
        public override string Name
        {
            get { return _distributingCoupling.Name; }
            set { _distributingCoupling.Name = value; }
        }
        // MASTER
        [CategoryAttribute("Control point")]
        [OrderedDisplayName(0, 10, "Reference point")]
        [DescriptionAttribute("Select the reference point for the creation of the distributing coupling definition.")]
        [Id(1, 2)]
        public string ReferencePointName
        {
            get { return _distributingCoupling.ReferencePointName; }
            set { _distributingCoupling.ReferencePointName = value; }
        }
        // SLAVE
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the surface region type for the distributing coupling definition.")]
        [Id(1, 3)]
        public override string SlaveRegionType { get { return base.SlaveRegionType; } set { base.SlaveRegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the distributing coupling definition.")]
        [Id(2, 3)]
        public string SurfaceName
        {
            get { return _distributingCoupling.RegionName; }
            set { _distributingCoupling.RegionName = value; }
        }
        // DOF
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(0, 10, "U1")]
        [DescriptionAttribute("Distribute the force in the direction of the first axis.")]
        [Id(1, 4)]
        public bool U1 { get { return _distributingCoupling.U1; } set { _distributingCoupling.U1 = value; } }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(1, 10, "U2")]
        [DescriptionAttribute("Distribute the force in the direction of the second axis.")]
        [Id(2, 4)]
        public bool U2 { get { return _distributingCoupling.U2; } set { _distributingCoupling.U2 = value; } }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(2, 10, "U3")]
        [DescriptionAttribute("Distribute the force in the direction of the third axis.")]
        [Id(3, 4)]
        public bool U3 { get { return _distributingCoupling.U3; } set { _distributingCoupling.U3 = value; } }
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
            get { return _distributingCoupling.MasterColor; }
            set { _distributingCoupling.MasterColor = value; }
        }


        // Constructors                                                                                                             
        public ViewDistributingCoupling(CaeModel.DistributingCoupling distributingCoupling)
        {
            // The order is important
            _distributingCoupling = distributingCoupling;
            //
            Dictionary<RegionTypeEnum, string> masterRegionTypePropertyNamePairs =
                new Dictionary<RegionTypeEnum, string>();
            masterRegionTypePropertyNamePairs.Add(RegionTypeEnum.ReferencePointName, nameof(ReferencePointName));
            //
            Dictionary<RegionTypeEnum, string> slaveRegionTypePropertyNamePairs =
                new Dictionary<RegionTypeEnum, string>();
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SlaveSelectionHidden));
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            base.SetBase(_distributingCoupling, masterRegionTypePropertyNamePairs, slaveRegionTypePropertyNamePairs);
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // Now lets display Distributed/Not distributed instead of True/False
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U1), "Distributed", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U2), "Distributed", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U3), "Distributed", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(UR1), "Distributed", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(UR2), "Distributed", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(UR3), "Distributed", "Unconstrained");
            // 2D
            base.DynamicCustomTypeDescriptor.GetProperty(nameof(U3)).SetIsBrowsable(!_distributingCoupling.TwoD);
            base.DynamicCustomTypeDescriptor.GetProperty(nameof(UR1)).SetIsBrowsable(!_distributingCoupling.TwoD);
            base.DynamicCustomTypeDescriptor.GetProperty(nameof(UR2)).SetIsBrowsable(!_distributingCoupling.TwoD);
        }


        // Methods                                                                                                                  
        public override CaeModel.Constraint GetBase()
        {
            return _distributingCoupling;
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
    }
}
