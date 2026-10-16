// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewCompositeStrength : ViewMaterialProperty
    {
        // Variables
        private CompositeStrength _compositeStrength;


        // Properties
        public override string Name
        {
            get { return "Composite Strength"; }
        }
        //
        [CategoryAttribute("Fiber direction")]
        [OrderedDisplayName(0, 2, "Xt")]
        [Id(1, 1)]
        [DescriptionAttribute("Tensile strength in the material 1 (fiber) direction.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString TensileStrength1
        {
            get { return _compositeStrength.TensileStrength1.Equation; }
            set { _compositeStrength.TensileStrength1.Equation = value; }
        }
        //
        [CategoryAttribute("Fiber direction")]
        [OrderedDisplayName(1, 2, "Xc")]
        [Id(2, 1)]
        [DescriptionAttribute("Compressive strength in the material 1 (fiber) direction, entered as a positive value.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString CompressiveStrength1
        {
            get { return _compositeStrength.CompressiveStrength1.Equation; }
            set { _compositeStrength.CompressiveStrength1.Equation = value; }
        }
        //
        [CategoryAttribute("Transverse direction")]
        [OrderedDisplayName(0, 2, "Yt")]
        [Id(1, 2)]
        [DescriptionAttribute("Tensile strength in the material 2 (transverse) direction.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString TensileStrength2
        {
            get { return _compositeStrength.TensileStrength2.Equation; }
            set { _compositeStrength.TensileStrength2.Equation = value; }
        }
        //
        [CategoryAttribute("Transverse direction")]
        [OrderedDisplayName(1, 2, "Yc")]
        [Id(2, 2)]
        [DescriptionAttribute("Compressive strength in the material 2 (transverse) direction, entered as a positive value.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString CompressiveStrength2
        {
            get { return _compositeStrength.CompressiveStrength2.Equation; }
            set { _compositeStrength.CompressiveStrength2.Equation = value; }
        }
        //
        [CategoryAttribute("Shear")]
        [OrderedDisplayName(0, 2, "S12")]
        [Id(1, 3)]
        [DescriptionAttribute("In-plane shear strength in the material 1-2 plane.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString ShearStrength12
        {
            get { return _compositeStrength.ShearStrength12.Equation; }
            set { _compositeStrength.ShearStrength12.Equation = value; }
        }
        //
        [CategoryAttribute("Shear")]
        [OrderedDisplayName(1, 2, "S23")]
        [Id(2, 3)]
        [DescriptionAttribute("Transverse shear strength in the material 2-3 plane, used by the Hashin matrix " +
                              "compression mode.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString ShearStrength23
        {
            get { return _compositeStrength.ShearStrength23.Equation; }
            set { _compositeStrength.ShearStrength23.Equation = value; }
        }
        //
        [CategoryAttribute("Interaction")]
        [OrderedDisplayName(0, 1, "Tsai-Wu F12*")]
        [Id(1, 4)]
        [DescriptionAttribute("The normalized Tsai-Wu interaction coefficient F12* = F12 / sqrt(F11 F22), " +
                              "larger than -1 and smaller than 1. The usual value is -0.5.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString TsaiWuCoefficient
        {
            get { return _compositeStrength.TsaiWuCoefficient.Equation; }
            set { _compositeStrength.TsaiWuCoefficient.Equation = value; }
        }


        // Constructors
        public ViewCompositeStrength(CompositeStrength compositeStrength)
        {
            _compositeStrength = compositeStrength;
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            base.DynamicCustomTypeDescriptor.CategorySortOrder = CustomSortOrder.AscendingById;
        }


        // Methods
        public override MaterialProperty GetBase()
        {
            return _compositeStrength;
        }
    }
}
