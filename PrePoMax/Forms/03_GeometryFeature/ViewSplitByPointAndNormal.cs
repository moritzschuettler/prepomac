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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewSplitByPointAndNormal : ViewGeometryFeature
    {
        // Variables
        private SplitByPointAndNormal _splitByPointAndNormal;


        // Properties
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(0, 10, "Selection")]
        [DescriptionAttribute("Select the geometry that defines the splitting plane. A vertex gives the point, a " +
                              "straight edge or a planar, cylindrical or conical face gives the normal, and a " +
                              "circular edge gives both at once.")]
        [ReadOnlyAttribute(true)]
        [Id(1, 2)]
        public string SplitterSelection
        {
            get { return SelectionToString(_splitByPointAndNormal.SplitterIds, "item"); }
            set { }
        }
        //
        [CategoryAttribute("Point")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("X coordinate of the point on the splitting plane.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double X { get { return _splitByPointAndNormal.Point[0]; } set { _splitByPointAndNormal.Point[0] = value; } }
        //
        [CategoryAttribute("Point")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the point on the splitting plane.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 3)]
        public double Y { get { return _splitByPointAndNormal.Point[1]; } set { _splitByPointAndNormal.Point[1] = value; } }
        //
        [CategoryAttribute("Point")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the point on the splitting plane.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 3)]
        public double Z { get { return _splitByPointAndNormal.Point[2]; } set { _splitByPointAndNormal.Point[2] = value; } }
        //
        [CategoryAttribute("Normal")]
        [OrderedDisplayName(0, 10, "Nx")]
        [DescriptionAttribute("X component of the splitting plane normal.")]
        [Id(1, 4)]
        public double Nx { get { return _splitByPointAndNormal.Normal[0]; } set { _splitByPointAndNormal.Normal[0] = value; } }
        //
        [CategoryAttribute("Normal")]
        [OrderedDisplayName(1, 10, "Ny")]
        [DescriptionAttribute("Y component of the splitting plane normal.")]
        [Id(2, 4)]
        public double Ny { get { return _splitByPointAndNormal.Normal[1]; } set { _splitByPointAndNormal.Normal[1] = value; } }
        //
        [CategoryAttribute("Normal")]
        [OrderedDisplayName(2, 10, "Nz")]
        [DescriptionAttribute("Z component of the splitting plane normal.")]
        [Id(3, 4)]
        public double Nz { get { return _splitByPointAndNormal.Normal[2]; } set { _splitByPointAndNormal.Normal[2] = value; } }
        //
        [CategoryAttribute("Plane")]
        [OrderedDisplayName(0, 10, "Offset")]
        [DescriptionAttribute("Enter the distance the splitting plane is moved along its normal.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 5)]
        public double Offset { get { return _splitByPointAndNormal.Offset; } set { _splitByPointAndNormal.Offset = value; } }


        // Constructors
        public ViewSplitByPointAndNormal(SplitByPointAndNormal splitByPointAndNormal)
        {
            _splitByPointAndNormal = splitByPointAndNormal;                 // 1 command
            _dctd = ProviderInstaller.Install(this);                        // 2 command
            //
            UpdateVisibility();
        }


        // Methods
        public override GeometryFeature GetBase()
        {
            return _splitByPointAndNormal;
        }
    }
}
