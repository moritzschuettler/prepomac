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
    public class ViewSplitByExtrudedEdges : ViewGeometryFeature
    {
        // Variables
        private SplitByExtrudedEdges _splitByExtrudedEdges;


        // Properties
        [CategoryAttribute("Splitter")]
        [OrderedDisplayName(0, 10, "Edges")]
        [DescriptionAttribute("Select the edges that are swept into the splitting tool. Hold the Shift key to " +
                              "select a chain of edges in one operation.")]
        [Id(1, 2)]
        public string SplitterSelection
        {
            get { return SelectionToString(_splitByExtrudedEdges.SplitterIds, "edge"); }
            set { }
        }
        //
        [CategoryAttribute("Direction")]
        [OrderedDisplayName(0, 10, "Defined by")]
        [DescriptionAttribute("Select how the sweep direction is defined: by picking the geometry that " +
                              "gives it, or by entering its components.")]
        [Id(1, 3)]
        public SplitExtrusionDirectionTypeEnum DirectionType
        {
            get { return _splitByExtrudedEdges.DirectionType; }
            set { _splitByExtrudedEdges.DirectionType = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Direction")]
        [OrderedDisplayName(1, 10, "Selection")]
        [DescriptionAttribute("Select the geometry that gives the sweep direction. One item gives the " +
                              "direction it carries: a straight edge the way it runs, a planar face its " +
                              "normal, a cylindrical or conical face its axis. Hold the Shift key to select " +
                              "a planar face and a straight edge together, which gives the direction that " +
                              "lies in the face and crosses the edge.")]
        [Id(2, 3)]
        public string DirectionSelection
        {
            get { return SelectionToString(_splitByExtrudedEdges.DirectionIds, "item"); }
            set { }
        }
        //
        [CategoryAttribute("Direction")]
        [OrderedDisplayName(2, 10, "Dx")]
        [DescriptionAttribute("X component of the sweep direction.")]
        [Id(3, 3)]
        public double Dx
        {
            get { return _splitByExtrudedEdges.Direction[0]; }
            set { _splitByExtrudedEdges.Direction[0] = value; }
        }
        //
        [CategoryAttribute("Direction")]
        [OrderedDisplayName(3, 10, "Dy")]
        [DescriptionAttribute("Y component of the sweep direction.")]
        [Id(4, 3)]
        public double Dy
        {
            get { return _splitByExtrudedEdges.Direction[1]; }
            set { _splitByExtrudedEdges.Direction[1] = value; }
        }
        //
        [CategoryAttribute("Direction")]
        [OrderedDisplayName(4, 10, "Dz")]
        [DescriptionAttribute("Z component of the sweep direction.")]
        [Id(5, 3)]
        public double Dz
        {
            get { return _splitByExtrudedEdges.Direction[2]; }
            set { _splitByExtrudedEdges.Direction[2] = value; }
        }
        //
        [CategoryAttribute("Length")]
        [OrderedDisplayName(0, 10, "Defined by")]
        [DescriptionAttribute("Select how far the edges are swept. Through the region sizes the sweep from the " +
                              "bounding box of the parts being split, so that it reaches past all of them.")]
        [Id(1, 4)]
        public SplitExtrusionLengthTypeEnum LengthType
        {
            get { return _splitByExtrudedEdges.LengthType; }
            set { _splitByExtrudedEdges.LengthType = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Length")]
        [OrderedDisplayName(1, 10, "Length")]
        [DescriptionAttribute("Enter the sweep length.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 4)]
        public double Length
        {
            get { return _splitByExtrudedEdges.Length; }
            set { _splitByExtrudedEdges.Length = value; }
        }
        //
        [CategoryAttribute("Length")]
        [OrderedDisplayName(2, 10, "Both directions")]
        [DescriptionAttribute("Sweep the edges in both directions instead of only along the direction.")]
        [Id(3, 4)]
        public bool BothDirections
        {
            get { return _splitByExtrudedEdges.BothDirections; }
            set { _splitByExtrudedEdges.BothDirections = value; }
        }


        // Constructors
        public ViewSplitByExtrudedEdges(SplitByExtrudedEdges splitByExtrudedEdges)
        {
            _splitByExtrudedEdges = splitByExtrudedEdges;                   // 1 command
            _dctd = ProviderInstaller.Install(this);                        // 2 command
            //
            UpdateVisibility();
        }


        // Methods
        public override GeometryFeature GetBase()
        {
            return _splitByExtrudedEdges;
        }
        public override void UpdateVisibility()
        {
            bool selected = _splitByExtrudedEdges.DirectionType == SplitExtrusionDirectionTypeEnum.Selected;
            bool vector = _splitByExtrudedEdges.DirectionType == SplitExtrusionDirectionTypeEnum.Vector;
            //
            _dctd.GetProperty(nameof(DirectionSelection)).SetIsBrowsable(selected);
            _dctd.GetProperty(nameof(Dx)).SetIsBrowsable(selected || vector);
            _dctd.GetProperty(nameof(Dy)).SetIsBrowsable(selected || vector);
            _dctd.GetProperty(nameof(Dz)).SetIsBrowsable(selected || vector);
            //
            _dctd.GetProperty(nameof(Length))
                 .SetIsBrowsable(_splitByExtrudedEdges.LengthType == SplitExtrusionLengthTypeEnum.Value);
        }
    }
}
