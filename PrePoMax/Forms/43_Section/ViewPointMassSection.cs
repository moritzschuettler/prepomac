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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeModel;

namespace PrePoMax
{
    [PmxType(10385)]
    public class ViewPointMassSection : ViewSection
    {
        // Variables                                                                                                                
        [PmxKey(0)] private PointMassSection _massSection;
        

        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(6, 10, "Mass")]
        [DescriptionAttribute("Enter the mass to be applied to each selected node.")]
        [TypeConverter(typeof(EquationMassConverter))]
        public EquationString Mass
        {
            get { return _massSection.Mass.Equation; }
            set { _massSection.Mass.Equation = value; }
        }
       


        // Constructors                                                                                                             
        public ViewPointMassSection(PointMassSection massSection)
        {
            _massSection = massSection;
            //
            SetBase(_massSection);
        }


        // Methods                                                                                                                  
        

    }

}
