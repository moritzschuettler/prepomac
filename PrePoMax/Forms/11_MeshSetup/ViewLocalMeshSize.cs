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
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewLocalMeshSize : ViewMeshSetupItem
    {
        // Variables                                                                                                                
        private LocalMeshSize _localMeshSize;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the mesh setup item.")]
        [Id(1, 1)]
        public override string Name { get { return _localMeshSize.Name; } set { _localMeshSize.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Defined by")]
        [DescriptionAttribute("Select how the mesh setup item is defined.")]
        [Id(2, 1)]
        public LocalMeshSizeTypeEnum Type
        {
            get { return _localMeshSize.Type; }
            set { _localMeshSize.Type = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the mesh setup item.")]
        [Id(1, 2)]
        public override string RegionType { get { return _regionType; } set { _regionType = value; } }
        //
        [CategoryAttribute("Mesh size")]
        [OrderedDisplayName(1, 10, "Element size")]
        [DescriptionAttribute("Enter the element size. The global limit will override the local size.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double MeshSize { get { return _localMeshSize.MeshSize; } set { _localMeshSize.MeshSize = value; } }
        //
        [CategoryAttribute("Mesh size")]
        [OrderedDisplayName(2, 10, "Number of elements")]
        [DescriptionAttribute("Enter the number of elements per edge. The global limit will override the local size.")]
        [TypeConverter(typeof(StringIntegerConverter))]
        [Id(2, 3)]
        public int NumOfElements
        {
            get { return _localMeshSize.NumOfElements; }
            set { _localMeshSize.NumOfElements = value; }
        }


        // Constructors                                                                                                             
        public ViewLocalMeshSize(LocalMeshSize localMeshSize)
        {
            _localMeshSize = localMeshSize;                                 // 1 command
            _dctd = ProviderInstaller.Install(this);                        // 2 command
            InitializeRegion();                                             // 3 command
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public override MeshSetupItem GetBase()
        {
            return _localMeshSize;
        }
        public void UpdateVisibility()
        {
            bool visible = _localMeshSize.Type == LocalMeshSizeTypeEnum.ElementSize;
            //
            _dctd.GetProperty(nameof(MeshSize)).SetIsBrowsable(visible);
            _dctd.GetProperty(nameof(NumOfElements)).SetIsBrowsable(!visible);
        }
    }
}
