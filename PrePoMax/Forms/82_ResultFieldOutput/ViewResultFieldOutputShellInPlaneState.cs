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
using CaeResults;
using CaeModel;

namespace PrePoMax
{
    [PmxType(10505)]
    public class ViewResultFieldOutputShellInPlaneState : ViewResultFieldOutput
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        private ResultFieldOutputShellPlaneStress ResultFieldOutput
        {
            get { return (ResultFieldOutputShellPlaneStress)_resultFieldOutput; }
        }
        public override string Name { get { return ResultFieldOutput.Name; } set { ResultFieldOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Quantity")]
        [DescriptionAttribute("The in-plane state of the plies: the stresses as CalculiX solves them in 3D, the plane " +
                              "stresses the elastic constants of the ply material give the in-plane strains, as " +
                              "classical lamination theory computes them, or the in-plane strains.")]
        [Id(2, 1)]
        public ShellInPlaneQuantityEnum Quantity
        {
            get { return ResultFieldOutput.Quantity; }
            set { ResultFieldOutput.Quantity = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Source field")]
        [DescriptionAttribute("The field whose in-plane components in the material axes of the shell elements and of " +
                              "the layers of the composite shells give the in-plane state: a stress field for the " +
                              "stresses as solved, a strain field otherwise.")]
        [Id(3, 1)]
        public string FieldName { get { return ResultFieldOutput.FieldName; } set { ResultFieldOutput.FieldName = value; } }


        // Constructors                                                                                                             
        public ViewResultFieldOutputShellInPlaneState(ResultFieldOutputShellPlaneStress resultFieldOutput)
            : base(resultFieldOutput)
        {
        }


        // Methods                                                                                                                  
        public override ResultFieldOutput GetBase()
        {
            return _resultFieldOutput;
        }
        public void PopulateDropDownLists(string[] fieldNames)
        {
            _dctd.PopulateProperty(nameof(FieldName), fieldNames);
        }
    }
}
