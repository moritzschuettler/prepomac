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

namespace PrePoMax
{
    [Serializable]
    public class ViewSolidSection : ViewSection
    {
        // Variables                                                                                                                
        private CaeModel.SolidSection _solidSection;
        

        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(6, 10, "Thickness")]
        [DescriptionAttribute("Set the thickness for the 2D plane strain/stress elements. " +
                              "For the mixed axisymmetric model the thickness of the plane stress elements must be " +
                              "defined for an angle of 360°.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        public EquationString Thickness
        {
            get { return _solidSection.Thickness.Equation; }
            set { _solidSection.Thickness.Equation = value; }
        }


        // Constructors                                                                                                             
        public ViewSolidSection(CaeModel.SolidSection solidSection)
        {
            _solidSection = solidSection;
            SetBase(_solidSection);
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(Thickness)).SetIsBrowsable(solidSection.TwoD);
        }


        // Methods                                                                                                                  


    }

}
