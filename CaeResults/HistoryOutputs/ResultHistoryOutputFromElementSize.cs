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

namespace CaeResults
{
    [Serializable]
    public enum ElementSizeTypeEnum
    {
        Volume,
        Area
    }

    [Serializable]
    public class ResultHistoryOutputFromElementSize : ResultHistoryOutput
    {
        // Variables                                                                                                                
        private string _deformationVariableName;
        private string[] _parentNames;
        private ElementSizeTypeEnum _elementSizeType;


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
