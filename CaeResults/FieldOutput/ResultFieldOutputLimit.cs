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
    [PmxType(5026)]
    public enum LimitPlotBasedOnEnum
    {
        [StandardValue("Parts", DisplayName = "Parts", Description = "Parts")]
        Parts,
        [StandardValue("ElementSets", DisplayName = "Element sets", Description = "Element sets")]
        ElementSets,
        [StandardValue("AllElements", DisplayName = "All elements", Description = "All elements")]
        AllElements,
    }


    [PmxType(5027)]
    public class ResultFieldOutputLimit : ResultFieldOutput
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _fieldName;
        [PmxKey(1)] private string _componentName;
        [PmxKey(2)] private LimitPlotBasedOnEnum _limitPlotBasedOn;
        [PmxKey(3)] private OrderedDictionary<string, double> _itemNameLimit;


        // Properties                                                                                                               
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public string ComponentName { get { return _componentName; } set { _componentName = value; } }
        public LimitPlotBasedOnEnum LimitPlotBasedOn
        {
            get { return _limitPlotBasedOn; }
            set { _limitPlotBasedOn = value; }
        }
        public OrderedDictionary<string, double> ItemNameLimit
        {
            get { return _itemNameLimit; }
            set { _itemNameLimit = value; }
        }


        // Constructors                                                                                                             
        public ResultFieldOutputLimit(string name, string filedName, string componentName)
            : base(name)
        {
            _fieldName = filedName;
            _componentName = componentName;
            _limitPlotBasedOn = LimitPlotBasedOnEnum.Parts;
            _itemNameLimit = new OrderedDictionary<string, double>("ItemNameLimit");
        }


        // Methods                                                                                                                  
        public override string[] GetParentNames()
        {
            return new string[] { _fieldName };
        }
        public override string[] GetComponentNames()
        {
            return new string[] { FOComponentNames.Ratio, FOComponentNames.SafetyFactor };
        }
    }
}
