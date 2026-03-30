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
    public class ViewResultFieldOutputCoordinateSystemTransform : ViewResultFieldOutput
    {
        // Variables                                                                                                                


        // Properties                                                                                                               
        private ResultFieldOutputCoordinateSystemTransform ResultFieldOutput
        {
            get { return (ResultFieldOutputCoordinateSystemTransform)_resultFieldOutput; }
        }
        public override string Name { get { return ResultFieldOutput.Name; } set { ResultFieldOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Field name")]
        [DescriptionAttribute("Filed name for the field output.")]
        [Id(2, 1)]
        public string FieldName { get { return ResultFieldOutput.FieldName; } set { ResultFieldOutput.FieldName = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Coordinate system")]
        [DescriptionAttribute("Coordinate system name for the field output.")]
        [Id(3, 1)]
        public string CoordinateSystemName
        {
            get { return ResultFieldOutput.CoordinateSystemName; }
            set { ResultFieldOutput.CoordinateSystemName = value; }
        }


        // Constructors                                                                                                             
        public ViewResultFieldOutputCoordinateSystemTransform(ResultFieldOutputCoordinateSystemTransform resultFieldOutput)
            : base(resultFieldOutput)
        {
        }


        // Methods                                                                                                                  
        public override ResultFieldOutput GetBase()
        {
            return _resultFieldOutput;
        }
        public void PopulateDropDownLists(Dictionary<string, string[]> filedNameComponentNames, string[] coordinateSystemNames)
        {
            _dctd.PopulateProperty(nameof(FieldName), filedNameComponentNames.Keys.ToArray());
            _dctd.PopulateProperty(nameof(CoordinateSystemName), coordinateSystemNames);
        }
        private void UpdateVisibility()
        {
           
        }
    }



   
}
