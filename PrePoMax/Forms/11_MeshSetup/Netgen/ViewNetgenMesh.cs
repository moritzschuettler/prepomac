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
    public class ViewNetgenMesh : ViewMeshSetupItem
    {
        // Variables
        private NetgenMesh _netgenMesh;


        // Properties
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [Description("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _netgenMesh.Name; } set { _netgenMesh.Name = value; } }
        //
        [Category("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [Description("Select the solid or shell parts for the Netgen meshing.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [Category("Mesh optimization")]
        [OrderedDisplayName(0, 10, "Optimize steps 2D")]
        [Description("Number of optimize steps to use for 2-D mesh optimization.")]
        [Id(1, 3)]
        public int OptimizeSteps2D { get { return _netgenMesh.OptimizeSteps2D; } set { _netgenMesh.OptimizeSteps2D = value; } }
        //
        [Category("Mesh optimization")]
        [OrderedDisplayName(1, 10, "Optimize steps 3D")]
        [Description("Number of optimize steps to use for 3-D mesh optimization.")]
        [Id(2, 3)]
        public int OptimizeSteps3D { get { return _netgenMesh.OptimizeSteps3D; } set { _netgenMesh.OptimizeSteps3D = value; } }
        //
        [Category("Mesh type")]
        [OrderedDisplayName(0, 10, "Quad-dominated mesh")]
        [Description("Use quad-dominated mesh for shell parts.")]
        [Id(1, 4)]
        public bool QuadDominated { get { return _netgenMesh.QuadDominated; } set { _netgenMesh.QuadDominated = value; } }
        //
        [Category("Mesh type")]
        [OrderedDisplayName(1, 10, "Transfinite 4-sided faces")]
        [Description("Select the 4-sided model faces meshed as structured triangle grids. Size checked leaves a " +
                     "face whose grid would be much finer than the mesh size asks for to the usual mesher.")]
        [Id(2, 4)]
        public NetgenTransfiniteFacesEnum TransfiniteFaces
        {
            get { return _netgenMesh.TransfiniteFaces; }
            set
            {
                _netgenMesh.TransfiniteFaces = value;
                UpdateVisibility();
            }
        }
        //
        [Category("Mesh type")]
        [OrderedDisplayName(2, 10, "Transfinite threshold angle")]
        [Description("Quadrangular faces with a corner angle larger than the threshold angle are ignored.")]
        [TypeConverter(typeof(StringAngleDegConverter))]
        [Id(3, 4)]
        public double TransfiniteAngleDeg
        {
            get { return _netgenMesh.TransfiniteAngleDeg; }
            set { _netgenMesh.TransfiniteAngleDeg = value; }
        }


        // Constructors
        public ViewNetgenMesh(NetgenMesh netgenMesh)
        {
            _netgenMesh = netgenMesh;
            _dctd = ProviderInstaller.Install(this);
            InitializeRegion();
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(QuadDominated));
            //
            UpdateVisibility();
        }


        // Methods
        public override MeshSetupItem GetBase()
        {
            return _netgenMesh;
        }
        private void UpdateVisibility()
        {
            _dctd.GetProperty(nameof(TransfiniteAngleDeg)).SetIsBrowsable(
                _netgenMesh.TransfiniteFaces != NetgenTransfiniteFacesEnum.None);
        }
    }
}
