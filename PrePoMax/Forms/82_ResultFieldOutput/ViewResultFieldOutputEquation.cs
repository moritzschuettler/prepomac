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
using CaeResults;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultFieldOutputEquation : ViewResultFieldOutput
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        private ResultFieldOutputEquation ResultFieldOutput
        {
            get { return (ResultFieldOutputEquation)_resultFieldOutput; }
        }
        public override string Name { get { return ResultFieldOutput.Name; } set { ResultFieldOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Equation")]
        [DescriptionAttribute("Example equation: =DISP.ALL\r\n" +
                              "To include a field component into the equation " +
                              "refer to the component with its full name as " +
                              "Field_Name.Component_Name. All names are case sensitive.")]
        [Id(2, 1)]
        public string Equation
        {
            get { return ResultFieldOutput.Equation; }
            set
            {
                if (!value.Trim().StartsWith("=")) value = "=" + value;
                ResultFieldOutput.Equation = value;
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "User defined unit")]
        [DescriptionAttribute("User defined unit for the history output equation value.")]
        [Id(3, 1)]
        public string Unit { get { return ResultFieldOutput.Unit; } set { ResultFieldOutput.Unit = value; } }


        // Constructors                                                                                                             
        public ViewResultFieldOutputEquation(ResultFieldOutputEquation resultFieldOutput)
            : base(resultFieldOutput)
        {
        }


        // Methods                                                                                                                  
        public override ResultFieldOutput GetBase()
        {
            return _resultFieldOutput;
        }
        private void UpdateVisibility()
        {
            
        }
    }



   
}
