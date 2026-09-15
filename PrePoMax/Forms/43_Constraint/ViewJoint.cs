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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.Drawing;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewJoint : ViewConstraint
    {
        // Variables
        private Joint _joint;


        // Properties
        public override string Name { get { return _joint.Name; } set { _joint.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Joint type")]
        [DescriptionAttribute("Select the joint type. The joint is exact for any rotation about an axis whose direction stays " +
                              "fixed in space; it is approximate when the joint axis itself is rotated.")]
        [Id(2, 1)]
        public JointTypeEnum JointType
        {
            get { return _joint.JointType; }
            set
            {
                _joint.JointType = value;
                UpdateJointVisibility();
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Formulation")]
        [DescriptionAttribute("Select how the joint is written for the solver: as two rigid bodies, which is exact, or as " +
                              "axial springs from the region nodes to nodes on the joint axis, which needs no rigid body " +
                              "and solves on shell elements.")]
        [Id(3, 1)]
        public JointFormulationEnum Formulation
        {
            get { return _joint.Formulation; }
            set
            {
                _joint.Formulation = value;
                UpdateJointVisibility();
            }
        }
        // CENTRE ------------------------------------------------------------------------------------------------------------------
        [CategoryAttribute("Joint centre")]
        [OrderedDisplayName(0, 10, "Defined by")]
        [DescriptionAttribute("Select how the joint centre, at which the two regions are connected, is defined. The point " +
                              "selection modes take the nodes selected in the viewport: one node, two nodes whose midpoint " +
                              "is the centre, or three nodes on a circle whose centre is the joint centre.")]
        [Id(1, 4)]
        public JointCenterDefinitionEnum CenterDefinition
        {
            get { return _joint.CenterDefinition; }
            set
            {
                _joint.CenterDefinition = value;
                UpdateJointVisibility();
            }
        }
        //
        [CategoryAttribute("Joint centre")]
        [OrderedDisplayName(1, 10, "X")]
        [DescriptionAttribute("X coordinate of the joint centre.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 4)]
        public EquationString X { get { return _joint.X.Equation; } set { _joint.X.Equation = value; } }
        //
        [CategoryAttribute("Joint centre")]
        [OrderedDisplayName(2, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the joint centre.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 4)]
        public EquationString Y { get { return _joint.Y.Equation; } set { _joint.Y.Equation = value; } }
        //
        [CategoryAttribute("Joint centre")]
        [OrderedDisplayName(3, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the joint centre.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(4, 4)]
        public EquationString Z { get { return _joint.Z.Equation; } set { _joint.Z.Equation = value; } }
        // AXIS --------------------------------------------------------------------------------------------------------------------
        [CategoryAttribute("Joint axis")]
        [OrderedDisplayName(0, 10, "Defined by ")]
        [DescriptionAttribute("Select how the joint axis direction is defined.")]
        [Id(1, 5)]
        public JointDefinitionEnum AxisDefinition
        {
            get { return _joint.AxisDefinition; }
            set
            {
                _joint.AxisDefinition = value;
                UpdateJointVisibility();
            }
        }
        //
        [CategoryAttribute("Joint axis")]
        [OrderedDisplayName(1, 10, "N1")]
        [DescriptionAttribute("First component of the joint axis direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(2, 5)]
        public EquationString N1 { get { return _joint.N1.Equation; } set { _joint.N1.Equation = value; } }
        //
        [CategoryAttribute("Joint axis")]
        [OrderedDisplayName(2, 10, "N2")]
        [DescriptionAttribute("Second component of the joint axis direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(3, 5)]
        public EquationString N2 { get { return _joint.N2.Equation; } set { _joint.N2.Equation = value; } }
        //
        [CategoryAttribute("Joint axis")]
        [OrderedDisplayName(3, 10, "N3")]
        [DescriptionAttribute("Third component of the joint axis direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(4, 5)]
        public EquationString N3 { get { return _joint.N3.Equation; } set { _joint.N3.Equation = value; } }
        // SPRINGS -----------------------------------------------------------------------------------------------------------------
        [CategoryAttribute("Spring stiffness")]
        [OrderedDisplayName(0, 10, "Defined by  ")]
        [DescriptionAttribute("Select how the stiffness of a single spring is defined. From model computes it as " +
                              "100 * E * sqrt(A) / n, where E is the largest Young's modulus of the elements carrying the " +
                              "two regions, A is the average area of the two regions and n is the average number of their " +
                              "nodes; the springs of one region therefore carry 100 * E * sqrt(A) together, whatever the " +
                              "mesh density, which keeps the joint much stiffer than the regions it joins.")]
        [Id(1, 6)]
        public JointStiffnessDefinitionEnum StiffnessDefinition
        {
            get { return _joint.StiffnessDefinition; }
            set
            {
                _joint.StiffnessDefinition = value;
                UpdateJointVisibility();
            }
        }
        //
        [CategoryAttribute("Spring stiffness")]
        [OrderedDisplayName(1, 10, "Stiffness")]
        [DescriptionAttribute("Stiffness of each axial spring between a region node and a node on the joint axis.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(2, 6)]
        public EquationString Stiffness { get { return _joint.Stiffness.Equation; } set { _joint.Stiffness.Equation = value; } }
        // MASTER ------------------------------------------------------------------------------------------------------------------
        [CategoryAttribute("Master Region")]
        [OrderedDisplayName(0, 10, "Master region type")]
        [DescriptionAttribute("Select the master region type for the creation of the joint definition. The master surface " +
                              "defines the joint centre and axis when they are not entered.")]
        [Id(1, 2)]
        public override string MasterRegionType { get { return base.MasterRegionType; } set { base.MasterRegionType = value; } }
        //
        [CategoryAttribute("Master Region")]
        [OrderedDisplayName(1, 10, "Master surface")]
        [DescriptionAttribute("Select the master surface for the creation of the joint definition.")]
        [Id(2, 2)]
        public string MasterSurfaceName { get { return _joint.MasterRegionName; } set { _joint.MasterRegionName = value; } }
        // SLAVE -------------------------------------------------------------------------------------------------------------------
        [CategoryAttribute("Slave Region")]
        [OrderedDisplayName(0, 10, "Slave region type")]
        [DescriptionAttribute("Select the slave region type for the creation of the joint definition.")]
        [Id(1, 3)]
        public override string SlaveRegionType { get { return base.SlaveRegionType; } set { base.SlaveRegionType = value; } }
        //
        [CategoryAttribute("Slave Region")]
        [OrderedDisplayName(1, 10, "Slave surface")]
        [DescriptionAttribute("Select the slave surface for the creation of the joint definition.")]
        [Id(2, 3)]
        public string SlaveSurfaceName { get { return _joint.SlaveRegionName; } set { _joint.SlaveRegionName = value; } }
        // -------------------------------------------------------------------------------------------------------------------------
        [Category("Appearance")]
        [DisplayName("Master surface color")]
        [Description("Select the master surface color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 10)]
        public Color MasterColor { get { return _joint.MasterColor; } set { _joint.MasterColor = value; } }
        //
        [Category("Appearance")]
        [DisplayName("Slave surface color")]
        [Description("Select the slave surface color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(2, 10)]
        public Color SlaveColor { get { return _joint.SlaveColor; } set { _joint.SlaveColor = value; } }


        // Constructors
        public ViewJoint(Joint joint)
        {
            _joint = joint;
            // Master
            Dictionary<RegionTypeEnum, string> masterRegionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            masterRegionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(MasterSelectionHidden));
            masterRegionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(MasterSurfaceName));
            // Slave
            Dictionary<RegionTypeEnum, string> slaveRegionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SlaveSelectionHidden));
            slaveRegionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SlaveSurfaceName));
            //
            SetBase(joint, masterRegionTypePropertyNamePairs, slaveRegionTypePropertyNamePairs);
            //
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            UpdateJointVisibility();
        }


        // Methods
        public override Constraint GetBase()
        {
            return _joint;
        }
        public void PopulateDropDownLists(string[] surfaceNames)
        {
            Dictionary<RegionTypeEnum, string[]> masterRegionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            masterRegionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            masterRegionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            //
            Dictionary<RegionTypeEnum, string[]> slaveTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            slaveTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            slaveTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            //
            PopulateDropDownLists(masterRegionTypeListItemsPairs, slaveTypeListItemsPairs);
        }
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            //
            UpdateJointVisibility();
        }
        private void UpdateJointVisibility()
        {
            if (DynamicCustomTypeDescriptor == null) return;
            //
            // The values the master surface defines and those the selected points give stay visible, read only
            bool typedCenter = _joint.CenterDefinition == JointCenterDefinitionEnum.Coordinates;
            DynamicCustomTypeDescriptor.GetProperty(nameof(X)).SetIsReadOnly(!typedCenter);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Y)).SetIsReadOnly(!typedCenter);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Z)).SetIsReadOnly(!typedCenter);
            //
            bool hasAxis = _joint.HasAxis;
            bool userDefinedAxis = _joint.HasUserDefinedAxis;
            DynamicCustomTypeDescriptor.GetProperty(nameof(AxisDefinition)).SetIsBrowsable(hasAxis);
            DynamicCustomTypeDescriptor.GetProperty(nameof(N1)).SetIsBrowsable(hasAxis);
            DynamicCustomTypeDescriptor.GetProperty(nameof(N2)).SetIsBrowsable(hasAxis);
            DynamicCustomTypeDescriptor.GetProperty(nameof(N3)).SetIsBrowsable(hasAxis);
            DynamicCustomTypeDescriptor.GetProperty(nameof(N1)).SetIsReadOnly(!userDefinedAxis);
            DynamicCustomTypeDescriptor.GetProperty(nameof(N2)).SetIsReadOnly(!userDefinedAxis);
            DynamicCustomTypeDescriptor.GetProperty(nameof(N3)).SetIsReadOnly(!userDefinedAxis);
            //
            bool springs = _joint.HasSprings;
            DynamicCustomTypeDescriptor.GetProperty(nameof(StiffnessDefinition)).SetIsBrowsable(springs);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Stiffness)).SetIsBrowsable(springs);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Stiffness)).SetIsReadOnly(!_joint.HasUserDefinedStiffness);
        }
    }
}
