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
    public enum IntegralTypeEnum
    {
        Surface,
        Volume
    }

    [Serializable]
    public class ResultHistoryOutputFromIntegration : ResultHistoryOutput
    {
        // Variables                                                                                                                
        private IntegralTypeEnum _integralType;
        private string _fieldName;
        private string[] _componentNames;
        private string _unit;
        private string _deformationVariableName;
        private string[] _parentNames;


        // Properties                                                                                                               
        public IntegralTypeEnum IntegralType { get { return _integralType; } set { _integralType = value; } }
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public string[] ComponentNames { get { return _componentNames; } set { _componentNames = value; } }
        public string Unit { get { return _unit; } set { _unit = value; } }
        public string DeformationVariableName { get { return _deformationVariableName; } set { _deformationVariableName = value; } }


        // Constructors                                                                                                             
        public ResultHistoryOutputFromIntegration(string name, IntegralTypeEnum integralType,
                                                  string regionName, RegionTypeEnum regionType,
                                                  string filedName, string[] componentNames,
                                                  string deformationVariableName)
            : base(name, regionName, regionType)
        {
            _integralType = integralType;
            _fieldName = filedName;
            _componentNames = componentNames;
            _unit = "/";
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
