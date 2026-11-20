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

namespace CaeResults
{
    // Compatibility for version v2.6.0
    // The From Element Size history output was replaced by the From Integration history output using the
    // Geometry integral result. It can no longer be created, but existing definitions are still read and
    // evaluated, and they can still be edited.
    [PmxType(5065)]
    public enum ElementSizeTypeEnum
    {
        Area,
        Volume
    }


    [PmxType(5066)]
    public class ResultHistoryOutputFromElementSize : ResultHistoryOutput
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _deformationVariableName;
        [PmxKey(1)] private string[] _parentNames;
        [PmxKey(2)] private ElementSizeTypeEnum _elementSizeType;


        // Properties                                                                                                               
        public string DeformationVariableName { get { return _deformationVariableName; } set { _deformationVariableName = value; } }
        public ElementSizeTypeEnum ElementSizeType { get { return _elementSizeType; } set { _elementSizeType = value; } }


        // Constructors                                                                                                             
        public ResultHistoryOutputFromElementSize(string name, ElementSizeTypeEnum elementSizeType, 
                                                  string regionName, RegionTypeEnum regionType,
                                                  string deformationVariableName)
            : base(name, regionName, regionType)
        {
            _elementSizeType = elementSizeType;
            _deformationVariableName = deformationVariableName;
            OrderedDictionary<string, string> map = FeResults.GetPossibleDeformationFieldOutputNamesMap();
            _parentNames = new string[] { map[_deformationVariableName] };
        }


        // Methods                                                                                                                  
        public override string[] GetParentNames()
        {
            return _parentNames;
        }
    }
}
