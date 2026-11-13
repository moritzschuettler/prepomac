// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
        [OrderedDisplayName(1, 10, "Material")]
        [DescriptionAttribute("Select the material of a single layer. A layup of more layers shows the material of all " +
                              "layers, or Mixed, and its materials are set in the layers.")]
        public override string MaterialName
        {
            get { return GetLayupMaterialName(); }
            set { if (_layers.Count == 1) _layers[0].MaterialName = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Total thickness")]
        [DescriptionAttribute("The thickness of all layers, of the mirrored ones too for a symmetric layup. It can be " +
                              "set for a single layer that is not symmetric.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        public EquationString TotalThickness
        {
            get
            {
                if (IsThicknessEditable()) return _layers[0].ThicknessEq;
                //
                CompositeShellSection section = (CompositeShellSection)GetBase();
                try
                {
                    return new EquationContainer(typeof(StringLengthConverter), section.GetTotalThickness()).Equation;
                }
                catch { return section.Thickness.Equation; }
            }
            set { if (IsThicknessEditable()) _layers[0].ThicknessEq = value; }
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
        [OrderedDisplayName(4, 10, "Symmetric layup")]
        [DescriptionAttribute("Complete the layup with its mirror image: the layers are followed by the same layers in " +
                              "reverse order.")]
        public bool Symmetric
        {
            get { return _compositeShellSection.Symmetric; }
            set { _compositeShellSection.Symmetric = value; UpdateLayupProperties(); }
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
            base.DynamicCustomTypeDescriptor.RenameBooleanPropertyToYesNo(nameof(Symmetric));
            UpdateLayupProperties();
        }


        // Methods
        public override Section GetBase()
        {
            List<CompositeLayer> layers = new List<CompositeLayer>();
            foreach (CompositeLayerDataPoint point in _layers) layers.Add(point.GetLayer());
            _compositeShellSection.Layers = layers;
            return _compositeShellSection;
        }
        // The material and the thickness of a single layer are set here, the ones of more layers in the layers
        public void UpdateLayupProperties()
        {
            DynamicCustomTypeDescriptor dctd = base.DynamicCustomTypeDescriptor;
            dctd.GetProperty(nameof(MaterialName)).SetIsReadOnly(_layers.Count != 1);
            dctd.GetProperty(nameof(TotalThickness)).SetIsReadOnly(!IsThicknessEditable());
        }
        private bool IsThicknessEditable()
        {
            return _layers.Count == 1 && !_compositeShellSection.Symmetric;
        }
        private string GetLayupMaterialName()
        {
            HashSet<string> materialNames = new HashSet<string>();
            foreach (CompositeLayerDataPoint layer in _layers) materialNames.Add(layer.MaterialName);
            //
            if (materialNames.Count == 0) return null;
            else if (materialNames.Count == 1) return _layers[0].MaterialName;
            else return "Mixed";
        }
    }
}
