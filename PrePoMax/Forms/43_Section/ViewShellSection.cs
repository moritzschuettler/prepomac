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
    public class ViewShellSection : ViewSection
    {
        // Variables                                                                                                                
        protected CaeModel.ShellSection _shellSection;
        

        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Thickness")]
        [DescriptionAttribute("Set the shell thickness.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        public EquationString Thickness
        {
            get { return _shellSection.Thickness.Equation; }
            set { _shellSection.Thickness.Equation = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 10, "Offset")]
        [DescriptionAttribute("Set the offset of the shell mid-surface in regard to the selected geometry. " +
                              "The unit is the shell thickness.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Offset
        {
            get { return _shellSection.Offset.Equation; }
            set { _shellSection.Offset.Equation = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(4, 10, "Composites")]
        [DescriptionAttribute("Enable composites mode to write the orientation angle to the shell section in the INP file.")]
        public bool Composites
        {
            get { return _shellSection.Composites; }
            set
            {
                _shellSection.Composites = value;
                base.DynamicCustomTypeDescriptor.GetProperty(nameof(Angle)).SetIsBrowsable(value);
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(5, 10, "Angle [deg]")]
        [DescriptionAttribute("Set the material orientation angle of the shell section in degrees. Default is 0.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Angle
        {
            get { return _shellSection.Angle.Equation; }
            set { _shellSection.Angle.Equation = value; }
        }


        // Constructors                                                                                                             
        public ViewShellSection(CaeModel.ShellSection shellSection)
        {
            _shellSection = shellSection;
            SetBase(_shellSection);
            //
            base.DynamicCustomTypeDescriptor.RenameBooleanPropertyToYesNo(nameof(Composites));
            base.DynamicCustomTypeDescriptor.GetProperty(nameof(Angle)).SetIsBrowsable(shellSection.Composites);
        }


        // Methods                                                                                                                  


    }

}
