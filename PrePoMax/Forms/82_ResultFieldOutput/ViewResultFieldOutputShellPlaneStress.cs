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
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultFieldOutputShellPlaneStress : ViewResultFieldOutput
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
        [OrderedDisplayName(1, 10, "Field name")]
        [DescriptionAttribute("Stress field whose in-plane components in the material axes give the principal " +
                              "values of the plane stress of the shell elements and of the layers of the composite " +
                              "shells.")]
        [Id(2, 1)]
        public string FieldName { get { return ResultFieldOutput.FieldName; } set { ResultFieldOutput.FieldName = value; } }


        // Constructors                                                                                                             
        public ViewResultFieldOutputShellPlaneStress(ResultFieldOutputShellPlaneStress resultFieldOutput)
            : base(resultFieldOutput)
        {
        }


        // Methods                                                                                                                  
        public override ResultFieldOutput GetBase()
        {
            return _resultFieldOutput;
        }
        public void PopulateDropDownLists(string[] tensorFieldNames)
        {
            _dctd.PopulateProperty(nameof(FieldName), tensorFieldNames);
        }
    }
}
