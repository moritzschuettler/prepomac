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
using CaeMesh;
using DynamicTypeDescriptor;

namespace CaeResults
{
    [Serializable]
    [PmxType(5052)]
    public enum IntegralTypeEnum
    {
        Line,
        Surface,
        Volume
    }
    //
    [Serializable]
    [PmxType(5053)]
    public enum IntegralResultEnum
    {
        // Geometry:          the region measure              integral of w
        // Field:             the field integral              integral of f * w
        // Field by Geometry: the region average of the field (integral of f * w) / (region integral of w)
        [StandardValue("Geometry", DisplayName = "Geometry")]
        Geometry,
        [StandardValue("Field", DisplayName = "Field")]
        Field,
        [StandardValue("FieldByGeometry", DisplayName = "Field by Geometry")]
        FieldByGeometry
    }

    [Serializable]
    [PmxType(5054)]
    public class ResultHistoryOutputFromIntegration : ResultHistoryOutput
    {
        // Variables
        [PmxKey(0)] private IntegralTypeEnum _integralType;
        [PmxKey(1)] private IntegralResultEnum _integralResult;
        [PmxKey(2)] private string _fieldName;
        [PmxKey(3)] private string[] _componentNames;
        [PmxKey(4)] private string _unit;
        [PmxKey(5)] private string _deformationVariableName;
        [PmxKey(6)] private string[] _parentNames;
        [PmxKey(7)] private IntegrationScaleType _crossSection;         // 2D out-of-plane scaling (Line and Volume integrals)
        [PmxKey(8)] private double _thickness;                          // used when _crossSection == Thickness


        // Properties
        public IntegralTypeEnum IntegralType { get { return _integralType; } set { _integralType = value; } }
        public IntegralResultEnum IntegralResult { get { return _integralResult; } set { _integralResult = value; } }
        public string FieldName { get { return _fieldName; } set { _fieldName = value; } }
        public string[] ComponentNames { get { return _componentNames; } set { _componentNames = value; } }
        public string Unit { get { return _unit; } set { _unit = value; } }
        public string DeformationVariableName { get { return _deformationVariableName; } set { _deformationVariableName = value; } }
        public IntegrationScaleType CrossSection { get { return _crossSection; } set { _crossSection = value; } }
        public double Thickness { get { return _thickness; } set { _thickness = value; } }


        // Constructors                                                                                                             
        public ResultHistoryOutputFromIntegration(string name, IntegralTypeEnum integralType,
                                                  string regionName, RegionTypeEnum regionType,
                                                  string filedName, string[] componentNames,
                                                  string deformationVariableName)
            : base(name, regionName, regionType)
        {
            _integralType = integralType;
            _integralResult = IntegralResultEnum.Geometry;
            _fieldName = filedName;
            _componentNames = componentNames;
            _unit = "/";
            _deformationVariableName = deformationVariableName;
            _crossSection = IntegrationScaleType.None;
            _thickness = 1.0;
            OrderedDictionary<string, string> map = FeResults.GetPossibleDeformationFieldOutputNamesMap();
            _parentNames = new string[] { map[_deformationVariableName] };
        }


        // Methods
        public override string[] GetParentNames()
        {
            return _parentNames;
        }
        // Builds the out-of-plane integration scaling from the user selected cross section.
        // Old files deserialize _crossSection == None, so the result is an unscaled integral.
        public IntegrationScale GetIntegrationScale()
        {
            if (_integralType == IntegralTypeEnum.Surface) return IntegrationScale.None;
            //
            switch (_crossSection)
            {
                case IntegrationScaleType.None:
                    return IntegrationScale.None;
                case IntegrationScaleType.Thickness:
                    return IntegrationScale.FromThickness(_thickness);
                case IntegrationScaleType.Axisymmetric:
                    return IntegrationScale.FromAxisymmetric();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
