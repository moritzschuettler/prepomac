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
    public class ViewKinematicCoupling : ViewConstraint
    {
        // Variables                                                                                                                
        private CaeModel.KinematicCoupling _kinematicCoupling;


        // Properties                                                                                                               
        public override string Name
        {
            get { return _kinematicCoupling.Name; }
            set { _kinematicCoupling.Name = value; }
        }
        // MASTER
        [CategoryAttribute("Control point")]
        [OrderedDisplayName(0, 10, "Reference point")]
        [DescriptionAttribute("Select the reference point for the creation of the kinematic coupling definition.")]
        [Id(1, 2)]
        public string ReferencePointName
        {
            get { return _kinematicCoupling.ReferencePointName; }
            set { _kinematicCoupling.ReferencePointName = value; }
        }
        // SLAVE
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the kinematic coupling definition.")]
        [Id(1, 3)]
        public override string SlaveRegionType { get { return base.SlaveRegionType; } set { base.SlaveRegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the kinematic coupling definition.")]
        [Id(2, 3)]
        public string SurfaceName
        {
            get { return _kinematicCoupling.RegionName; }
            set { _kinematicCoupling.RegionName = value; }
        }
        // DOF
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(0, 10, "U1")]
        [DescriptionAttribute("Couple the displacement in the direction of the first axis.")]
        [Id(1, 4)]
        public bool U1 { get { return _kinematicCoupling.U1; } set { _kinematicCoupling.U1 = value; } }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(1, 10, "U2")]
        [DescriptionAttribute("Couple the displacement in the direction of the second axis.")]
        [Id(2, 4)]
        public bool U2 { get { return _kinematicCoupling.U2; } set { _kinematicCoupling.U2 = value; } }
        //
        [CategoryAttribute("DOF")]
        [OrderedDisplayName(2, 10, "U3")]
        [DescriptionAttribute("Couple the displacement in the direction of the third axis.")]
        [Id(3, 4)]
        public bool U3 { get { return _kinematicCoupling.U3; } set { _kinematicCoupling.U3 = value; } }
        //
        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Select constraint color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 10)]
        public Color Color
        {
            get { return _kinematicCoupling.MasterColor; }
            set { _kinematicCoupling.MasterColor = value; }
        }


        // Constructors                                                                                                             
        public ViewKinematicCoupling(CaeModel.KinematicCoupling kinematicCoupling)
        {
            // The order is important
            _kinematicCoupling = kinematicCoupling;
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
            base.SetBase(_kinematicCoupling, masterRegionTypePropertyNamePairs, slaveRegionTypePropertyNamePairs);
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // Now lets display Coupled/Free instead of True/False
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U1), "Coupled", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U2), "Coupled", "Unconstrained");
            base.DynamicCustomTypeDescriptor.RenameBooleanProperty(nameof(U3), "Coupled", "Unconstrained");
            // 2D
            base.DynamicCustomTypeDescriptor.GetProperty(nameof(U3)).SetIsBrowsable(!_kinematicCoupling.TwoD);
        }


        // Methods                                                                                                                  
        public override CaeModel.Constraint GetBase()
        {
            return _kinematicCoupling;
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
