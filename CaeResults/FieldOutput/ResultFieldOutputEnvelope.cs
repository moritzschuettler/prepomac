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

namespace CaeResults
{
    [PmxType(5023)]
    public enum EnvelopeTypeEnum
    {
        [StandardValue("Min", DisplayName = "Min", Description = "Min")]
        Min,
        [StandardValue("Max", DisplayName = "Max", Description = "Max")]
        Max,
        [StandardValue("Average", DisplayName = "Average", Description = "Average")]
        Average
    }


    [PmxType(5024)]
    public class ResultFieldOutputEnvelope : ResultFieldOutput
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _fieldName;
        [PmxKey(1)] private string _componentName;
        //private EnvelopeTypeEnum _envelopeType;


        // Properties                                                                                                               
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public string ComponentName { get { return _componentName; } set { _componentName = value; } }
        //public EnvelopeTypeEnum EnvelopeType { get { return _envelopeType; } set { _envelopeType = value; } }


        // Constructors                                                                                                             
        public ResultFieldOutputEnvelope(string name, string filedName, string componentName)
            : base(name)
        {
            _fieldName = filedName;
            _componentName = componentName;
            //_envelopeType = EnvelopeTypeEnum.Max;
        }


        // Methods                                                                                                                  
        public override string[] GetParentNames()
        {
            return new string[] { _fieldName };
        }
        public override string[] GetComponentNames()
        {
            return new string[] { FOComponentNames.Max, FOComponentNames.Min, FOComponentNames.Average };
        }
    }
}
