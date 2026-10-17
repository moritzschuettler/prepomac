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
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public class ViewCompositeShellSection : ViewSection
    {
        // Variables                                                                                                                
        private CompositeShellSection _compositeShellSection;
        private List<CompositeLayerDataPoint> _layers;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Total thickness")]
        [DescriptionAttribute("The thickness of all layers, of the mirrored ones too for a symmetric layup.")]
        [TypeConverter(typeof(StringLengthConverter))]
        public double TotalThickness
        {
            get
            {
                try { return ((CompositeShellSection)GetBase()).GetTotalThickness(); }
                catch { return double.NaN; }
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 10, "Offset")]
        [DescriptionAttribute("Set the offset of the shell mid-surface in regard to the selected geometry. " +
                              "The unit is the total thickness.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Offset
        {
            get { return _compositeShellSection.Offset.Equation; }
            set { _compositeShellSection.Offset.Equation = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(4, 10, "Symmetric")]
        [DescriptionAttribute("Complete the layup with its mirror image: the layers are followed by the same layers in " +
                              "reverse order.")]
        public bool Symmetric
        {
            get { return _compositeShellSection.Symmetric; }
            set { _compositeShellSection.Symmetric = value; }
        }
        //
        [Browsable(false)]
        public List<CompositeLayerDataPoint> Layers { get { return _layers; } }


        // Constructors                                                                                                             
        public ViewCompositeShellSection(CompositeShellSection compositeShellSection)
        {
            _compositeShellSection = compositeShellSection;
            _layers = new List<CompositeLayerDataPoint>();
            foreach (CompositeLayer layer in _compositeShellSection.Layers) _layers.Add(new CompositeLayerDataPoint(layer));
            SetBase(_compositeShellSection);
            // Each layer has its own material
            base.DynamicCustomTypeDescriptor.GetProperty(nameof(MaterialName)).SetIsBrowsable(false);
        }


        // Methods                                                                                                                  
        public override Section GetBase()
        {
            List<CompositeLayer> layers = new List<CompositeLayer>();
            foreach (CompositeLayerDataPoint point in _layers) layers.Add(point.GetLayer());
            _compositeShellSection.Layers = layers;
            return _compositeShellSection;
        }
    }
}
