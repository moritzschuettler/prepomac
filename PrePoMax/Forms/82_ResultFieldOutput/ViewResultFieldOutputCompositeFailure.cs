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
    [PmxType(10499)]
    public class ViewResultFieldOutputCompositeFailure : ViewResultFieldOutput
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        private ResultFieldOutputCompositeFailure ResultFieldOutput
        {
            get { return (ResultFieldOutputCompositeFailure)_resultFieldOutput; }
        }
        public override string Name { get { return ResultFieldOutput.Name; } set { ResultFieldOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Criterion")]
        [DescriptionAttribute("Failure criterion evaluated with the composite strength of the material of each ply. " +
                              "Plies whose material has no composite strength show no value.")]
        [Id(2, 1)]
        public CompositeFailureCriterionEnum Criterion
        {
            get { return ResultFieldOutput.Criterion; }
            set { ResultFieldOutput.Criterion = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Stresses")]
        [DescriptionAttribute("The in-plane stresses of the plies the criterion evaluates: as CalculiX solves them in " +
                              "3D, or the plane stresses the elastic constants of the ply material give the in-plane " +
                              "strains, as classical lamination theory computes them.")]
        [Id(3, 1)]
        public ShellInPlaneQuantityEnum Quantity
        {
            get { return ResultFieldOutput.Quantity; }
            set { ResultFieldOutput.Quantity = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 10, "Source field")]
        [DescriptionAttribute("The field whose in-plane components in the material axes give the stresses: a stress " +
                              "field for the stresses as solved, a strain field for the plane stresses from strains.")]
        [Id(4, 1)]
        public string FieldName { get { return ResultFieldOutput.FieldName; } set { ResultFieldOutput.FieldName = value; } }


        // Constructors                                                                                                             
        public ViewResultFieldOutputCompositeFailure(ResultFieldOutputCompositeFailure resultFieldOutput)
            : base(resultFieldOutput)
        {
            // A failure criterion evaluates stresses
            _dctd.GetProperty(nameof(Quantity)).RemoveStandardValues(
                new HashSet<string>() { ShellInPlaneQuantityEnum.Strain.ToString() });
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
