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
    public class ViewTetrahedralNetgen : ViewMeshSetupItem
    {
        // Variables
        private TetrahedralNetgen _tetrahedralNetgen;


        // Properties
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [Description("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _tetrahedralNetgen.Name; } set { _tetrahedralNetgen.Name = value; } }
        //
        [Category("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [Description("Select the solid parts for the Netgen tetrahedral meshing.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }


        // Constructors
        public ViewTetrahedralNetgen(TetrahedralNetgen tetrahedralNetgen)
        {
            _tetrahedralNetgen = tetrahedralNetgen;
            _dctd = ProviderInstaller.Install(this);
            InitializeRegion();
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
        }


        // Methods
        public override MeshSetupItem GetBase()
        {
            return _tetrahedralNetgen;
        }
    }
}
