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
using DynamicTypeDescriptor;
using System.ComponentModel;

namespace PrePoMax.Forms
{
    public class ViewShellMmg : ViewMeshSetupItem
    {
        // Variables
        private ShellMmg _shellMmg;


        // Properties
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [Description("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _shellMmg.Name; } set { _shellMmg.Name = value; } }
        //
        [Category("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [Description("Select the shell parts for the MMG meshing.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [Category("Geometry approximation")]
        [OrderedDisplayName(0, 10, "Hausdorff definition")]
        [Description("Select whether the Hausdorff distance is absolute or relative to the bounding box diagonal.")]
        [Id(1, 3)]
        public bool Relative
        {
            get { return _shellMmg.RelativeSize; }
            set
            {
                _shellMmg.RelativeSize = value;
                UpdateVisibility();
            }
        }
        [Category("Geometry approximation")]
        [OrderedDisplayName(1, 10, "Relative Hausdorff")]
        [Description("The Hausdorff distance relative to the bounding box diagonal.")]
        [Id(2, 3)]
        public double RelativeHausdorff
        {
            get { return _shellMmg.RelativeHausdorff; }
            set { _shellMmg.RelativeHausdorff = value; }
        }
        [Category("Geometry approximation")]
        [OrderedDisplayName(2, 10, "Absolute Hausdorff")]
        [Description("Maximum distance allowed between the remeshed boundary and the geometry.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 3)]
        public double Hausdorff { get { return _shellMmg.Hausdorff; } set { _shellMmg.Hausdorff = value; } }
        //
        [Category("Mesh operations")]
        [OrderedDisplayName(0, 10, "Keep model edges")]
        [Description("Select Yes to preserve and No to ignore the model edges.")]
        [Id(1, 4)]
        public bool KeepModelEdges
        {
            get { return _shellMmg.KeepModelEdges; }
            set { _shellMmg.KeepModelEdges = value; }
        }


        // Constructors
        public ViewShellMmg(ShellMmg shellMmg)
        {
            _shellMmg = shellMmg;
            _dctd = ProviderInstaller.Install(this);
            InitializeRegion();
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            _dctd.RenameBooleanProperty(nameof(Relative), "Relative", "Absolute");
            _dctd.RenameBooleanPropertyToYesNo(nameof(KeepModelEdges));
            UpdateVisibility();
        }


        // Methods
        public override MeshSetupItem GetBase()
        {
            return _shellMmg;
        }
        public void UpdateVisibility()
        {
            _dctd.GetProperty(nameof(RelativeHausdorff)).SetIsBrowsable(_shellMmg.RelativeSize);
            _dctd.GetProperty(nameof(Hausdorff)).SetIsBrowsable(!_shellMmg.RelativeSize);
        }
    }
}
